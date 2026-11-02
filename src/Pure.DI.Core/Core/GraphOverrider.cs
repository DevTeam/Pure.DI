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
        var state = new RewriteState(graph.Entries.Count, new ConsumedInjections(setup, graph));
        foreach (var rootNode in from node in graph.Vertices where node.Root is not null select node)
        {
            Override(
                state,
                0,
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
        }

        // A node above on the current path is referred back to.
        if (state.Path.TryGetValue(bindingId, out var pathNode))
        {
            state.MinBackReferenceDepth = Math.Min(state.MinBackReferenceDepth, pathNode.Depth);
            return pathNode.Node;
        }

        // The rewrite of a node depends only on the override scope it is reached in,
        // so a node reached by several dependency paths in the same scope is rewritten once, see #172.
        // A node rewritten outside any scope is not reused in a scope, because overridden copies of a node share its binding id.
        var isContextFree = nodes.Count == 0 && !consumeLocalOverrides && overrides.Count == 0;
        var context = isContextFree
            ? null
            : new OverrideContext(bindingId, nodes, consumeLocalOverrides ? localOverrides : null, overrides);

        if (context is null
            ? state.Processed.TryGetValue(bindingId, out var node)
            : state.Rewritten.TryGetValue(context, out node))
        {
            return node;
        }

        var outerMinBackReferenceDepth = state.MinBackReferenceDepth;
        state.MinBackReferenceDepth = int.MaxValue;
        var rewrittenNode = RewriteNode(
            state,
            depth,
            bindingId,
            isContextFree,
            nodes,
            localOverrides,
            consumeLocalOverrides,
            overrides,
            setup,
            graph,
            dependencies,
            rootNode,
            GetCopy(state, nodes, consumeLocalOverrides ? localOverrides : null, overrides, targetNode, ref maxId),
            ref maxId,
            entries);

        state.Path.Remove(bindingId);
        var minBackReferenceDepth = state.MinBackReferenceDepth;
        state.MinBackReferenceDepth = Math.Min(outerMinBackReferenceDepth, minBackReferenceDepth);
        // A rewrite that refers back to a node above it on the current path belongs to this path only.
        if (context is not null && minBackReferenceDepth >= depth && !cancellationToken.IsCancellationRequested)
        {
            state.Rewritten[context] = rewrittenNode;
        }

        return rewrittenNode;
    }

    // The nodes are compared by binding id, so the rewrites of a node that override different injections
    // are copies with binding ids of their own. Otherwise, only one of them would remain in the graph, and,
    // for example, a node that is reached both under a delegate that overrides an argument and outside it
    // would use the overridden value outside the delegate too.
    // The rewrites that override the same injections by different nodes share the binding id,
    // because an overridden value is named by its contract, so their code is the same.
    private static DependencyNode GetCopy(
        RewriteState state,
        Dictionary<Injection, DependencyNode> nodes,
        Dictionary<Injection, DependencyNode>? localOverrides,
        Dictionary<int, DpOverride> overrides,
        DependencyNode targetNode,
        ref int maxId)
    {
        // A shared instance is created once for its binding id.
        if (targetNode.Lifetime is Lifetime.Singleton or Lifetime.Scoped or Lifetime.PerResolve)
        {
            return targetNode;
        }

        var binding = targetNode.Binding;
        var scope = new OverrideScope(nodes, localOverrides, overrides);
        if (!state.Copies.TryGetValue(binding.Id, out var copies))
        {
            state.Copies.Add(binding.Id, [(scope, binding.Id)]);
            return targetNode;
        }

        foreach (var (copyScope, copyId) in copies)
        {
            if (copyScope.Equals(scope))
            {
                return copyId == binding.Id ? targetNode : targetNode with { Binding = CreateCopy(binding, copyId) };
            }
        }

        var id = ++maxId;
        copies.Add((scope, id));
        return targetNode with { Binding = CreateCopy(binding, id) };
    }

    private static MdBinding CreateCopy(MdBinding binding, int id) =>
        binding with
        {
            Id = id,
            OriginalIds = (binding.OriginalIds.IsDefault ? ImmutableArray<int>.Empty : binding.OriginalIds).Add(binding.Id)
        };

    private DependencyNode RewriteNode(
        RewriteState state,
        int depth,
        int bindingId,
        bool isContextFree,
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

        state.Path[bindingId] = (targetNode, depth);
        if (isContextFree)
        {
            state.Processed[bindingId] = targetNode;
        }

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
        int capacity,
        ConsumedInjections consumedInjections)
    {
        // Nodes rewritten outside any override scope.
        public readonly Dictionary<int, DependencyNode> Processed = new(capacity);

        public readonly ConsumedInjections ConsumedInjections = consumedInjections;

        // Nodes rewritten in a non-empty override scope.
        public readonly Dictionary<OverrideContext, DependencyNode> Rewritten = new();

        // The nodes being rewritten on the current path and their depths, by binding id.
        public readonly Dictionary<int, (DependencyNode Node, int Depth)> Path = new();

        // The smallest depth of a node on the current path that the rewrite has referred back to.
        public int MinBackReferenceDepth = int.MaxValue;

        // The binding ids of the copies of a node by the injections they override, by the binding id of the node.
        public readonly Dictionary<int, List<(OverrideScope Scope, int BindingId)>> Copies = new();
    }

    // The injections that a rewrite of a node overrides, regardless of the nodes that override them.
    private sealed class OverrideScope(
        Dictionary<Injection, DependencyNode> nodes,
        Dictionary<Injection, DependencyNode>? localOverrides,
        Dictionary<int, DpOverride> overrides)
        : IEquatable<OverrideScope>
    {
        private readonly HashSet<Injection> _injections = [..nodes.Keys.Concat(localOverrides?.Keys ?? Enumerable.Empty<Injection>())];
        private readonly HashSet<int> _overrides = [..overrides.Keys];

        public bool Equals(OverrideScope? other) =>
            other is not null
            && _injections.SetEquals(other._injections)
            && _overrides.SetEquals(other._overrides);

        public override bool Equals(object? obj) => obj is OverrideScope other && Equals(other);

        public override int GetHashCode() => _injections.Count * 397 ^ _overrides.Count;
    }

    private sealed class OverrideContext : IEquatable<OverrideContext>
    {
        private readonly int _bindingId;
        private readonly KeyValuePair<Injection, DependencyNode>[] _nodes;
        private readonly KeyValuePair<Injection, DependencyNode>[]? _localOverrides;
        private readonly KeyValuePair<int, DpOverride>[] _overrides;
        private readonly int _hashCode;

        public OverrideContext(
            int bindingId,
            Dictionary<Injection, DependencyNode> nodes,
            Dictionary<Injection, DependencyNode>? localOverrides,
            Dictionary<int, DpOverride> overrides)
        {
            // The scope is copied, because a caller keeps adding to it after the call.
            // It is kept in arrays, so that neither the copy nor the comparison hashes the injections, see #176.
            _bindingId = bindingId;
            _nodes = nodes.ToArray();
            _localOverrides = localOverrides?.ToArray();
            _overrides = overrides.ToArray();
            unchecked
            {
                var hashCode = bindingId;
                hashCode = hashCode * 397 ^ GetHashCode(_nodes);
                hashCode = hashCode * 397 ^ (_localOverrides is not null ? GetHashCode(_localOverrides) + 1 : 0);
                var overridesHashCode = _overrides.Length;
                foreach (var pair in _overrides)
                {
                    overridesHashCode += Mix(pair.Key * 397 ^ pair.Value.Source.Id);
                }

                _hashCode = hashCode * 397 ^ overridesHashCode;
            }
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
                || _overrides.Length != other._overrides.Length
                || !AreSame(_nodes, other._nodes)
                || !AreSame(_localOverrides, other._localOverrides))
            {
                return false;
            }

            foreach (var pair in _overrides)
            {
                if (!other._overrides.Any(otherPair => otherPair.Key == pair.Key && AreSame(pair.Value, otherPair.Value)))
                {
                    return false;
                }
            }

            return true;
        }

        public override bool Equals(object? obj) => obj is OverrideContext other && Equals(other);

        public override int GetHashCode() => _hashCode;

        // The nodes are compared by reference, so they are hashed by reference too,
        // and the sum of the hashes does not depend on the order.
        private static int GetHashCode(KeyValuePair<Injection, DependencyNode>[] nodes)
        {
            unchecked
            {
                var hashCode = nodes.Length;
                foreach (var pair in nodes)
                {
                    hashCode += Mix(RuntimeHelpers.GetHashCode(pair.Value));
                }

                return hashCode;
            }
        }

        // Spreads the bits of a value, so that the sums of the hashes of different scopes rarely match.
        private static int Mix(int value)
        {
            unchecked
            {
                var hash = (uint)value;
                hash ^= hash >> 16;
                hash *= 0x85EBCA6B;
                hash ^= hash >> 13;
                hash *= 0xC2B2AE35;
                hash ^= hash >> 16;
                return (int)hash;
            }
        }

        // An override is created again each time its factory is rewritten, so the overrides are compared by their content.
        private static bool AreSame(DpOverride @override, DpOverride otherOverride) =>
            @override.Source.Equals(otherOverride.Source)
            && @override.Injections.SequenceEqual(otherOverride.Injections);

        // A node is found by reference first, so an injection is compared only for a matching node.
        private static bool AreSame(
            KeyValuePair<Injection, DependencyNode>[]? nodes,
            KeyValuePair<Injection, DependencyNode>[]? otherNodes)
        {
            if (nodes is null || otherNodes is null)
            {
                return nodes is null && otherNodes is null;
            }

            if (nodes.Length != otherNodes.Length)
            {
                return false;
            }

            foreach (var pair in nodes)
            {
                if (!otherNodes.Any(otherPair => ReferenceEquals(pair.Value, otherPair.Value) && pair.Key.Equals(otherPair.Key)))
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
        // A type is found by reference, so this only saves formatting the same type again.
        [SuppressMessage("MicrosoftCodeAnalysisCorrectness", "RS1024:Symbols should be compared for equality")]
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
