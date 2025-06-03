namespace Pure.DI.Core;

using System.Collections.Concurrent;

class GraphOverrider(
    INodesFactory nodesFactory,
    IBindingsFactory bindingsFactory,
    IOverrideIdProvider overrideIdProvider,
    CancellationToken cancellationToken)
    : IGraphRewriter
{
    public IGraph<DependencyNode, Dependency> Rewrite(
        MdSetup setup,
        IGraph<DependencyNode, Dependency> graph,
        ref int maxId)
    {
        var entries = new List<GraphEntry<DependencyNode, Dependency>>(graph.Entries.Count);
        var processed = new Dictionary<int, DependencyNode>(graph.Entries.Count);
        foreach (var rootNode in from node in graph.Vertices where node.Root is not null select node)
        {
            Override(processed, new Dictionary<Injection, DependencyNode>(), new Dictionary<int, DpOverride>(), setup, graph, rootNode, rootNode, ref maxId, entries);
            if (cancellationToken.IsCancellationRequested)
            {
                return graph;
            }
        }

        if (entries.Count == 0)
        {
            return graph;
        }

        var entriesMap = entries.ToDictionary(i => i.Target, i => i.Edges);
        foreach (var entry in graph.Entries.Where(i => !entriesMap.ContainsKey(i.Target)))
        {
            entriesMap.Add(entry.Target, entry.Edges);
        }

        return new Graph<DependencyNode, Dependency>(
            entriesMap.Select(i => new GraphEntry<DependencyNode, Dependency>(i.Key, i.Value)));
    }

    private DependencyNode Override(
        IDictionary<int, DependencyNode> processed,
        IReadOnlyDictionary<Injection, DependencyNode> nodes,
        IReadOnlyDictionary<int, DpOverride> overrides,
        MdSetup setup,
        IGraph<DependencyNode, Dependency> graph,
        DependencyNode rootNode,
        DependencyNode targetNode,
        ref int maxId,
        List<GraphEntry<DependencyNode, Dependency>> entries)
    {
        if (!graph.TryGetInEdges(targetNode, out var dependencies))
        {
            return targetNode;
        }

        if (processed.TryGetValue(targetNode.Binding.Id, out var node))
        {
            return node;
        }

        var nodesMap = nodes.ToDictionary();
        var overridesMap = overrides.ToDictionary();
        IEnumerable<ImmutableArray<DpOverride>> overridesEnumerable;
        if (targetNode.Factory is {} factory)
        {

            targetNode = targetNode with { Factory = factory with { OverridesMap = overridesMap } };
            overridesEnumerable = factory.Resolvers.Select(i => (i.Source.Position, i.Overrides))
                .Concat(factory.Initializers.Select(i => (i.Source.Position, i.Overrides)))
                .OrderBy(i => i.Position)
                .Select(i => i.Overrides);
        }
        else
        {
            overridesEnumerable = [];
        }

        processed.Add(targetNode.Binding.Id, targetNode);
        var newDependencies = new List<Dependency>(dependencies.Count);
        var lastDependencyPosition = 0;
        using var overridesEnumerator = overridesEnumerable.GetEnumerator();
        foreach (var dependency in dependencies)
        {
            if (cancellationToken.IsCancellationRequested)
            {
                return targetNode;
            }

            if (targetNode.Root is not null)
            {
                rootNode = dependency.Source;
            }

            if (dependency.Position.HasValue)
            {
                while (lastDependencyPosition < dependency.Position && overridesEnumerator.MoveNext())
                {
                    lastDependencyPosition++;
                    foreach (var @override in overridesEnumerator.Current)
                    {
                        if (@override.Injections.IsDefaultOrEmpty)
                        {
                            continue;
                        }

                        var typeConstructor = targetNode.TypeConstructor;
                        var contractType = typeConstructor.Construct(setup, @override.Source.ContractType);
                        var overrideId = overrideIdProvider.GetId(contractType, @override.Source.Tags);
                        var injections = @override.Injections.Select(i => i with { Type = typeConstructor.Construct(setup, i.Type) }).ToImmutableArray();
                        var currentOverride = new DpOverride(@override.Source with { Id = overrideId, ContractType = contractType }, injections);
                        overridesMap[@override.Source.Id] = currentOverride;
                        MdBinding? overrideBinding = null;
                        foreach (var injection in injections)
                        {
                            if (cancellationToken.IsCancellationRequested)
                            {
                                return targetNode;
                            }

                            if (nodesMap.ContainsKey(injection))
                            {
                                continue;
                            }

                            overrideBinding ??= bindingsFactory.CreateConstructBinding(
                                setup,
                                targetNode,
                                injection,
                                contractType,
                                Lifetime.Transient,
                                typeConstructor,
                                ++maxId,
                                MdConstructKind.Override,
                                state: currentOverride);

                            foreach (var sourceNode in nodesFactory.CreateNodes(setup, typeConstructor, overrideBinding))
                            {
                                nodesMap[injection] = sourceNode;
                            }
                        }
                    }
                }
            }

            var currentDependency = dependency with { Target = targetNode };
            if (!nodesMap.TryGetValue(currentDependency.Injection, out var overridingSourceNode))
            {
                var source = Override(processed, nodesMap, overridesMap, setup, graph, rootNode, currentDependency.Source, ref maxId, entries);
                currentDependency = currentDependency with { Source = source };
            }
            else
            {
                currentDependency = currentDependency with
                {
                    Injection = currentDependency.Injection with { Kind = InjectionKind.Override },
                    Source = overridingSourceNode,
                    IsResolved = true
                };
            }

            newDependencies.Add(currentDependency);
        }

        entries.Add(new GraphEntry<DependencyNode, Dependency>(targetNode, newDependencies));
        return targetNode;
    }
}