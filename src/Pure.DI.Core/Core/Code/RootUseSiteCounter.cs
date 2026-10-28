namespace Pure.DI.Core.Code;

sealed class RootUseSiteCounter(INodeTools nodeTools) : IRootUseSiteCounter
{
    // Bounds the estimate for a graph whose number of paths grows exponentially.
    private const int MaxInlineCost = 1 << 20;

    public RootUseSiteAnalysis Analyze(DependencyGraph graph, DependencyNode root)
    {
        // Edge convention in Pure.DI: Source is the dependency, Target is the consumer.
        // To walk from root downward through its dependencies we follow IN-edges.
        var counts = new Dictionary<int, int>();
        var factoryDownstream = new HashSet<int>();
        var visited = new HashSet<int> { root.BindingId };
        var stack = new Stack<(DependencyNode Node, bool UnderFactory)>();
        stack.Push((root, false));

        while (stack.Count > 0)
        {
            var (consumer, underFactory) = stack.Pop();
            if (!graph.Graph.TryGetInEdges(consumer, out var inEdges))
            {
                continue;
            }

            // If the current consumer node is a factory, anything it depends on can
            // be referenced inside branching factory body code.
            var consumerIsFactory = consumer.Factory is not null;

            foreach (var edge in inEdges)
            {
                var dep = edge.Source;
                counts.TryGetValue(dep.BindingId, out var c);
                counts[dep.BindingId] = c + 1;

                var depUnderFactory = underFactory || consumerIsFactory;
                if (depUnderFactory)
                {
                    factoryDownstream.Add(dep.BindingId);
                }

                if (visited.Add(dep.BindingId))
                {
                    stack.Push((dep, depUnderFactory));
                }
                else if (depUnderFactory)
                {
                    PropagateFactory(graph, dep, factoryDownstream);
                }
            }
        }

        return new RootUseSiteAnalysis(
            counts,
            factoryDownstream,
            GetOverrideConsumers(graph, root),
            GetInlineCosts(graph, root, counts, factoryDownstream),
            GetTransientCosts(graph, root, counts),
            GetPerBlockDependents(graph, root));
    }

    // A transient instance is created anew at each use site, so its code, together with the code
    // of the transient dependencies created with it, is repeated at each of them.
    private Dictionary<int, int> GetTransientCosts(
        DependencyGraph graph,
        DependencyNode root,
        IReadOnlyDictionary<int, int> useSiteCount)
    {
        var costs = new Dictionary<int, int>();
        VisitDependenciesFirst(graph, root, (node, dependencies) =>
        {
            var cost = IsTransientCandidate(node) ? nodeTools.EstimateBodyCost(node, false) : 1;
            if (!nodeTools.IsBlock(node))
            {
                foreach (var dep in dependencies)
                {
                    cost = Math.Min(cost + GetDependencyCost(dep), MaxInlineCost);
                }
            }

            costs[node.BindingId] = cost;
        });

        return costs;

        int GetDependencyCost(DependencyNode dep)
        {
            if (dep.Arg is not null || nodeTools.IsBlock(dep))
            {
                return 1;
            }

            // A dependency that is still being estimated is part of a cycle.
            if (!costs.TryGetValue(dep.BindingId, out var depCost))
            {
                return 1;
            }

            var hasLocalFunction = IsTransientCandidate(dep)
                                   && useSiteCount.TryGetValue(dep.BindingId, out var count)
                                   && LocalFunctions.IsTransientCostTooHigh(count, depCost);
            return hasLocalFunction ? 1 : depCost;
        }
    }

    // A PerBlock instance is shared by the code of one block, so a local function that creates it anew
    // on each call would change it. The code inside a lambda is a block of its own.
    private HashSet<int> GetPerBlockDependents(DependencyGraph graph, DependencyNode root)
    {
        var dependents = new HashSet<int>();
        VisitDependenciesFirst(graph, root, (node, dependencies) =>
        {
            if (nodeTools.IsLazy(node, graph))
            {
                return;
            }

            if (dependencies.Any(dep => dep.ActualLifetime is Lifetime.PerBlock || dependents.Contains(dep.BindingId)))
            {
                dependents.Add(node.BindingId);
            }
        });

        return dependents;
    }

    private static bool IsTransientCandidate(DependencyNode node) =>
        node is { Implementation: not null, Arg: null, ActualLifetime: Lifetime.Transient };

    // Visits each node after its dependencies; a dependency that closes a cycle is visited after its consumer.
    private static void VisitDependenciesFirst(
        DependencyGraph graph,
        DependencyNode root,
        Action<DependencyNode, IReadOnlyCollection<DependencyNode>> visit)
    {
        var visited = new HashSet<int>();
        var inProgress = new HashSet<int>();
        var stack = new Stack<(DependencyNode Node, bool IsExpanded)>();
        stack.Push((root, false));
        while (stack.Count > 0)
        {
            var (node, isExpanded) = stack.Pop();
            if (visited.Contains(node.BindingId))
            {
                continue;
            }

            var dependencies = graph.Graph.TryGetInEdges(node, out var inEdges)
                ? inEdges.Select(i => i.Source).ToList()
                : [];

            if (!isExpanded)
            {
                if (!inProgress.Add(node.BindingId))
                {
                    continue;
                }

                stack.Push((node, true));
                foreach (var dep in dependencies)
                {
                    if (!visited.Contains(dep.BindingId) && !inProgress.Contains(dep.BindingId))
                    {
                        stack.Push((dep, false));
                    }
                }

                continue;
            }

            visit(node, dependencies);
            visited.Add(node.BindingId);
            inProgress.Remove(node.BindingId);
        }
    }

