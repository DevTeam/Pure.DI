namespace Pure.DI.Core.Code;

sealed class RootUseSiteCounter : IRootUseSiteCounter
{
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

        return new RootUseSiteAnalysis(counts, factoryDownstream, GetOverrideConsumers(graph, root));
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
