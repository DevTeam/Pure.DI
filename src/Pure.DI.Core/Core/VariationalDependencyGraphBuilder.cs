// ReSharper disable ClassNeverInstantiated.Global
// ReSharper disable ForeachCanBePartlyConvertedToQueryUsingAnotherGetEnumerator
// ReSharper disable HeapView.ObjectAllocation.Evident
// ReSharper disable HeapView.ObjectAllocation.Possible
// ReSharper disable IdentifierTypo
// ReSharper disable LoopCanBeConvertedToQuery
namespace Pure.DI.Core;

using Variation = IEnumerator<ProcessingNode>;

internal sealed class VariationalDependencyGraphBuilder : IBuilder<MdSetup, DependencyGraph>
{
    private readonly ILogger<VariationalDependencyGraphBuilder> _logger;
    private readonly IGlobalOptions _globalOptions;
    private readonly IReadOnlyCollection<IBuilder<MdSetup, IEnumerable<DependencyNode>>> _dependencyNodeBuilders;
    private readonly IVariator<ProcessingNode> _variator;
    private readonly IMarker _marker;
    private readonly IBuilder<ContractsBuildContext, ISet<Injection>> _contractsBuilder;
    private readonly IDependencyGraphBuilder _graphBuilder;
    private readonly CancellationToken _cancellationToken;

    public VariationalDependencyGraphBuilder(
        ILogger<VariationalDependencyGraphBuilder> logger,
        IGlobalOptions globalOptions,
        IReadOnlyCollection<IBuilder<MdSetup, IEnumerable<DependencyNode>>> dependencyNodeBuilders,
        IVariator<ProcessingNode> variator,
        IMarker marker,
        IBuilder<ContractsBuildContext, ISet<Injection>> contractsBuilder,
        IDependencyGraphBuilder graphBuilder,
        CancellationToken cancellationToken)
    {
        _logger = logger;
        _globalOptions = globalOptions;
        _dependencyNodeBuilders = dependencyNodeBuilders;
        _variator = variator;
        _marker = marker;
        _contractsBuilder = contractsBuilder;
        _graphBuilder = graphBuilder;
        _cancellationToken = cancellationToken;
    }

    public DependencyGraph Build(MdSetup setup)
    {
        var rawNodes = SortByPriority(_dependencyNodeBuilders.SelectMany(builder => builder.Build(setup))).Reverse();
        var allNodes = new List<ProcessingNode>();
        var injections = new Dictionary<Injection, DependencyNode>();
        var allOverriddenInjections = new HashSet<Injection>();
        foreach (var node in rawNodes)
        {
            var contracts = _contractsBuilder.Build(new ContractsBuildContext(node.Binding, MdTag.ContextTag));
            var isRoot = node.Root is not null;
            if (!isRoot)
            {
                var overriddenInjections = new List<KeyValuePair<Injection, DependencyNode>>();
                foreach (var exposedInjection in contracts)
                {
                    if (!injections.TryGetValue(exposedInjection, out var currentNode) || currentNode.Binding == node.Binding)
                    {
                        injections[exposedInjection] = node;
                    }
                    else
                    {
                        overriddenInjections.Add(new KeyValuePair<Injection, DependencyNode>(exposedInjection, currentNode));
                    }
                }

                foreach (var item in overriddenInjections)
                {
                    contracts.Remove(item.Key);
                    if (!allOverriddenInjections.Add(item.Key))
                    {
                        continue;
                    }

                    if (node.Binding.SourceSetup.Kind != CompositionKind.Global)
                    {
                        _logger.CompileWarning($"{item.Key.ToString()} has been overridden.", item.Value.Binding.Source.GetLocation(), LogId.WarningOverriddenBinding);
                    }
                }
            }

            if (isRoot || contracts.Any())
            {
                allNodes.Add(new ProcessingNode(node, contracts, _marker));
            }
        }

        allNodes.Reverse();
        var variations = new LinkedList<Variation>(CreateVariations(allNodes));
        try
        {
            var maxIterations = _globalOptions.MaxIterations;
            DependencyGraph? first = default;
            while (_variator.TryGetNextVariants(variations, node => !node.HasNode, out var nodes))
            {
                if (maxIterations-- <= 0)
                {
                    _logger.CompileError($"The maximum number of iterations {_globalOptions.MaxIterations.ToString()} was exceeded when building the optimal dependency graph. Try to specify the dependency graph more accurately.", setup.Source.GetLocation(), LogId.ErrorInvalidMetadata);
                }
                
                _cancellationToken.ThrowIfCancellationRequested();

                var newNodes = SortByPriority(_graphBuilder.TryBuild(setup, nodes, out var dependencyGraph))
                    .Select(CreateProcessingNode)
                    .ToArray();

                if (newNodes.Any())
                {
                    foreach (var newVariant in CreateVariations(newNodes))
                    {
                        variations.AddFirst(newVariant);
                    }

                    continue;
                }

                if (dependencyGraph is { IsValid: true })
                {
                    return dependencyGraph;
                }

                first ??= dependencyGraph;
                continue;

                ProcessingNode CreateProcessingNode(DependencyNode dependencyNode) => new(
                    dependencyNode,
                    _contractsBuilder.Build(new ContractsBuildContext(dependencyNode.Binding, MdTag.ContextTag)),
                    _marker);
            }

            return first!;
        }
        finally
        {
            foreach (var variation in variations)
            {
                variation.Dispose();
            }
        }
    }

    private static IEnumerable<Variation> CreateVariations(IEnumerable<ProcessingNode> allNodes) =>
        allNodes.GroupBy(i => i.Node.Binding)
            .Select(i => i.GetEnumerator());

    private static IEnumerable<DependencyNode> SortByPriority(IEnumerable<DependencyNode> nodes) =>
        nodes
            .OrderBy(i => i.Implementation?.Constructor.Ordinal ?? int.MaxValue)
            .ThenByDescending(i => i.Implementation?.Constructor.Parameters.Count(p => !p.ParameterSymbol.IsOptional))
            .ThenByDescending(i => i.Implementation?.Constructor.Method.DeclaredAccessibility);
}