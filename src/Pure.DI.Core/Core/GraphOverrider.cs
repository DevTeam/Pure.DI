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
        var state = new RewriteState(graph.Entries.Count, new ConsumedInjections(setup, graph), CreateCyclicOverrides(setup, graph, ref bindingId));
        foreach (var rootNode in from node in graph.Vertices where node.Root is not null select node)
        {
            Override(
                state,
                new Dictionary<Injection, DependencyNode>(),
                new Dictionary<Injection, DependencyNode>(),
                false,
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
        Dictionary<Injection, DependencyNode> nodes,
        Dictionary<Injection, DependencyNode> localOverrides,
        bool consumeLocalOverrides,
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
        // A node on a cycle through lambdas that override values gets each of those values in its scope,
        // whether a lambda above it on the path has overridden the value or not. Otherwise, the node got a rewrite
        // for each set of the values overridden on the paths to it, which grows exponentially with the length of the cycle, see #192.
        // The code reads such a value from a variable when it is overridden and creates it otherwise, see MaybeOverride.
        if (state.CyclicOverrides.Markers.TryGetValue(bindingId, out var markers))
        {
            Dictionary<Injection, DependencyNode>? scopeNodes = null;
            foreach (var marker in markers)
            {
                if (!nodes.ContainsKey(marker.Key))
                {
                    (scopeNodes ??= new Dictionary<Injection, DependencyNode>(nodes))[marker.Key] = marker.Value;
                }
            }

            nodes = scopeNodes ?? nodes;
        }

        if (nodes.Count > 0 || consumeLocalOverrides)
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
        }

        // The rewrite of a node depends only on the override scope it is reached in,
        // so a node reached by several dependency paths in the same scope is rewritten once, see #172.
        // A node rewritten outside any scope is not reused in a scope, because overridden copies of a node share its binding id.
        // A rewrite is registered before its dependencies are rewritten, so a node that refers back to it,
        // for example through Lazy<T>, gets the same rewrite on every path, see #177.
        var context = nodes.Count == 0 && !consumeLocalOverrides
            ? null
            : new OverrideContext(bindingId, nodes, consumeLocalOverrides ? localOverrides : null);

        // A shared instance is created once for its binding id, so it has one rewrite. Otherwise, each rewrite replaced
        // the dependencies of the previous one, and the code of the instance could read the overridden value
        // of a delegate where it is not declared, see #185. The rewrite outside any scope is valid everywhere,
        // so it replaces the rewrite in a scope, and the instance does not depend on the order in which it is reached.
        if (IsShared(targetNode)
            && state.Shared.TryGetValue(bindingId, out var shared)
            && (shared.IsContextFree || context is not null))
        {
            return shared.Node;
        }

        if (context is null
            ? state.Processed.TryGetValue(bindingId, out var node)
            : state.Rewritten.TryGetValue(context, out node))
        {
            return node;
        }

        return RewriteNode(
            state,
            bindingId,
            context,
            nodes,
            localOverrides,
            consumeLocalOverrides,
            setup,
            graph,
            dependencies,
            rootNode,
            GetCopy(state, nodes, consumeLocalOverrides ? localOverrides : null, targetNode, ref maxId),
            ref maxId,
            entries);
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
        DependencyNode targetNode,
        ref int maxId)
    {
        // A shared instance is created once for its binding id.
        if (IsShared(targetNode))
        {
            return targetNode;
        }

        var binding = targetNode.Binding;
        var scope = new OverrideScope(nodes, localOverrides);
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

    private static bool IsShared(DependencyNode node) =>
        node.Lifetime is Lifetime.Singleton or Lifetime.Scoped or Lifetime.PerResolve;

    // The 1-based positions of the resolvers that a delegate calls after overriding its arguments, in the order of RewriteNode.
    // Such a resolver creates the result of the delegate, so an override inherited from an outer delegate does not replace it.
    // Otherwise, Func<T1, T> under Func<T, T2> returned the argument of the outer delegate instead of creating its result, see #185.
    private static HashSet<int>? GetDelegateResultPositions(DependencyNode node)
    {
        if (node.Factory is not {} factory)
        {
            return null;
        }

        HashSet<int>? positions = null;
        var position = 0;
        foreach (var (isResolver, overrides) in factory.Resolvers.Select(i => (i.Source.Position, IsResolver: true, i.Overrides))
                     .Concat(factory.Initializers.Select(i => (i.Source.Position, IsResolver: false, i.Overrides)))
                     .OrderBy(i => i.Position)
                     .Select(i => (i.IsResolver, i.Overrides)))
        {
            position++;
            if (isResolver && overrides.Any(i => !i.Injections.IsDefaultOrEmpty && DelegateArgumentOverrideValidator.IsDelegateArgument(i.Source)))
            {
                (positions ??= []).Add(position);
            }
        }

        return positions;
    }

    private static MdBinding CreateCopy(MdBinding binding, int id) =>
        binding with
        {
            Id = id,
            OriginalIds = (binding.OriginalIds.IsDefault ? ImmutableArray<int>.Empty : binding.OriginalIds).Add(binding.Id)
        };

    private DependencyNode RewriteNode(
        RewriteState state,
        int bindingId,
        OverrideContext? context,
        Dictionary<Injection, DependencyNode> nodes,
        Dictionary<Injection, DependencyNode> localOverrides,
        bool consumeLocalOverrides,
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

        // A factory resolves only the overrides it declares itself, so the overrides of the factories above it
        // are not passed down and do not split the rewrites of the nodes under it, see #178.
        var overridesMap = new Dictionary<int, DpOverride>();
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

        if (context is null)
        {
            state.Processed[bindingId] = targetNode;
        }
        else
        {
            state.Rewritten[context] = targetNode;
        }

        if (IsShared(targetNode))
        {
            state.Shared[bindingId] = (context is null, targetNode);
        }

        var delegateResultPositions = GetDelegateResultPositions(targetNode);
        // The overrides that the factory declares itself.
        var ownNodesMap = new Dictionary<Injection, DependencyNode>();

        var newDependencies = new List<Dependency>(dependencies.Count);
        var lastDependencyPosition = 0;
        using var overridesEnumerator = overridesEnumerable.GetEnumerator();
        var nextLocalOverrides = new Dictionary<Injection, DependencyNode>();
        var overriddenInjections = new HashSet<Injection>();
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

                            // An override of a factory replaces the one inherited from the factories above it,
                            // otherwise, for example, Func<T1, T> under a factory that overrides T1 did not pass its own argument, see #183.
                            // The code of both reads the value by the same name, so only the first override of the factory itself is kept.
                            if (!overriddenInjections.Add(injection))
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
                                ownNodesMap[injection] = sourceNode;
                            }
                        }
                    }
                }
            }

            var currentDependency = dependency with { Target = targetNode };
            var overridingNodesMap = dependency.Position is {} resolverPosition && delegateResultPositions?.Contains(resolverPosition) == true
                ? ownNodesMap
                : localNodesMap;
            if (!TryGetOverride(overridingNodesMap, currentDependency.Injection, out var overridingSourceNode))
            {
                var source = Override(
                    state,
                    nodesMap,
                    nextLocalOverrides,
                    nextLocalOverrides.Count > 0,
                    setup,
                    graph,
                    rootNode,
                    currentDependency.Source,
                    ref maxId,
                    entries);
                currentDependency = currentDependency with { Source = source };
            }
            else if (dependency.IsResolved
                     && dependency.Source.Construct is not { Source.Kind: MdConstructKind.Override }
                     && overridingSourceNode.Construct is { Source: { Kind: MdConstructKind.Override, State: DpOverride cyclicOverride } }
                     && state.CyclicOverrides.Ids.Contains(cyclicOverride.Source.Id)
                     && state.CyclicOverrides.CanCreate(graph, dependency.Source))
            {
                // The value may be overridden or not, depending on the path to the node, so the code checks it.
                var maybeOverride = CreateMaybeOverride(
                    state,
                    nodesMap,
                    nextLocalOverrides,
                    setup,
                    graph,
                    rootNode,
                    targetNode,
                    currentDependency,
                    dependency.Source,
                    cyclicOverride,
                    ref maxId,
                    entries);
                currentDependency = currentDependency with
                {
                    Injection = currentDependency.Injection.Kind is InjectionKind.FactoryInjection
                        ? currentDependency.Injection
                        : currentDependency.Injection with { Kind = InjectionKind.Override },
                    Source = maybeOverride,
                    IsResolved = true
                };
            }
            else
            {
                // A factory matches its dependencies to its ctx.Inject calls by the kind of injection,
                // so an overridden one keeps it. Otherwise, a factory that injects the argument of a delegate
                // had fewer injections than calls and was reported as a cyclic dependency.
                currentDependency = currentDependency with
                {
                    Injection = currentDependency.Injection.Kind is InjectionKind.FactoryInjection
                        ? currentDependency.Injection
                        : currentDependency.Injection with { Kind = InjectionKind.Override },
                    Source = overridingSourceNode,
                    IsResolved = true
                };
            }

            newDependencies.Add(currentDependency);
        }

        entries[targetNode] = newDependencies;
        return targetNode;
    }

    // A node that reads the overridden value when a lambda above it has overridden it, and creates the value otherwise.
    // It is an override of the value with the dependency that creates the value as if nothing overrides it.
    private DependencyNode CreateMaybeOverride(
        RewriteState state,
        Dictionary<Injection, DependencyNode> nodesMap,
        Dictionary<Injection, DependencyNode> nextLocalOverrides,
        MdSetup setup,
        IGraph<DependencyNode, Dependency> graph,
        DependencyNode rootNode,
        DependencyNode targetNode,
        Dependency dependency,
        DependencyNode valueSource,
        DpOverride @override,
        ref int maxId,
        Dictionary<DependencyNode, IReadOnlyCollection<Dependency>> entries)
    {
        var binding = bindingsFactory.CreateConstructBinding(
            setup,
            targetNode,
            dependency.Injection,
            @override.Source.ContractType,
            Lifetime.Transient,
            targetNode.TypeConstructor,
            ++maxId,
            MdConstructKind.Override,
            state: @override);

        var maybeOverride = nodesFactory.CreateNodes(setup, targetNode.TypeConstructor, binding).First();
        // The value is created where nothing overrides it, so the scope of its dependencies does not have it.
        var valueNodesMap = new Dictionary<Injection, DependencyNode>(nodesMap.Count);
        foreach (var pair in nodesMap)
        {
            if (pair.Value.Construct is not { Source.State: DpOverride nodeOverride } || nodeOverride.Source.Id != @override.Source.Id)
            {
                valueNodesMap.Add(pair.Key, pair.Value);
            }
        }

        var source = Override(
            state,
            valueNodesMap,
            nextLocalOverrides,
            nextLocalOverrides.Count > 0,
            setup,
            graph,
            rootNode,
            valueSource,
            ref maxId,
            entries);

        entries[maybeOverride] = [new Dependency(true, source, dependency.Injection, maybeOverride)];
        return maybeOverride;
    }

    // The values that the lambdas on cycles override for all the levels below them, see #192.
    private CyclicOverrides CreateCyclicOverrides(MdSetup setup, IGraph<DependencyNode, Dependency> graph, ref int maxId)
    {
        var cyclicOverrides = new CyclicOverrides();
        foreach (var component in GraphCycles.GetCycles(graph))
        {
            var markers = new Dictionary<Injection, DependencyNode>();
            foreach (var node in component)
            {
                if (node.Factory is not {} factory)
                {
                    continue;
                }

                var typeConstructor = node.TypeConstructor;
                foreach (var @override in factory.Resolvers.SelectMany(i => i.Overrides).Concat(factory.Initializers.SelectMany(i => i.Overrides)))
                {
                    if (@override.Injections.IsDefaultOrEmpty || !@override.Source.IsDeep)
                    {
                        continue;
                    }

                    var contractType = typeConstructor.Construct(setup, @override.Source.ContractType);
                    var overrideId = overrideIdProvider.GetId(contractType, @override.Source.Tags);
                    var injections = @override.Injections.Select(i => i with { Type = typeConstructor.Construct(setup, i.Type) }).ToImmutableArray();
                    var currentOverride = new DpOverride(@override.Source with { Id = overrideId, ContractType = contractType }, injections);
                    MdBinding? binding = null;
                    foreach (var injection in injections)
                    {
                        if (markers.ContainsKey(injection))
                        {
                            continue;
                        }

                        binding ??= bindingsFactory.CreateConstructBinding(
                            setup,
                            node,
                            injection,
                            contractType,
                            Lifetime.Transient,
                            typeConstructor,
                            ++maxId,
                            MdConstructKind.Override,
                            state: currentOverride);

                        markers[injection] = nodesFactory.CreateNodes(setup, typeConstructor, binding).First();
                        cyclicOverrides.Ids.Add(overrideId);
                    }
                }
            }

            if (markers.Count == 0)
            {
                continue;
            }

            foreach (var node in component)
            {
                cyclicOverrides.Markers[node.Binding.Id] = markers;
            }
        }

        return cyclicOverrides;
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

    private sealed class CyclicOverrides
    {
        // The ids of the overrides that the lambdas on cycles declare.
        public readonly HashSet<int> Ids = [];

        // The nodes that override such values by the injections they override, by the binding id of a node on the cycle.
        public readonly Dictionary<int, Dictionary<Injection, DependencyNode>> Markers = new();

        private readonly Dictionary<int, bool> _canCreate = new();

        // A value that cannot be created without an override, such as the argument of a delegate without a binding,
        // is overridden on each path to the code that reads it, so the code reads the overridden value.
        public bool CanCreate(IGraph<DependencyNode, Dependency> graph, DependencyNode node)
        {
            if (_canCreate.TryGetValue(node.Binding.Id, out var canCreate))
            {
                return canCreate;
            }

            canCreate = true;
            var visited = new HashSet<int> { node.Binding.Id };
            var stack = new Stack<DependencyNode>();
            stack.Push(node);
            while (canCreate && stack.Count > 0)
            {
                if (!graph.TryGetInEdges(stack.Pop(), out var dependencies))
                {
                    continue;
                }

                foreach (var dependency in dependencies)
                {
                    if (!dependency.IsResolved)
                    {
                        canCreate = false;
                        break;
                    }

                    if (visited.Add(dependency.Source.Binding.Id))
                    {
                        stack.Push(dependency.Source);
                    }
                }
            }

            _canCreate[node.Binding.Id] = canCreate;
            return canCreate;
        }
    }

    private sealed class RewriteState(
        int capacity,
        ConsumedInjections consumedInjections,
        CyclicOverrides cyclicOverrides)
    {
        // Nodes rewritten outside any override scope.
        public readonly Dictionary<int, DependencyNode> Processed = new(capacity);

        public readonly ConsumedInjections ConsumedInjections = consumedInjections;

        public readonly CyclicOverrides CyclicOverrides = cyclicOverrides;

        // Nodes rewritten in a non-empty override scope.
        public readonly Dictionary<OverrideContext, DependencyNode> Rewritten = new();

        // The rewrite of a shared instance by its binding id and whether it was rewritten outside any override scope.
        public readonly Dictionary<int, (bool IsContextFree, DependencyNode Node)> Shared = new();

        // The binding ids of the copies of a node by the injections they override, by the binding id of the node.
        public readonly Dictionary<int, List<(OverrideScope Scope, int BindingId)>> Copies = new();
    }

    // The injections that a rewrite of a node overrides, regardless of the nodes that override them.
    private sealed class OverrideScope(
        Dictionary<Injection, DependencyNode> nodes,
        Dictionary<Injection, DependencyNode>? localOverrides)
        : IEquatable<OverrideScope>
    {
        private readonly HashSet<Injection> _injections = [..nodes.Keys.Concat(localOverrides?.Keys ?? Enumerable.Empty<Injection>())];

        public bool Equals(OverrideScope? other) =>
            other is not null
            && _injections.SetEquals(other._injections);

        public override bool Equals(object? obj) => obj is OverrideScope other && Equals(other);

        public override int GetHashCode() => _injections.Count;
    }

    private sealed class OverrideContext : IEquatable<OverrideContext>
    {
        private readonly int _bindingId;
        private readonly KeyValuePair<Injection, DependencyNode>[] _nodes;
        private readonly KeyValuePair<Injection, DependencyNode>[]? _localOverrides;
        private readonly int _hashCode;

        public OverrideContext(
            int bindingId,
            Dictionary<Injection, DependencyNode> nodes,
            Dictionary<Injection, DependencyNode>? localOverrides)
        {
            // The scope is copied, because a caller keeps adding to it after the call.
            // It is kept in arrays, so that neither the copy nor the comparison hashes the injections, see #176.
            _bindingId = bindingId;
            _nodes = nodes.ToArray();
            _localOverrides = localOverrides?.ToArray();
            unchecked
            {
                var hashCode = bindingId;
                hashCode = hashCode * 397 ^ GetHashCode(_nodes);
                _hashCode = hashCode * 397 ^ (_localOverrides is not null ? GetHashCode(_localOverrides) + 1 : 0);
            }
        }

        public bool Equals(OverrideContext? other)
        {
            if (ReferenceEquals(this, other))
            {
                return true;
            }

            return other is not null
                   && _bindingId == other._bindingId
                   && _hashCode == other._hashCode
                   && AreSame(_nodes, other._nodes)
                   && AreSame(_localOverrides, other._localOverrides);
        }

        public override bool Equals(object? obj) => obj is OverrideContext other && Equals(other);

        public override int GetHashCode() => _hashCode;

        // The sum of the hashes does not depend on the order.
        private static int GetHashCode(KeyValuePair<Injection, DependencyNode>[] nodes)
        {
            unchecked
            {
                var hashCode = nodes.Length;
                foreach (var pair in nodes)
                {
                    hashCode += Mix(TryGetOverrideId(pair.Value, out var overrideId) ? overrideId : RuntimeHelpers.GetHashCode(pair.Value));
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

        // An override node is created again each time the factory that overrides is rewritten,
        // and its code refers to the overridden value by the id of the override, which depends only on its contract and tags.
        // So the nodes that override an injection by the same override lead to the same rewrite, see #178.
        private static bool AreSame(DependencyNode node, DependencyNode otherNode) =>
            ReferenceEquals(node, otherNode)
            || TryGetOverrideId(node, out var overrideId)
            && TryGetOverrideId(otherNode, out var otherOverrideId)
            && overrideId == otherOverrideId;

        private static bool TryGetOverrideId(DependencyNode node, out int overrideId)
        {
            if (node.Construct is { Source: { Kind: MdConstructKind.Override, State: DpOverride @override } })
            {
                overrideId = @override.Source.Id;
                return true;
            }

            overrideId = 0;
            return false;
        }

        // A node is compared first, so an injection is compared only for a matching node.
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
                if (!otherNodes.Any(otherPair => AreSame(pair.Value, otherPair.Value) && pair.Key.Equals(otherPair.Key)))
                {
                    return false;
                }
            }

            return true;
        }
    }

    // The injections that some override can replace and that the nodes reachable from a node of the original graph depend on.
    // An injection that a node overrides itself before a dependency is not consumed from outside through that dependency,
    // so, for example, Func<T1, T> does not consume T1 from the scope it is reached in, see #178.
    private sealed class ConsumedInjections
    {
        private readonly MdSetup _setup;
        private readonly IGraph<DependencyNode, Dependency> _graph;
        // Ids of the injections that some override can replace, without tags.
        private readonly Dictionary<Injection, int> _ids = new();
        // The same ids by the type of an injection, because hashing an injection formats its type.
        // A type is found by reference, so this only saves formatting the same type again.
        [SuppressMessage("MicrosoftCodeAnalysisCorrectness", "RS1024:Symbols should be compared for equality")]
        private readonly Dictionary<ITypeSymbol, int> _idsByType = new(ReferenceComparer.Instance);
        private readonly Dictionary<int, Consumed> _sets = new();
        // Strongly connected components are found by Tarjan's algorithm, because the graph may have cycles.
        private readonly Dictionary<int, VisitState> _visits = new();
        private readonly Stack<int> _stack = new();
        private int _index;

        public ConsumedInjections(MdSetup setup, IGraph<DependencyNode, Dependency> graph)
        {
            _setup = setup;
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
            if (!_sets.TryGetValue(node.Binding.Id, out var consumed))
            {
                Visit(node);
                consumed = _sets[node.Binding.Id];
            }

            return consumed.Ids;
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
            var visit = new VisitState(index);
            var consumed = visit.Consumed;
            _visits[bindingId] = visit;
            _stack.Push(bindingId);
            if (_graph.TryGetInEdges(node, out var dependencies))
            {
                var overrides = GetOverrides(node);
                var delegateResultPositions = GetDelegateResultPositions(node);
                foreach (var dependency in dependencies)
                {
                    HashSet<int>? shadowedIds = null;
                    if (dependency.Position is {} position && overrides.Count > 0)
                    {
                        // The rewrite replaces the dependency with an override, so nothing under it is consumed.
                        if (IsOverridden(overrides, position, dependency.Injection))
                        {
                            continue;
                        }

                        shadowedIds = GetShadowedIds(overrides, position);
                    }

                    // The result of a delegate is created by the delegate, so an override from outside does not replace it.
                    if (dependency.Position is not {} resolverPosition || delegateResultPositions?.Contains(resolverPosition) != true)
                    {
                        AddInjection(consumed, dependency.Injection);
                    }

                    var sourceBindingId = dependency.Source.Binding.Id;
                    if (!_sets.TryGetValue(sourceBindingId, out var sourceConsumed))
                    {
                        if (_visits.TryGetValue(sourceBindingId, out var sourceVisit))
                        {
                            // The source is on the stack, in the same component.
                            lowLink = Math.Min(lowLink, sourceVisit.Index);
                        }
                        else
                        {
                            lowLink = Math.Min(lowLink, Visit(dependency.Source));
                            _sets.TryGetValue(sourceBindingId, out sourceConsumed);
                        }
                    }

                    if (sourceConsumed is not null)
                    {
                        Add(consumed, sourceConsumed, shadowedIds);
                    }
                    else
                    {
                        visit.Edges.Add((sourceBindingId, shadowedIds));
                    }
                }
            }

            if (lowLink < index)
            {
                return lowLink;
            }

            // The node is the root of a component. The nodes of the component may shadow different injections,
            // so their sets are completed together until nothing changes.
            var component = new List<(int BindingId, VisitState Visit)>();
            int componentBindingId;
            do
            {
                componentBindingId = _stack.Pop();
                component.Add((componentBindingId, _visits[componentBindingId]));
            } while (componentBindingId != bindingId);

            bool isChanged;
            do
            {
                isChanged = false;
                foreach (var (_, member) in component)
                {
                    foreach (var (sourceBindingId, shadowedIds) in member.Edges)
                    {
                        isChanged |= Add(member.Consumed, _visits[sourceBindingId].Consumed, shadowedIds);
                    }
                }
            } while (isChanged);

            foreach (var (memberBindingId, member) in component)
            {
                _visits.Remove(memberBindingId);
                _sets[memberBindingId] = member.Consumed;
            }

            return lowLink;
        }

        private void AddInjection(Consumed consumed, Injection injection)
        {
            // An override of a not annotated reference type replaces the annotated one too, see TryGetOverride.
            var type = injection.Type;
            if (TryGetId(type, out var id))
            {
                AddId(consumed, id, injection.Tag);
            }

            if (type is { IsReferenceType: true, NullableAnnotation: NullableAnnotation.Annotated }
                && TryGetId(type.WithNullableAnnotation(NullableAnnotation.NotAnnotated), out id))
            {
                AddId(consumed, id, injection.Tag);
            }
        }

        private static void AddId(Consumed consumed, int id, object? tag)
        {
            consumed.Ids.Add(id);
            if (tag is not null)
            {
                consumed.TaggedIds.Add(id);
            }
        }

        // An injection consumed with a tag is never shadowed, because the ids do not distinguish tags.
        private static bool Add(Consumed consumed, Consumed sourceConsumed, HashSet<int>? shadowedIds)
        {
            var isChanged = false;
            foreach (var id in sourceConsumed.Ids)
            {
                if ((shadowedIds is null || !shadowedIds.Contains(id) || sourceConsumed.TaggedIds.Contains(id))
                    && consumed.Ids.Add(id))
                {
                    isChanged = true;
                }
            }

            foreach (var id in sourceConsumed.TaggedIds)
            {
                isChanged |= consumed.TaggedIds.Add(id);
            }

            return isChanged;
        }

        // The overrides of a factory with the 1-based position of the resolver or initializer that declares them,
        // in the order in which the rewrite applies them to the dependencies, see RewriteNode.
        private List<(int Position, DpOverride Override)> GetOverrides(DependencyNode node)
        {
            var overrides = new List<(int Position, DpOverride Override)>();
            if (node.Factory is not {} factory)
            {
                return overrides;
            }

            var position = 0;
            foreach (var resolverOverrides in factory.Resolvers.Select(i => (i.Source.Position, i.Overrides))
                         .Concat(factory.Initializers.Select(i => (i.Source.Position, i.Overrides)))
                         .OrderBy(i => i.Position)
                         .Select(i => i.Overrides))
            {
                position++;
                foreach (var @override in resolverOverrides)
                {
                    if (@override.Injections.IsDefaultOrEmpty)
                    {
                        continue;
                    }

                    var injections = @override.Injections.Select(i => i with { Type = node.TypeConstructor.Construct(_setup, i.Type) }).ToImmutableArray();
                    overrides.Add((position, @override with { Injections = injections }));
                }
            }

            return overrides;
        }

        private static bool IsOverridden(List<(int Position, DpOverride Override)> overrides, int position, Injection injection)
        {
            Injection? notAnnotatedInjection = injection.Type is { IsReferenceType: true, NullableAnnotation: NullableAnnotation.Annotated }
                ? injection with { Type = injection.Type.WithNullableAnnotation(NullableAnnotation.NotAnnotated) }
                : null;

            foreach (var (overridePosition, @override) in overrides)
            {
                if (overridePosition > position)
                {
                    continue;
                }

                foreach (var overrideInjection in @override.Injections)
                {
                    if (overrideInjection.Equals(injection) || notAnnotatedInjection is {} notAnnotated && overrideInjection.Equals(notAnnotated))
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        // The injections that the deep overrides declared before a dependency replace everywhere under it.
        private HashSet<int>? GetShadowedIds(List<(int Position, DpOverride Override)> overrides, int position)
        {
            HashSet<int>? shadowedIds = null;
            foreach (var (overridePosition, @override) in overrides)
            {
                if (overridePosition > position || !@override.Source.IsDeep)
                {
                    continue;
                }

                foreach (var injection in @override.Injections)
                {
                    if (injection.Tag is null && TryGetId(injection.Type, out var id))
                    {
                        (shadowedIds ??= []).Add(id);
                    }
                }
            }

            return shadowedIds;
        }

        private sealed class Consumed
        {
            public readonly HashSet<int> Ids = [];
            // The ids of the injections consumed with a tag.
            public readonly HashSet<int> TaggedIds = [];
        }

        private sealed class VisitState(int index)
        {
            public readonly int Index = index;
            public readonly Consumed Consumed = new();
            // The dependencies on the nodes of the same component, which are completed with the component.
            public readonly List<(int SourceBindingId, HashSet<int>? ShadowedIds)> Edges = [];
        }
    }

    private sealed class ReferenceComparer : IEqualityComparer<ITypeSymbol>
    {
        public static readonly ReferenceComparer Instance = new();

        public bool Equals(ITypeSymbol? x, ITypeSymbol? y) => ReferenceEquals(x, y);

        public int GetHashCode(ITypeSymbol obj) => RuntimeHelpers.GetHashCode(obj);
    }
}
