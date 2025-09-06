// ReSharper disable ClassNeverInstantiated.Global

namespace Pure.DI.Core;

sealed class RootsBuilder(
    IBuilder<ContractsBuildContext, ISet<Injection>> contractsBuilder)
    : IBuilder<DependencyGraph, DependencyGraph>
{
    public DependencyGraph Build(DependencyGraph dependencyGraph)
    {
        var rootsPairs = new List<KeyValuePair<Injection, Root>>();
        foreach (var curNode in dependencyGraph.Graph.Vertices)
        {
            var node = curNode;
            if (node.Type is INamedTypeSymbol { IsUnboundGenericType: true })
            {
                continue;
            }

            if (node.Root is {} root
                && dependencyGraph.Graph.TryGetInEdges(node, out var rootDependencies) && rootDependencies.Count == 1)
            {
                node = rootDependencies.Single().Source;
            }
            else
            {
                continue;
            }

            // ReSharper disable once LoopCanBeConvertedToQuery
            foreach (var injection in contractsBuilder.Build(new ContractsBuildContext(node.Binding, MdTag.ContextTag, root.Injection.Tag)).Where(i => i == root.Injection).Take(1))
            {
                var rootInjection = ReferenceEquals(injection.Tag, MdTag.ContextTag) ? injection with { Tag = null } : injection;
                rootsPairs.Add(new KeyValuePair<Injection, Root>(
                    rootInjection,
                    new Root(
                        0,
                        node,
                        root.Source,
                        rootInjection,
                        root.Source.Name,
                        new Lines(),
                        ImmutableArray<VarDeclaration>.Empty,
                        root.Source.Kind)));
            }
        }

        var roots = rootsPairs
            .GroupBy(i => i.Key)
            .Select((byInjection, index) => (byInjection, index))
            .Select(group => CreateRoot(group))
            .ToImmutableArray();

        return dependencyGraph with { Roots = roots };
    }

    private static Root CreateRoot((IEnumerable<KeyValuePair<Injection, Root>> byInjection, int index) group) =>
        group.byInjection
                .OrderByDescending(j => j.Value.IsPublic)
                .Select(j => j.Value)
                .First() with { Index = group.index + 1 };
}