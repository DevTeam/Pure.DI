namespace Pure.DI.Core;

class GraphOverrider(
    INodesFactory nodesFactory,
    IBindingsFactory bindingsFactory,
    CancellationToken cancellationToken)
    : IGraphRewriter
{
    public IGraph<DependencyNode, Dependency> Rewrite(
        MdSetup setup,
        IGraph<DependencyNode, Dependency> graph,
        ref int maxId)
    {
        var overridingEntries = new List<GraphEntry<DependencyNode, Dependency>>();
        var processedNodes = new HashSet<DependencyNode>();
        var overrideMap = new Dictionary<Injection, DependencyNode>();
        foreach (var rootNode in from node in graph.Vertices where node.Root is not null select node)
        {
            Override(processedNodes, [], overrideMap, setup, graph, rootNode, ref maxId, overridingEntries);
            if (cancellationToken.IsCancellationRequested)
            {
                return graph;
            }
        }

        if (overridingEntries.Count == 0)
        {
            return graph;
        }

        var entriesMap = new Dictionary<DependencyNode, IReadOnlyCollection<Dependency>>();
        foreach (var entry in graph.Entries)
        {
            entriesMap[entry.Target] = entry.Edges;
            if (cancellationToken.IsCancellationRequested)
            {
                return graph;
            }
        }

        foreach (var overridingEntry in overridingEntries)
        {
            entriesMap[overridingEntry.Target] = overridingEntry.Edges;
            if (cancellationToken.IsCancellationRequested)
            {
                return graph;
            }
        }

        return new Graph<DependencyNode, Dependency>(
            entriesMap.Select(i => new GraphEntry<DependencyNode, Dependency>(i.Key, i.Value)));
    }

    private void Override(
        HashSet<DependencyNode> processedNodes,
        HashSet<Injection> overriddenInjections,
        Dictionary<Injection, DependencyNode> overrideMap,
        MdSetup setup,
        IGraph<DependencyNode, Dependency> graph,
        DependencyNode targetNode,
        ref int maxId,
        List<GraphEntry<DependencyNode, Dependency>> overridingEntries)
    {
        if (!processedNodes.Add(targetNode))
        {
            return;
        }

        if (!graph.TryGetInEdges(targetNode, out var dependencies))
        {
            return;
        }

        IEnumerable<ImmutableArray<DpOverride>> overrides = [];
        if (targetNode.Factory is {} factory)
        {
            overrides = factory.Resolvers.Select(i => (i.Source.Position, i.Overrides))
                .Concat(factory.Initializers.Select(i => (i.Source.Position, i.Overrides)))
                .OrderBy(i => i.Position)
                .Select(i => i.Overrides)
                .ToList();
        }

        var newDependencies = new List<Dependency>(dependencies.Count);
        var isOverridden = false;
        using var overridesEnumerator = overrides.GetEnumerator();
        var lastDependencyPosition = 0;
        foreach (var dependency in dependencies)
        {
            if (cancellationToken.IsCancellationRequested)
            {
                return;
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

                        MdBinding? overrideBinding = null;
                        foreach (var overrideInjection in @override.Injections)
                        {
                            if (cancellationToken.IsCancellationRequested)
                            {
                                return;
                            }

                            var typeConstructor = targetNode.TypeConstructor;
                            var injection = overrideInjection with { Type = typeConstructor.Construct(setup, overrideInjection.Type) };
                            var contractType = typeConstructor.Construct(setup, @override.Source.ContractType);
                            overriddenInjections.Add(injection);
                            if (overrideMap.TryGetValue(injection, out _))
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
                                state: @override);

                            foreach (var sourceNode in nodesFactory.CreateNodes(setup, typeConstructor, overrideBinding))
                            {
                                overrideMap[injection] = sourceNode;
                            }
                        }
                    }
                }
            }

            if (!overriddenInjections.Contains(dependency.Injection) || !overrideMap.TryGetValue(dependency.Injection, out var overridingSourceNode))
            {
                Override(processedNodes, overriddenInjections, overrideMap, setup, graph, dependency.Source, ref maxId, overridingEntries);
                newDependencies.Add(dependency);
                continue;
            }

            isOverridden = true;
            var newDependency = dependency with
            {
                Injection = dependency.Injection with { Kind = InjectionKind.Override },
                Source = overridingSourceNode,
                IsResolved = true
            };

            newDependencies.Add(newDependency);
        }

        if (isOverridden)
        {
            overridingEntries.Add(new GraphEntry<DependencyNode, Dependency>(targetNode, newDependencies));
        }
    }
}