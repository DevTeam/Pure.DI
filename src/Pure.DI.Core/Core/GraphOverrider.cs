namespace Pure.DI.Core;

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
        var entries = new List<GraphEntry<DependencyNode, Dependency>>();
        var processedNodes = new Dictionary<DependencyNode, DependencyNode>();
        var nodesMap = new Dictionary<Injection, DependencyNode>();
        foreach (var rootNode in from node in graph.Vertices where node.Root is not null select node)
        {
            Override(processedNodes, [], nodesMap, [], setup, graph, rootNode, ref maxId, entries);
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
        Dictionary<DependencyNode, DependencyNode> processedNodes,
        HashSet<Injection> overriddenInjections,
        Dictionary<Injection, DependencyNode> nodesMap,
        Dictionary<int, DpOverride> overridesMap,
        MdSetup setup,
        IGraph<DependencyNode, Dependency> graph,
        DependencyNode targetNode,
        ref int maxId,
        List<GraphEntry<DependencyNode, Dependency>> entries)
    {
        if (processedNodes.TryGetValue(targetNode, out var node))
        {
            return node;
        }

        if (!graph.TryGetInEdges(targetNode, out var dependencies))
        {
            return targetNode;
        }

        IEnumerable<ImmutableArray<DpOverride>> overrides = [];
        if (targetNode.Factory is {} factory)
        {
            overridesMap = new Dictionary<int, DpOverride>(overridesMap);
            targetNode = targetNode with { Factory = factory with { OverridesMap = overridesMap } };
            overrides = factory.Resolvers.Select(i => (i.Source.Position, i.Overrides))
                .Concat(factory.Initializers.Select(i => (i.Source.Position, i.Overrides)))
                .OrderBy(i => i.Position)
                .Select(i => i.Overrides);
        }

        processedNodes.Add(targetNode, targetNode);

        var newDependencies = new List<Dependency>(dependencies.Count);
        var lastDependencyPosition = 0;
        using var overridesEnumerator = overrides.GetEnumerator();
        foreach (var dependency in dependencies)
        {
            if (cancellationToken.IsCancellationRequested)
            {
                return targetNode;
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
                        var currentOverride = new DpOverride(
                            @override.Source with { Id = overrideId, ContractType = contractType },
                            @override.Injections.Select(i => i with { Type = typeConstructor.Construct(setup, i.Type) }).ToImmutableArray());

                        overridesMap[@override.Source.Id] = currentOverride;
                        MdBinding? overrideBinding = null;
                        foreach (var injection in currentOverride.Injections)
                        {
                            if (cancellationToken.IsCancellationRequested)
                            {
                                return targetNode;
                            }

                            overriddenInjections.Add(injection);
                            if (nodesMap.TryGetValue(injection, out _))
                            {
                                continue;
                            }

                            overrideBinding ??= bindingsFactory.CreateConstructBinding(
                                setup,
                                targetNode,
                                injection,
                                contractType,
                                Lifetime.PerResolve,
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
            if (!overriddenInjections.Contains(currentDependency.Injection)
                || !nodesMap.TryGetValue(currentDependency.Injection, out var overridingSourceNode))
            {
                var source = Override(processedNodes, overriddenInjections, nodesMap, overridesMap, setup, graph, currentDependency.Source, ref maxId, entries);
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