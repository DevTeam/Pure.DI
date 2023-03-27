// ReSharper disable ClassNeverInstantiated.Global
// ReSharper disable ForeachCanBePartlyConvertedToQueryUsingAnotherGetEnumerator
// ReSharper disable HeapView.ObjectAllocation.Evident
// ReSharper disable HeapView.ObjectAllocation.Possible
// ReSharper disable IdentifierTypo
// ReSharper disable LoopCanBeConvertedToQuery
namespace Pure.DI.Core;

using Variation = IEnumerator<ProcessingNode>;

internal class VariationalDependencyGraphBuilder : IBuilder<MdSetup, DependencyGraph>
{
    private readonly ILogger<VariationalDependencyGraphBuilder> _logger;
    private readonly IBuilder<MdSetup, IEnumerable<DependencyNode>>[] _dependencyNodeBuilders;
    private readonly IMarker _marker;
    private readonly IBuilder<MdBinding, ISet<Injection>> _injectionsBuilder;
    private readonly IDependencyGraphBuilder _graphBuilder;

    public VariationalDependencyGraphBuilder(
        ILogger<VariationalDependencyGraphBuilder> logger,
        IBuilder<MdSetup, IEnumerable<DependencyNode>>[] dependencyNodeBuilders,
        IMarker marker,
        IBuilder<MdBinding, ISet<Injection>> injectionsBuilder,
        IDependencyGraphBuilder graphBuilder)
    {
        _logger = logger;
        _dependencyNodeBuilders = dependencyNodeBuilders;
        _marker = marker;
        _injectionsBuilder = injectionsBuilder;
        _graphBuilder = graphBuilder;
    }

    public DependencyGraph Build(MdSetup setup, CancellationToken cancellationToken)
    {
        var rawNodes = SortByPriority(_dependencyNodeBuilders.SelectMany(builder => builder.Build(setup, cancellationToken))).Reverse().ToArray();
        var allNodes = new List<ProcessingNode>();
        var injections = new Dictionary<Injection, DependencyNode>();
        var allOverriddenInjections = new HashSet<Injection>();
        foreach (var node in rawNodes)
        {
            var exposedInjections = _injectionsBuilder.Build(node.Binding, cancellationToken);
            var isRoot = node.Root is { };
            if (!isRoot)
            {
                var overriddenInjections = new List<KeyValuePair<Injection, DependencyNode>>();
                foreach (var exposedInjection in exposedInjections)
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

                foreach (var (overriddenInjection, prevNode) in overriddenInjections)
                {
                    exposedInjections.Remove(overriddenInjection);
                    if (allOverriddenInjections.Add(overriddenInjection))
                    {
                        _logger.CompileWarning($"{overriddenInjection} has been overridden.", prevNode.Binding.Source.GetLocation(), LogId.WarningOverriddenBinding);
                    }
                }
            }

            if (isRoot || exposedInjections.Any())
            {
                allNodes.Add(new ProcessingNode(node, exposedInjections, _marker));
            }
        }

        allNodes.Reverse();
        var variations = new LinkedList<Variation>(CreateVariations(allNodes));
        try
        {
            DependencyGraph? first = default;
            while (TryGetNextNodes(variations, out var nodes))
            {
                cancellationToken.ThrowIfCancellationRequested();
                var newNodes = SortByPriority(_graphBuilder.TryBuild(setup, nodes, out var dependencyGraph, cancellationToken))
                    .Select(i => new ProcessingNode(i, _injectionsBuilder.Build(i.Binding, cancellationToken), _marker))
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

    private static bool TryGetNextNodes(
        LinkedList<Variation> variants,
        [NotNullWhen(true)] out IReadOnlyCollection<ProcessingNode>? nodeSet)
    {
        var hasNext = false;
        var nodes = new List<ProcessingNode>();
        foreach (var enumerator in variants)
        {
            if (!hasNext && enumerator.MoveNext())
            {
                hasNext = true;
                nodes.Add(enumerator.Current);
                continue;
            }

            if (!enumerator.Current.HasNode)
            {
                enumerator.MoveNext();
            }
            
            nodes.Add(enumerator.Current);
        }

        if (hasNext)
        {
            nodeSet = nodes;
            return hasNext;
        }

        nodeSet = default;
        return false;
    }
}