// ReSharper disable InvertIf
// ReSharper disable ClassNeverInstantiated.Global

namespace Pure.DI.Core.Code;

sealed class CompositionBuilder(
    IBuildTools buildTools,
    INodeInfo nodeInfo,
    ITypeResolver typeResolver,
    IVariablesBuilder variablesBuilder,
    ICodeBuilder<Block> blockBuilder,
    ICodeBuilder<IStatement> statementBuilder,
    IBuilder<CompositionCode, LinesBuilder> classDiagramBuilder,
    Func<IVariablesMap> variablesMapFactory,
    IVariableNameProvider variableNameProvider,
    IOverridesRegistry overridesRegistry,
    CancellationToken cancellationToken)
    : IBuilder<DependencyGraph, CompositionCode>
{
    public CompositionCode Build(DependencyGraph graph)
    {
        var roots = new List<Root>();
        var allArgs = new HashSet<Variable>();
        var isThreadSafe = false;
        var isThreadSafeEnabled = graph.Source.Hints.IsThreadSafeEnabled;
        var map = variablesMapFactory();
        foreach (var root in graph.Roots)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var rootBlock = variablesBuilder.Build(graph, map, root.Node, root.Injection);
            var ctx = new BuildContext(
                root,
                0,
                buildTools,
                statementBuilder,
                graph,
                rootBlock.Current,
                new LinesBuilder(),
                new LinesBuilder(),
                root.Injection.Tag != MdTag.ContextTag ? root.Injection.Tag : null,
                null,
                root.Node.Accumulators.ToImmutableArray());

            foreach (var perResolveVar in map.GetPerResolves())
            {
                ctx.Code.AppendLine($"var {perResolveVar.VariableName} = default({typeResolver.Resolve(graph.Source, perResolveVar.InstanceType)});");
                if (perResolveVar.Info.RefCount > 0
                    && perResolveVar.InstanceType.IsValueType)
                {
                    ctx.Code.AppendLine($"var {perResolveVar.VariableName}Created = false;");
                }
            }

            var bodyCode = new LinesBuilder();
            blockBuilder.Build(ctx with { Code = bodyCode }, rootBlock);
            foreach (var @override in overridesRegistry.GetOverrides(root))
            {
                var variableName = variableNameProvider.GetOverrideVariableName(@override.Source);
                ctx.Code.AppendLine($"{typeResolver.Resolve(graph.Source, @override.Source.ContractType)} {variableName};");
            }

            bodyCode.AppendLine($"return {buildTools.OnInjected(ctx, rootBlock.Current)};");
            bodyCode.AppendLines(ctx.LocalFunctionsCode.Lines);
            ctx.Code.AppendLines(bodyCode.Lines);

            var args = GetRootArgs(map.Values).ToList();
            var processedRoot = root with
            {
                Lines = ctx.Code.Lines.ToImmutableArray(),
                Args = args
                    .GetArgsOfKind(ArgKind.Root)
                    .OrderBy(i => !(i.Node.Arg?.Source.IsBuildUpInstance ?? false))
                    .ThenBy(i => i.Node.Binding.Id)
                    .ToImmutableArray()
            };

            foreach (var rootArg in args)
            {
                allArgs.Add(rootArg);
            }

            var typeDescription = typeResolver.Resolve(graph.Source, processedRoot.Injection.Type);
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
            isThreadSafe |= isThreadSafeEnabled && map.IsThreadSafe;
            map.Reset();
        }

        var singletons = map.GetSingletons().ToImmutableArray();
        var totalDisposables = singletons.Where(i => nodeInfo.IsDisposableAny(i.Node)).ToList();
        var disposables = singletons.Where(i => nodeInfo.IsDisposable(i.Node)).ToList();
        var asyncDisposables = singletons.Where(i => nodeInfo.IsAsyncDisposable(i.Node)).ToList();
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
            totalDisposables.Count,
            disposables.Count,
            asyncDisposables.Count,
            totalDisposables.Count(i => i.Node.Lifetime == Lifetime.Scoped),
            isThreadSafe,
            ImmutableArray<Line>.Empty);

        var diagram = classDiagramBuilder.Build(composition);
        return composition with { Diagram = diagram.Lines.ToImmutableArray() };
    }

    private static IEnumerable<Variable> GetRootArgs(IEnumerable<Variable> argVars) =>
        argVars
            .Where(arg => arg.Node.Arg is not null)
            .GroupBy(i => i.Node.Binding.Id)
            .Select(i => i.First())
            .OrderBy(i => i.Node.Binding.Id);
}