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
            GetInlineCosts(graph, root, counts, factoryDownstream));
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
        var inProgress = new HashSet<int>();
        var stack = new Stack<(DependencyNode Node, bool IsExpanded)>();
        stack.Push((root, false));
        while (stack.Count > 0)
        {
            var (node, isExpanded) = stack.Pop();
            if (costs.ContainsKey(node.BindingId))
            {
                continue;
            }

            graph.Graph.TryGetInEdges(node, out var inEdges);
            if (!isExpanded)
            {
                if (!inProgress.Add(node.BindingId))
                {
                    continue;
                }

                stack.Push((node, true));
                if (inEdges is not null)
                {
                    foreach (var edge in inEdges)
                    {
                        var dep = edge.Source;
                        if (!costs.ContainsKey(dep.BindingId) && !inProgress.Contains(dep.BindingId))
                        {
                            stack.Push((dep, false));
                        }
                    }
                }

                continue;
            }

            // Only a node that is created once per scope is worth wrapping in a local function,
            // the code of other nodes is counted through the dependencies they create in place.
            var ownCost = nodeTools.IsBlock(node)
                ? nodeTools.EstimateBodyCost(node, isThreadSafe && node.ActualLifetime is Lifetime.Singleton or Lifetime.Scoped)
                : 0;

            var cost = ownCost;
            if (inEdges is not null)
            {
                foreach (var edge in inEdges)
                {
                    cost = Math.Min(cost + GetDependencyCost(edge.Source), MaxInlineCost);
                }
            }

            costs[node.BindingId] = cost;
            ownCosts[node.BindingId] = ownCost;
            inProgress.Remove(node.BindingId);
        }

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
