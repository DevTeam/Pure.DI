// ReSharper disable ClassNeverInstantiated.Global

namespace Pure.DI.Core;

internal sealed class RootsBuilder(IBuilder<ContractsBuildContext, ISet<Injection>> contractsBuilder)
    : IBuilder<DependencyGraph, IReadOnlyDictionary<Injection, Root>>
{
    public IReadOnlyDictionary<Injection, Root> Build(DependencyGraph dependencyGraph)
    {
        var rootsPairs = new List<KeyValuePair<Injection, Root>>();
        foreach (var curNode in dependencyGraph.Graph.Vertices)
        {
            var node = curNode;
            if (node.Type is INamedTypeSymbol { IsUnboundGenericType: true })
            {
                continue;
            }

            string name;
            RootKinds kind;
            MdRoot source;
            if (node.Root is { } root)
            {
                if (dependencyGraph.Graph.TryGetInEdges(node, out var rootDependencies) && rootDependencies.Count == 1)
                {
                    node = rootDependencies.Single().Source;
                    source = root.Source;
                    name = root.Source.Name;
                    kind = root.Source.Kind;
                }
                else
                {
                    continue;
                }
            }
            else
            {
                continue;
            }

            // ReSharper disable once LoopCanBeConvertedToQuery
            foreach (var injection in contractsBuilder.Build(new ContractsBuildContext(node.Binding, MdTag.ContextTag)).Where(i => i == root.Injection).Take(1))
            {
                rootsPairs.Add(new KeyValuePair<Injection, Root>(
                    injection,
                    new Root(
                        0,
                        node,
                        source,
                        injection,
                        name,
                        ImmutableArray<Line>.Empty,
                        ImmutableArray<Variable>.Empty,
                        kind)));
            }
        }

        var index = 1;
        return rootsPairs
            .GroupBy(i => i.Key)
            .ToDictionary(
                i => i.Key,
                i => CreateRoot(i, ref index));
    }

    private static Root CreateRoot(IEnumerable<KeyValuePair<Injection, Root>> routesGroup, ref int index) =>
        routesGroup
                .OrderByDescending(j => j.Value.IsPublic)
                .Select(j => j.Value)
                .First() with
            {
                Index = index++
            };
}