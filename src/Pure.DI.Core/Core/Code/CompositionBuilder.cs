// ReSharper disable InvertIf
// ReSharper disable ClassNeverInstantiated.Global
namespace Pure.DI.Core.Code;

internal class CompositionBuilder(
    IBuildTools buildTools,
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
                default,
                default);

            foreach (var perResolveVar in map.GetPerResolves())
            {
                ctx.Code.AppendLine($"var {perResolveVar.VariableName} = default({perResolveVar.InstanceType});");
                if (perResolveVar.Info.RefCount > 1 && perResolveVar.InstanceType.IsValueType)
                {
                    ctx.Code.AppendLine($"var {perResolveVar.VariableName}Created = false;");
                }
            }
            
            blockBuilder.Build(ctx, rootBlock);
            ctx.Code.AppendLine($"return {buildTools.OnInjected(ctx, rootBlock.Current)};");
            ctx.Code.AppendLines(ctx.LocalFunctionsCode.Lines);
            
            var args = map.Values
                .Where(i => i.Node.Arg is not null)
                .OrderBy(i => i.Node.Binding.Id)
                .ToImmutableArray();
            
            var rootArgs = args
                .Where(i => i.Node.Arg?.Source.Kind == ArgKind.Root)
                .ToImmutableArray();

            var processedRoot = root with
            {
                Lines = ctx.Code.Lines.ToImmutableArray(),
                Args = rootArgs
            };

            foreach (var rootArg in args)
            {
                allArgs.Add(rootArg);
            }
            
            roots.Add(processedRoot);
            isThreadSafe |= map.IsThreadSafe(graph.Source.Hints);
            map.Reset();
        }

        var singletons = map.GetSingletons().ToImmutableArray();
        var disposableSingletons =  singletons.Where(i => i.Node.IsDisposable()).ToArray();
        var publicRoots = roots
            .OrderByDescending(i => i.IsPublic)
            .ThenBy(i => i.Node.Binding.Id)
            .ThenBy(i => i.PropertyName)
            .ToImmutableArray();
        
        var composition = new CompositionCode(
            graph,
            new LinesBuilder(),
            singletons,
            allArgs.OrderBy(i => i.Node.Binding.Id).ToImmutableArray(),
            publicRoots,
            disposableSingletons.Length,
            disposableSingletons.Count(i => i.Node.Lifetime == Lifetime.Scoped),
            isThreadSafe,
            ImmutableArray<Line>.Empty);
        
        if (graph.Source.Hints.GetHint<SettingState>(Hint.ToString) == SettingState.On)
        {
            var diagram = classDiagramBuilder.Build(composition);
            composition = composition with { Diagram = diagram.Lines.ToImmutableArray() };
        }

        return composition;
    }
}