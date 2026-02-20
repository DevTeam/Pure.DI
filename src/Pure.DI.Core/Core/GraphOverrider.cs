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
        ref int bindingId)
    {
        var entries = new List<GraphEntry<DependencyNode, Dependency>>(graph.Entries.Count);
        var processed = new Dictionary<int, DependencyNode>(graph.Entries.Count);
        foreach (var rootNode in from node in graph.Vertices where node.Root is not null select node)
        {
            Override(
                processed,
                new Dictionary<Injection, DependencyNode>(),
                new Dictionary<Injection, DependencyNode>(),
                false,
                new Dictionary<int, DpOverride>(),
                setup,
                graph,
                rootNode,
                rootNode,
                ref bindingId,
                entries);
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
        IReadOnlyDictionary<Injection, DependencyNode> localOverrides,
        bool consumeLocalOverrides,
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

        // Rewritten nodes are context-dependent when any override scope is active.
        // In such cases we isolate memoization to the current branch to avoid
        // leaking context-dependent rewrites into sibling branches.
        var isContextFree = !consumeLocalOverrides
                            && nodes.Count == 0
                            && localOverrides.Count == 0
                            && overrides.Count == 0;
        var branchProcessed = isContextFree ? processed : processed.ToDictionary();
        if (branchProcessed.TryGetValue(targetNode.Binding.Id, out var node))
        {
            return node;
        }

        var nodesMap = nodes.ToDictionary();
        var localNodesMap = nodesMap.ToDictionary();
        if (consumeLocalOverrides && localOverrides.Count > 0)
        {
            foreach (var pair in localOverrides)
            {
                localNodesMap[pair.Key] = pair.Value;
            }
        }

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

        branchProcessed[targetNode.Binding.Id] = targetNode;
        var newDependencies = new List<Dependency>(dependencies.Count);
        var lastDependencyPosition = 0;
        using var overridesEnumerator = overridesEnumerable.GetEnumerator();
        var nextLocalOverrides = new Dictionary<Injection, DependencyNode>();
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
                        var isDeepOverride = currentOverride.Source.IsDeep;
                        foreach (var injection in injections)
                        {
                            if (cancellationToken.IsCancellationRequested)
                            {
                                return targetNode;
                            }

                            if (localNodesMap.ContainsKey(injection))
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
                                if (isDeepOverride)
                                {
                                    nodesMap[injection] = sourceNode;
                                }
                                else
                                {
                                    nextLocalOverrides[injection] = sourceNode;
                                }

                                localNodesMap[injection] = sourceNode;
                            }
                        }
                    }
                }
            }

            var currentDependency = dependency with { Target = targetNode };
            if (!localNodesMap.TryGetValue(currentDependency.Injection, out var overridingSourceNode))
            {
                var sourceOverrides = overridesMap.ToDictionary();
                var source = Override(
                    branchProcessed,
                    nodesMap,
                    nextLocalOverrides,
                    nextLocalOverrides.Count > 0,
                    sourceOverrides,
                    setup,
                    graph,
                    rootNode,
                    currentDependency.Source,
                    ref maxId,
                    entries);
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

        var entry = new GraphEntry<DependencyNode, Dependency>(targetNode, newDependencies);
        var entryIndex = entries.FindIndex(i => Equals(i.Target, targetNode));
        if (entryIndex >= 0)
        {
            entries[entryIndex] = entry;
        }
        else
        {
            entries.Add(entry);
        }

        return targetNode;
    }
}
