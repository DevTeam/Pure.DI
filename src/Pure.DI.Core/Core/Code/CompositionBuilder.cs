// ReSharper disable InvertIf
// ReSharper disable ClassNeverInstantiated.Global
namespace Pure.DI.Core.Code;

internal class CompositionBuilder: IBuilder<DependencyGraph, CompositionCode>
{
    private readonly IBuildTools _buildTools;
    private readonly IVariablesBuilder _variablesBuilder;
    private readonly ICodeBuilder<Block> _blockBuilder;
    private readonly ICodeBuilder<IStatement> _statementBuilder;
    private readonly CancellationToken _cancellationToken;

    public CompositionBuilder(
        IBuildTools buildTools,
        IVariablesBuilder variablesBuilder,
        ICodeBuilder<Block> blockBuilder,
        ICodeBuilder<IStatement> statementBuilder,
        CancellationToken cancellationToken)
    {
        _buildTools = buildTools;
        _variablesBuilder = variablesBuilder;
        _blockBuilder = blockBuilder;
        _statementBuilder = statementBuilder;
        _cancellationToken = cancellationToken;
    }

    public CompositionCode Build(DependencyGraph graph)
    {
        var roots = new List<Root>();
        var map = new VariablesMap();
        var allArgs = new HashSet<Variable>();
        foreach (var root in graph.Roots.Values)
        {
            _cancellationToken.ThrowIfCancellationRequested();
            var rootBlock = _variablesBuilder.Build(graph.Graph, map, root.Node, root.Injection);
            var ctx = new BuildContext(
                0,
                _buildTools,
                _statementBuilder,
                graph,
                rootBlock.Current,
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
            
            _blockBuilder.Build(ctx, rootBlock);
            ctx.Code.AppendLine($"return {_buildTools.OnInjected(ctx, rootBlock.Current)};");
            
            var args = map.Values
                .Where(i => i.Node.Arg is not null)
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
            map.Reset();
        }

        var singletons = map.GetSingletons().ToImmutableArray();
        var publicRoots = roots
            .OrderByDescending(i => i.IsPublic)
            .ThenBy(i => i.Node.Binding.Id)
            .ThenBy(i => i.PropertyName)
            .ToImmutableArray();
        
        return new CompositionCode(
            graph,
            new LinesBuilder(),
            singletons,
            allArgs.OrderBy(i => i.Node.Binding.Id).ToImmutableArray(),
            publicRoots,
            singletons.Count(i => i.Node.IsDisposable()));
    }
}