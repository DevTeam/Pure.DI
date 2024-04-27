// ReSharper disable InvertIf
// ReSharper disable ClassNeverInstantiated.Global
namespace Pure.DI.Core.Code;

internal class CompositionBuilder(
    IBuildTools buildTools,
    INodeInfo nodeInfo,
    ITypeResolver typeResolver,
    IVariablesBuilder variablesBuilder,
    ICodeBuilder<Block> blockBuilder,
    ICodeBuilder<IStatement> statementBuilder,
    IBuilder<CompositionCode, LinesBuilder> classDiagramBuilder,
    CancellationToken cancellationToken)
    : IBuilder<DependencyGraph, CompositionCode>
{
    public CompositionCode Build(DependencyGraph graph)
    {
        var roots = new List<Root>();
        var map = new VariablesMap();
        var allArgs = new HashSet<Variable>();
        var isThreadSafe = false;
        var isThreadSafeEnabled = graph.Source.Hints.IsThreadSafeEnabled;
        foreach (var root in graph.Roots.Values)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var rootBlock = variablesBuilder.Build(graph.Graph, map, root.Node, root.Injection);
            var ctx = new BuildContext(
                0,
                buildTools,
                statementBuilder,
                graph,
                rootBlock.Current,
                new LinesBuilder(),
                new LinesBuilder(),
                root.Injection.Tag != MdTag.ContextTag ? root.Injection.Tag : default,
                default,
                root.Node.Accumulators.ToImmutableArray());

            foreach (var perResolveVar in map.GetPerResolves())
            {
                ctx.Code.AppendLine($"var {perResolveVar.VariableName} = default({typeResolver.Resolve(perResolveVar.InstanceType)});");
                if (perResolveVar.Info.RefCount > 1 && perResolveVar.InstanceType.IsValueType)
                {
                    ctx.Code.AppendLine($"var {perResolveVar.VariableName}Created = false;");
                }
            }
            
            blockBuilder.Build(ctx, rootBlock);
            ctx.Code.AppendLine($"return {buildTools.OnInjected(ctx, rootBlock.Current)};");
            ctx.Code.AppendLines(ctx.LocalFunctionsCode.Lines);
            
            var args = GetRootArgs(map.Values).ToImmutableArray();
            var processedRoot = root with
            {
                Lines = ctx.Code.Lines.ToImmutableArray(),
                Args = args.GetArgsOfKind(ArgKind.Root).ToImmutableArray()
            };

            foreach (var rootArg in args)
            {
                allArgs.Add(rootArg);
            }
            
            var typeDescription = typeResolver.Resolve(processedRoot.Injection.Type);
            var isMethod = (processedRoot.Kind & RootKinds.Method) == RootKinds.Method 
                           || processedRoot.Args.Length > 0
                           || typeDescription.TypeArgs.Count > 0;

            processedRoot = processedRoot with
            {
                TypeDescription = typeDescription,
                IsMethod = isMethod
            };
            
            roots.Add(processedRoot);
            isThreadSafe |= isThreadSafeEnabled && root.Node.Accumulators.Count > 0;
            isThreadSafe |= isThreadSafeEnabled && map.IsThreadSafe();
            map.Reset();
        }

        var singletons = map.GetSingletons().ToImmutableArray();
        var totalDisposables =  singletons.Where(i => nodeInfo.IsDisposable(i.Node)).ToArray();
        var asyncDisposables =  singletons.Where(i => nodeInfo.IsAsyncDisposable(i.Node)).ToArray();
        var publicRoots = roots
            .OrderByDescending(i => i.IsPublic)
            .ThenBy(i => i.Node.Binding.Id)
            .ThenBy(i => i.DisplayName)
            .ToImmutableArray();
        
        var composition = new CompositionCode(
            graph,
            new LinesBuilder(),
            singletons,
            GetRootArgs(allArgs).ToImmutableArray(),
            publicRoots,
            totalDisposables.Length,
            asyncDisposables.Length,
            totalDisposables.Count(i => i.Node.Lifetime == Lifetime.Scoped),
            isThreadSafe,
            ImmutableArray<Line>.Empty);
        
        if (graph.Source.Hints.IsToStringEnabled)
        {
            var diagram = classDiagramBuilder.Build(composition);
            composition = composition with { Diagram = diagram.Lines.ToImmutableArray() };
        }

        return composition;
    }
    
    private static IEnumerable<Variable> GetRootArgs(IEnumerable<Variable> argVars) => 
        argVars
            .Where(arg => arg.Node.Arg is not null)
            .GroupBy(i => i.Node.Binding.Id)
            .Select(i => i.First())
            .OrderBy(i => i.Node.Binding.Id);
}