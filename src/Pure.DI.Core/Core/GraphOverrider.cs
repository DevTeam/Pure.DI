namespace Pure.DI.Core;

using System.Runtime.CompilerServices;

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
        var entries = new Dictionary<DependencyNode, IReadOnlyCollection<Dependency>>(graph.Entries.Count);
        var processed = new Dictionary<int, DependencyNode>(graph.Entries.Count);
        var state = new RewriteState(processed, new ConsumedInjections(setup, graph));
        foreach (var rootNode in from node in graph.Vertices where node.Root is not null select node)
        {
            Override(
                state,
                0,
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

        foreach (var graphEntry in graph.Entries)
        {
            if (entries.ContainsKey(graphEntry.Target))
            {
                continue;
            }

            entries[graphEntry.Target] = graphEntry.Edges;
        }

        return new Graph<DependencyNode, Dependency>(entries.Select(i => new GraphEntry<DependencyNode, Dependency>(i.Key, i.Value)));
    }

    private DependencyNode Override(
        RewriteState state,
        int depth,
        Dictionary<int, DependencyNode> processed,
        Dictionary<Injection, DependencyNode> nodes,
        Dictionary<Injection, DependencyNode> localOverrides,
        bool consumeLocalOverrides,
        Dictionary<int, DpOverride> overrides,
        MdSetup setup,
        IGraph<DependencyNode, Dependency> graph,
        DependencyNode rootNode,
        DependencyNode targetNode,
        ref int maxId,
        Dictionary<DependencyNode, IReadOnlyCollection<Dependency>> entries)
    {
        if (!graph.TryGetInEdges(targetNode, out var dependencies))
        {
            return targetNode;
        }

        var bindingId = targetNode.Binding.Id;
        if (nodes.Count > 0 || consumeLocalOverrides || overrides.Count > 0)
        {
            // Overrides that nothing under the node consumes do not change its rewrite,
            // so the node is rewritten once for all the scopes that differ only in them, see #175.
            var consumedInjections = state.ConsumedInjections;
            var consumedIds = consumedInjections.Get(targetNode);
            nodes = GetConsumedOverrides(nodes, consumedInjections, consumedIds);
            if (consumeLocalOverrides)
            {
                localOverrides = GetConsumedOverrides(localOverrides, consumedInjections, consumedIds);
                consumeLocalOverrides = localOverrides.Count > 0;
            }

            overrides = GetConsumedOverrides(overrides, consumedInjections, consumedIds);
            if (nodes.Count == 0 && !consumeLocalOverrides && overrides.Count == 0)
            {
                processed = state.Processed;
            }
        }

        var branchProcessed = CreateBranchProcessedCache(
            processed,
            consumeLocalOverrides,
            nodes,
            localOverrides,
            overrides);

        if (branchProcessed.TryGetValue(bindingId, out var node))
        {
            if (state.PathDepths.TryGetValue(bindingId, out var ancestorDepth))
            {
                state.MinBackReferenceDepth = Math.Min(state.MinBackReferenceDepth, ancestorDepth);
            }

            return node;
        }

        // The rewrite of a node depends only on the override scope it is reached in,
        // so a node reached by several dependency paths in the same scope is rewritten once, see #172.
        var context = ReferenceEquals(branchProcessed, processed)
            ? null
            : new OverrideContext(bindingId, nodes, consumeLocalOverrides ? localOverrides : null, overrides);

        if (context is not null && state.Rewritten.TryGetValue(context, out node))
        {
            return node;
        }

        var outerMinBackReferenceDepth = state.MinBackReferenceDepth;
        state.MinBackReferenceDepth = int.MaxValue;
        state.PathDepths[bindingId] = depth;
        var rewrittenNode = RewriteNode(
            state,
            depth,
            branchProcessed,
            nodes,
            localOverrides,
            consumeLocalOverrides,
            overrides,
            setup,
            graph,
            dependencies,
            rootNode,
            targetNode,
            ref maxId,
            entries);

        state.PathDepths.Remove(bindingId);
        var minBackReferenceDepth = state.MinBackReferenceDepth;
        state.MinBackReferenceDepth = Math.Min(outerMinBackReferenceDepth, minBackReferenceDepth);
        // A rewrite that refers back to a node above it on the current path belongs to this path only.
        if (context is not null && minBackReferenceDepth >= depth && !cancellationToken.IsCancellationRequested)
        {
            state.Rewritten[context] = rewrittenNode;
        }

        return rewrittenNode;
    }

    private DependencyNode RewriteNode(
        RewriteState state,
        int depth,
        Dictionary<int, DependencyNode> branchProcessed,
        Dictionary<Injection, DependencyNode> nodes,
        Dictionary<Injection, DependencyNode> localOverrides,
        bool consumeLocalOverrides,
        Dictionary<int, DpOverride> overrides,
        MdSetup setup,
        IGraph<DependencyNode, Dependency> graph,
        IReadOnlyCollection<Dependency> dependencies,
        DependencyNode rootNode,
        DependencyNode targetNode,
        ref int maxId,
        Dictionary<DependencyNode, IReadOnlyCollection<Dependency>> entries)
    {
        var nodesMap = new Dictionary<Injection, DependencyNode>(nodes);
        var localNodesMap = CreateLocalNodesMap(nodesMap, localOverrides, consumeLocalOverrides);

        var overridesMap = new Dictionary<int, DpOverride>(overrides);
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
            if (!TryGetOverride(localNodesMap, currentDependency.Injection, out var overridingSourceNode))
            {
                var sourceOverrides = new Dictionary<int, DpOverride>(overridesMap);
                var source = Override(
                    state,
                    depth + 1,
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

        entries[targetNode] = newDependencies;
        return targetNode;
    }

    private static Dictionary<int, DependencyNode> CreateBranchProcessedCache(
        Dictionary<int, DependencyNode> processed,
        bool consumeLocalOverrides,
        Dictionary<Injection, DependencyNode> nodes,
        Dictionary<Injection, DependencyNode> localOverrides,
        Dictionary<int, DpOverride> overrides)
    {
        // Rewritten nodes are context-dependent when any override scope is active.
        // In such cases we isolate memoization to the current branch to avoid
        // leaking context-dependent rewrites into sibling branches.
        var isContextFree = !consumeLocalOverrides
                            && nodes.Count == 0
                            && localOverrides.Count == 0
                            && overrides.Count == 0;

        return isContextFree ? processed : new Dictionary<int, DependencyNode>(processed);
    }

    private static Dictionary<Injection, DependencyNode> CreateLocalNodesMap(
        Dictionary<Injection, DependencyNode> nodes,
        Dictionary<Injection, DependencyNode> localOverrides,
        bool consumeLocalOverrides)
    {
        var localNodesMap = new Dictionary<Injection, DependencyNode>(nodes);
        if (!consumeLocalOverrides || localOverrides.Count <= 0)
        {
            return localNodesMap;
        }

        foreach (var pair in localOverrides)
        {
            localNodesMap[pair.Key] = pair.Value;
        }

        return localNodesMap;
    }

    private static bool TryGetOverride(
        Dictionary<Injection, DependencyNode> localNodesMap,
        Injection injection,
        [NotNullWhen(true)] out DependencyNode? overridingSourceNode)
    {
        if (localNodesMap.TryGetValue(injection, out overridingSourceNode))
        {
            return true;
        }

        if (injection.Type is { IsReferenceType: true, NullableAnnotation: NullableAnnotation.Annotated })
        {
            return localNodesMap.TryGetValue(
                injection with { Type = injection.Type.WithNullableAnnotation(NullableAnnotation.NotAnnotated) },
                out overridingSourceNode);
        }

        return false;
    }

    private static Dictionary<Injection, DependencyNode> GetConsumedOverrides(
        Dictionary<Injection, DependencyNode> overrides,
        ConsumedInjections consumedInjections,
        HashSet<int> consumedIds)
    {
        Dictionary<Injection, DependencyNode>? consumedOverrides = null;
        foreach (var pair in overrides)
        {
            if (!consumedInjections.IsConsumed(consumedIds, pair.Key))
            {
                consumedOverrides ??= new Dictionary<Injection, DependencyNode>(overrides);
                consumedOverrides.Remove(pair.Key);
            }
        }

        return consumedOverrides ?? overrides;
    }

    private static Dictionary<int, DpOverride> GetConsumedOverrides(
        Dictionary<int, DpOverride> overrides,
        ConsumedInjections consumedInjections,
        HashSet<int> consumedIds)
    {
        Dictionary<int, DpOverride>? consumedOverrides = null;
        foreach (var pair in overrides)
        {
            if (!pair.Value.Injections.Any(injection => consumedInjections.IsConsumed(consumedIds, injection)))
            {
                consumedOverrides ??= new Dictionary<int, DpOverride>(overrides);
                consumedOverrides.Remove(pair.Key);
            }
        }

        return consumedOverrides ?? overrides;
    }

    private sealed class RewriteState(
        Dictionary<int, DependencyNode> processed,
        ConsumedInjections consumedInjections)
    {
        // Nodes rewritten outside any override scope.
        public readonly Dictionary<int, DependencyNode> Processed = processed;

        public readonly ConsumedInjections ConsumedInjections = consumedInjections;

        // Nodes rewritten in a non-empty override scope.
        public readonly Dictionary<OverrideContext, DependencyNode> Rewritten = new();

        // Depths of the nodes being rewritten on the current path, by binding id.
        public readonly Dictionary<int, int> PathDepths = new();

        // The smallest depth of a node on the current path that the rewrite has referred back to.
        public int MinBackReferenceDepth = int.MaxValue;
    }

    private sealed class OverrideContext : IEquatable<OverrideContext>
    {
        private readonly int _bindingId;
        private readonly Dictionary<Injection, DependencyNode> _nodes;
        private readonly Dictionary<Injection, DependencyNode>? _localOverrides;
        private readonly Dictionary<int, DpOverride> _overrides;
        private readonly int _hashCode;

        public OverrideContext(
            int bindingId,
            Dictionary<Injection, DependencyNode> nodes,
            Dictionary<Injection, DependencyNode>? localOverrides,
            Dictionary<int, DpOverride> overrides)
        {
            // The scope dictionaries are copied, because a caller keeps adding to them after the call.
            _bindingId = bindingId;
            _nodes = new Dictionary<Injection, DependencyNode>(nodes);
            _localOverrides = localOverrides is not null ? new Dictionary<Injection, DependencyNode>(localOverrides) : null;
            _overrides = new Dictionary<int, DpOverride>(overrides);
            var hashCode = bindingId;
            hashCode = hashCode * 397 ^ GetHashCode(_nodes);
            hashCode = hashCode * 397 ^ (_localOverrides is not null ? GetHashCode(_localOverrides) + 1 : 0);
            foreach (var id in _overrides.Keys)
            {
                hashCode ^= id * 31;
            }

            _hashCode = hashCode * 397 ^ _overrides.Count;
        }

        public bool Equals(OverrideContext? other)
        {
            if (ReferenceEquals(this, other))
            {
                return true;
            }

            if (other is null
                || _bindingId != other._bindingId
                || _hashCode != other._hashCode
                || _overrides.Count != other._overrides.Count
                || !AreSame(_nodes, other._nodes)
                || !AreSame(_localOverrides, other._localOverrides))
            {
                return false;
            }

            foreach (var pair in _overrides)
            {
                if (!other._overrides.TryGetValue(pair.Key, out var otherOverride) || !pair.Value.Equals(otherOverride))
                {
                    return false;
                }
            }

            return true;
        }

        public override bool Equals(object? obj) => obj is OverrideContext other && Equals(other);

        public override int GetHashCode() => _hashCode;

        private static int GetHashCode(Dictionary<Injection, DependencyNode> nodes)
        {
            var hashCode = nodes.Count;
            foreach (var node in nodes.Values)
            {
                hashCode ^= node.Binding.Id * 31;
            }

            return hashCode;
        }

        private static bool AreSame(
            Dictionary<Injection, DependencyNode>? nodes,
            Dictionary<Injection, DependencyNode>? otherNodes)
        {
            if (nodes is null || otherNodes is null)
            {
                return nodes is null && otherNodes is null;
            }

            if (nodes.Count != otherNodes.Count)
            {
                return false;
            }

            foreach (var pair in nodes)
            {
                if (!otherNodes.TryGetValue(pair.Key, out var otherNode) || !ReferenceEquals(pair.Value, otherNode))
                {
                    return false;
                }
            }

            return true;
        }
    }

    // The injections that some override can replace and that the nodes reachable from a node of the original graph depend on.
    private sealed class ConsumedInjections
    {
        private readonly IGraph<DependencyNode, Dependency> _graph;
        // Ids of the injections that some override can replace, without tags.
        private readonly Dictionary<Injection, int> _ids = new();
        // The same ids by the type of an injection, because hashing an injection formats its type.
        private readonly Dictionary<ITypeSymbol, int> _idsByType = new(ReferenceComparer.Instance);
        private readonly Dictionary<int, HashSet<int>> _sets = new();
        // Strongly connected components are found by Tarjan's algorithm, because the graph may have cycles.
        private readonly Dictionary<int, (int Index, HashSet<int> Ids)> _visits = new();
        private readonly Stack<int> _stack = new();
        private int _index;

        public ConsumedInjections(MdSetup setup, IGraph<DependencyNode, Dependency> graph)
        {
            _graph = graph;
            foreach (var node in graph.Vertices)
            {
                if (node.Factory is not {} factory)
                {
                    continue;
                }

                var overrides = factory.Resolvers.SelectMany(i => i.Overrides)
                    .Concat(factory.Initializers.SelectMany(i => i.Overrides));
                foreach (var injection in overrides.SelectMany(i => i.Injections))
                {
                    var key = injection with { Type = node.TypeConstructor.Construct(setup, injection.Type), Tag = null };
                    if (!_ids.ContainsKey(key))
                    {
                        _ids.Add(key, _ids.Count);
                    }
                }
            }
        }

        public HashSet<int> Get(DependencyNode node)
        {
            if (!_sets.TryGetValue(node.Binding.Id, out var ids))
            {
                Visit(node);
                ids = _sets[node.Binding.Id];
            }

            return ids;
        }

        // An injection that no override was expected to replace is kept as consumed.
        public bool IsConsumed(HashSet<int> consumedIds, Injection injection) =>
            !TryGetId(injection.Type, out var id) || consumedIds.Contains(id);

        private bool TryGetId(ITypeSymbol type, out int id)
        {
            if (!_idsByType.TryGetValue(type, out id))
            {
                id = _ids.TryGetValue(new Injection(InjectionKind.Override, RefKind.None, type, null, ImmutableArray<Location>.Empty), out var injectionId)
                    ? injectionId
                    : -1;
                _idsByType.Add(type, id);
            }

            return id >= 0;
        }

        private int Visit(DependencyNode node)
        {
            var bindingId = node.Binding.Id;
            var index = _index++;
            var lowLink = index;
            var ids = new HashSet<int>();
            _visits[bindingId] = (index, ids);
            _stack.Push(bindingId);
            if (_graph.TryGetInEdges(node, out var dependencies))
            {
                foreach (var dependency in dependencies)
                {
                    // An override of a not annotated reference type replaces the annotated one too, see TryGetOverride.
                    var type = dependency.Injection.Type;
                    if (TryGetId(type, out var id))
                    {
                        ids.Add(id);
                    }

                    if (type is { IsReferenceType: true, NullableAnnotation: NullableAnnotation.Annotated }
                        && TryGetId(type.WithNullableAnnotation(NullableAnnotation.NotAnnotated), out id))
                    {
                        ids.Add(id);
                    }

                    var sourceBindingId = dependency.Source.Binding.Id;
                    if (_sets.TryGetValue(sourceBindingId, out var sourceIds))
                    {
                        ids.UnionWith(sourceIds);
                    }
                    else if (_visits.TryGetValue(sourceBindingId, out var sourceVisit))
                    {
                        // The source is on the stack, in the same component.
                        lowLink = Math.Min(lowLink, sourceVisit.Index);
                    }
                    else
                    {
                        lowLink = Math.Min(lowLink, Visit(dependency.Source));
                        if (_sets.TryGetValue(sourceBindingId, out sourceIds))
                        {
                            ids.UnionWith(sourceIds);
                        }
                    }
                }
            }

            if (lowLink < index)
            {
                return lowLink;
            }

            // The node is the root of a component, so all the nodes of the component consume the same injections.
            var componentBindingIds = new List<int>();
            int componentBindingId;
            do
            {
                componentBindingId = _stack.Pop();
                componentBindingIds.Add(componentBindingId);
                if (componentBindingId != bindingId)
                {
                    ids.UnionWith(_visits[componentBindingId].Ids);
                }
            } while (componentBindingId != bindingId);

            foreach (var memberBindingId in componentBindingIds)
            {
                _visits.Remove(memberBindingId);
                _sets[memberBindingId] = ids;
            }

            return lowLink;
        }
    }

    private sealed class ReferenceComparer : IEqualityComparer<ITypeSymbol>
    {
        public static readonly ReferenceComparer Instance = new();

        public bool Equals(ITypeSymbol? x, ITypeSymbol? y) => ReferenceEquals(x, y);

        public int GetHashCode(ITypeSymbol obj) => RuntimeHelpers.GetHashCode(obj);
    }
}