    // A node created in place repeats the code of every dependency created in place with it,
    // and this code is repeated at each of its use sites. Without a local function, a diamond
    // of such nodes is written out once per dependency path, so the cost of a node includes
    // the cost of each shared dependency that gets no local function, and a single call for one that does.
    // A dependency with a single use site is written out once, so only what it passes on from
    // its own dependencies is added.
    private Dictionary<int, int> GetInlineCosts(
        DependencyGraph graph,
        DependencyNode root,
        IReadOnlyDictionary<int, int> useSiteCount,
        HashSet<int> factoryDownstream)
    {
        var isThreadSafe = graph.Source.Hints.IsThreadSafeEnabled;
        var costs = new Dictionary<int, int>();
        var ownCosts = new Dictionary<int, int>();
        VisitDependenciesFirst(graph, root, (node, dependencies) =>
        {
            // Only a node that is created once per scope is worth wrapping in a local function,
            // the code of other nodes is counted through the dependencies they create in place.
            var ownCost = nodeTools.IsBlock(node)
                ? nodeTools.EstimateBodyCost(node, isThreadSafe && node.ActualLifetime is Lifetime.Singleton or Lifetime.Scoped)
                : 0;

            var cost = ownCost;
            foreach (var dep in dependencies)
            {
                cost = Math.Min(cost + GetDependencyCost(dep), MaxInlineCost);
            }

            costs[node.BindingId] = cost;
            ownCosts[node.BindingId] = ownCost;
        });

        return costs;

        int GetDependencyCost(DependencyNode dep)
        {
            // A dependency that is still being estimated is part of a cycle.
            if (!costs.TryGetValue(dep.BindingId, out var depCost))
            {
                return 1;
            }

            if (!useSiteCount.TryGetValue(dep.BindingId, out var count) || count < LocalFunctions.MinUseSites)
            {
                return depCost - ownCosts[dep.BindingId];
            }

            var hasLocalFunction = nodeTools.IsBlock(dep)
                                   && (factoryDownstream.Contains(dep.BindingId) || depCost >= LocalFunctions.MinBodyCost);
            return hasLocalFunction ? 1 : depCost;
        }
    }

    // Binding ids of the nodes whose dependencies reach an overridden value (ctx.Override or ctx.Let),
    // directly or through any number of other nodes. Such a node reads a local of the lambda
    // that declares the override. Overridden graph branches can share a binding id
    // with their non-overridden copies, so this walks node instances, not binding ids.
    private static HashSet<int> GetOverrideConsumers(DependencyGraph graph, DependencyNode root)
    {
        var consumers = new Dictionary<DependencyNode, List<DependencyNode>>();
        var overrideNodes = new List<DependencyNode>();
        var visited = new HashSet<DependencyNode> { root };
        var stack = new Stack<DependencyNode>();
        stack.Push(root);
        while (stack.Count > 0)
        {
            var consumer = stack.Pop();
            if (!graph.Graph.TryGetInEdges(consumer, out var inEdges))
            {
                continue;
            }

            foreach (var edge in inEdges)
            {
                var dep = edge.Source;
                if (!consumers.TryGetValue(dep, out var depConsumers))
                {
                    depConsumers = [];
                    consumers.Add(dep, depConsumers);
                }

                depConsumers.Add(consumer);
                if (!visited.Add(dep))
                {
                    continue;
                }

                if (dep.Construct is { Source.Kind: MdConstructKind.Override })
                {
                    overrideNodes.Add(dep);
                }

                stack.Push(dep);
            }
        }

        var result = new HashSet<int>();
        var reached = new HashSet<DependencyNode>(overrideNodes);
        var pending = new Stack<DependencyNode>(overrideNodes);
        while (pending.Count > 0)
        {
            if (!consumers.TryGetValue(pending.Pop(), out var nodeConsumers))
            {
                continue;
            }

            foreach (var consumer in nodeConsumers)
            {
                result.Add(consumer.BindingId);
                if (reached.Add(consumer))
                {
                    pending.Push(consumer);
                }
            }
        }

        return result;
    }

    private static void PropagateFactory(DependencyGraph graph, DependencyNode start, HashSet<int> factoryDownstream)
    {
        var stack = new Stack<DependencyNode>();
        stack.Push(start);
        while (stack.Count > 0)
        {
            var node = stack.Pop();
            if (!graph.Graph.TryGetInEdges(node, out var inEdges))
            {
                continue;
            }

            foreach (var edge in inEdges)
            {
                if (factoryDownstream.Add(edge.Source.BindingId))
                {
                    stack.Push(edge.Source);
                }
            }
        }
    }
}
