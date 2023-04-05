namespace Pure.DI.Core;

internal class RootsBuilder: IBuilder<DependencyGraph, IReadOnlyDictionary<Injection, Root>>
{
    private readonly IBuilder<ContractsBuildContext, ISet<Injection>> _contractsBuilder;

    public RootsBuilder(IBuilder<ContractsBuildContext, ISet<Injection>> contractsBuilder) =>
        _contractsBuilder = contractsBuilder;

    public IReadOnlyDictionary<Injection, Root> Build(DependencyGraph dependencyGraph, CancellationToken cancellationToken)
    {
        var rootsPairs = new List<KeyValuePair<Injection, Root>>();
        foreach (var curNode in dependencyGraph.Graph.Vertices)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var node = curNode;
            if (node.Type is INamedTypeSymbol { IsUnboundGenericType: true })
            {
                continue;
            }

            bool isRoot;
            var name = "";
            if (node.Root is { } root)
            {
                if (dependencyGraph.Graph.TryGetInEdges(node, out var rootDependencies) && rootDependencies.Length == 1)
                {
                    node = rootDependencies.Single().Source;
                    isRoot = true;
                    name = root.Source.Name;
                }
                else
                {
                    continue;
                }
            }
            else
            {
                isRoot = false;
            }

            // ReSharper disable once LoopCanBeConvertedToQuery
            foreach (var injection in _contractsBuilder.Build(new ContractsBuildContext(node.Binding, MdTag.ContextTag), cancellationToken))
            {
                rootsPairs.Add(new KeyValuePair<Injection, Root>(
                    injection,
                    new Root(
                        0,
                        node,
                        injection,
                        isRoot,
                        name,
                        ImmutableArray<Line>.Empty)));
            }
        }

        var index = 1;
        return rootsPairs
            .GroupBy(i => i.Key)
            .ToDictionary(
                i => i.Key,
                i => i.OrderByDescending(j => j.Value.IsPublic).Select(j => j.Value).First() with { Index = index++ });
    }
}