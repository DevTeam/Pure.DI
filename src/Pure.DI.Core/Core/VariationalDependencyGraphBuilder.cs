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
    private readonly IBuilder<MdSetup, IEnumerable<DependencyNode>>[] _dependencyNodeBuilders;
    private readonly IMarker _marker;
    private readonly IBuilder<MdBinding, ISet<Injection>> _injectionsBuilder;
    private readonly IDependencyGraphBuilder _graphBuilder;

    public VariationalDependencyGraphBuilder(
        IBuilder<MdSetup, IEnumerable<DependencyNode>>[] dependencyNodeBuilders,
        IMarker marker,
        IBuilder<MdBinding, ISet<Injection>> injectionsBuilder,
        IDependencyGraphBuilder graphBuilder)
    {
        _dependencyNodeBuilders = dependencyNodeBuilders;
        _marker = marker;
        _injectionsBuilder = injectionsBuilder;
        _graphBuilder = graphBuilder;
    }

    public DependencyGraph Build(MdSetup setup, CancellationToken cancellationToken)
    {
        var allNodes = _dependencyNodeBuilders
            .SelectMany(builder => builder.Build(setup, cancellationToken))
            .Select(i => new ProcessingNode(i, _marker, _injectionsBuilder));
        
        var variations = new LinkedList<Variation>(CreateVariations(allNodes));
        try
        {
            DependencyGraph? first = default;
            while (TryGetNextNodes(variations, out var nodes))
            {
                cancellationToken.ThrowIfCancellationRequested();
                var newNodes = _graphBuilder.TryBuild(setup, nodes, out var dependencyGraph, cancellationToken).ToArray();
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
            .Select(i => SortByPriority(i).GetEnumerator());

    private static IEnumerable<ProcessingNode> SortByPriority(IEnumerable<ProcessingNode> nodes) =>
        nodes
            .OrderBy(i => i.Node.Implementation?.Constructor.Ordinal ?? int.MaxValue)
            .ThenByDescending(i => i.Node.Implementation?.Constructor.Parameters.Count(p => !p.ParameterSymbol.IsOptional))
            .ThenByDescending(i => i.Node.Implementation?.Constructor.Method.DeclaredAccessibility);

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
                nodes.Add(enumerator.Current!);
                continue;
            }

            if (!enumerator.Current.HasNode)
            {
                enumerator.MoveNext();
            }
            
            nodes.Add(enumerator.Current!);
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