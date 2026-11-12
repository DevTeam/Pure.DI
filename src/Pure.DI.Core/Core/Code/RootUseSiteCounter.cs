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
        var consumers = new Dictionary<int, List<DependencyNode>>();
        var factoryDownstream = new HashSet<int>();
        var visited = new HashSet<int> { root.BindingId };
        var nodes = new List<DependencyNode> { root };
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
                if (!consumers.TryGetValue(dep.BindingId, out var depConsumers))
                {
                    depConsumers = [];
                    consumers.Add(dep.BindingId, depConsumers);
                }

                depConsumers.Add(consumer);

                var depUnderFactory = underFactory || consumerIsFactory;
                if (depUnderFactory)
                {
                    factoryDownstream.Add(dep.BindingId);
                }

                if (visited.Add(dep.BindingId))
                {
                    nodes.Add(dep);
                    stack.Push((dep, depUnderFactory));
                }
                else if (depUnderFactory)
                {
                    PropagateFactory(graph, dep, factoryDownstream);
                }
            }
        }

        AddInlineUseSites(graph, counts, consumers);
        var cycleEntries = GetCycleEntries(graph, out var branchingCycles);
        var freeOverrides = GetFreeOverrides(graph, root);
        return new RootUseSiteAnalysis(
            counts,
            factoryDownstream,
            freeOverrides,
            GetInlineCosts(graph, root, counts, factoryDownstream),
            GetTransientCosts(graph, root, counts, freeOverrides),
            GetPerBlockDependents(graph, root, freeOverrides, out var privateLambdas),
            GetMaybeOverrides(graph),
            cycleEntries,
            branchingCycles,
            GetConsumerDependents(nodes, consumers),
            privateLambdas);
    }

    // A factory that reads ctx.ConsumerType creates an instance for the consumer it is injected into, so the code
    // of the instance is not shared by its consumers. A factory that reads ctx.ConsumerTypes depends on all the consumers
    // up to the root, so the code of each of these consumers is not shared too. Otherwise, a local function
    // created the instances for all the consumers with the consumers of its first call.
    private HashSet<int> GetConsumerDependents(List<DependencyNode> nodes, Dictionary<int, List<DependencyNode>> consumers)
    {
        var dependents = new HashSet<int>();
        HashSet<int>? chainDependents = null;
        Stack<int>? stack = null;
        foreach (var node in nodes)
        {
            if (nodeTools.ReadsContextMember(node, nameof(IContext.ConsumerTypes)))
            {
                if ((chainDependents ??= []).Add(node.BindingId))
                {
                    (stack ??= new Stack<int>()).Push(node.BindingId);
                }
            }
            else if (nodeTools.ReadsContextMember(node, nameof(IContext.ConsumerType)))
            {
                dependents.Add(node.BindingId);
            }
        }

        if (chainDependents is null || stack is null)
        {
            return dependents;
        }

        while (stack.Count > 0)
        {
            if (!consumers.TryGetValue(stack.Pop(), out var nodeConsumers))
            {
                continue;
            }

            foreach (var consumer in nodeConsumers)
            {
                if (chainDependents.Add(consumer.BindingId))
                {
                    stack.Push(consumer.BindingId);
                }
            }
        }

        dependents.UnionWith(chainDependents);
        return dependents;
    }

    // A node created only as the result of lambdas, such as Func<T>, is created by the code of each lambda,
    // and the code of a lambda is repeated at each of its own use sites. So, for example, a view model
    // that opens the next one through delegates has as many use sites as the delegates that create it, see #184.
    // Arrays and spans cannot have a creation helper of their own. Each use of the collection
    // repeats its elements, so those elements need the collection's use sites too, see #187.
    private void AddInlineUseSites(
        DependencyGraph graph,
        Dictionary<int, int> counts,
        Dictionary<int, List<DependencyNode>> consumers)
    {
        var lambdaCounts = (
                from pair in consumers
                where pair.Value.All(consumer => nodeTools.IsLazy(consumer, graph))
                      || pair.Value.Any(IsInlineCollection)
                select (BindingId: pair.Key, Count: pair.Value.Sum(consumer =>
                    (nodeTools.IsLazy(consumer, graph) || IsInlineCollection(consumer))
                    && counts.TryGetValue(consumer.BindingId, out var count) ? count : 1)))
            .ToList();

        foreach (var (bindingId, count) in lambdaCounts)
        {
            counts[bindingId] = count;
        }
    }

    private static bool IsInlineCollection(DependencyNode node) =>
        node.Construct is { Source.Kind: MdConstructKind.Array or MdConstructKind.Span };

    // A transient instance is created anew at each use site, so its code, together with the code
    // of the transient dependencies created with it, is repeated at each of them.
    // A PerBlock instance that reads a value overridden by a lambda is created again by the code of each such lambda,
    // so it is estimated like a transient instance, see LocalFunctions.
    private Dictionary<int, int> GetTransientCosts(
        DependencyGraph graph,
        DependencyNode root,
        IReadOnlyDictionary<int, int> useSiteCount,
        IReadOnlyDictionary<int, ImmutableArray<MdOverride>> freeOverrides)
    {
        var costs = new Dictionary<int, int>();
        VisitDependenciesFirst(graph, root, (node, dependencies) =>
        {
            var cost = IsTransientCandidate(graph, node) || IsOverrideReader(node) ? nodeTools.EstimateBodyCost(node, false) : 1;
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

            var hasLocalFunction = (IsTransientCandidate(graph, dep) || IsOverrideReader(dep))
                                   && useSiteCount.TryGetValue(dep.BindingId, out var count)
                                   && LocalFunctions.IsTransientCostTooHigh(count, depCost);
            return hasLocalFunction ? 1 : depCost;
        }

        bool IsOverrideReader(DependencyNode node) =>
            node is { ActualLifetime: Lifetime.PerBlock, Arg: null }
            && freeOverrides.TryGetValue(node.BindingId, out var overrides)
            && !overrides.IsDefaultOrEmpty;
    }

    // A PerBlock instance is shared by the code of one block, so a local function that creates it anew
    // on each call would change it. The code inside a lambda is a block of its own.
    private HashSet<int> GetPerBlockDependents(
        DependencyGraph graph,
        DependencyNode root,
        IReadOnlyDictionary<int, ImmutableArray<MdOverride>> freeOverrides,
        out Dictionary<int, HashSet<int>> privateLambdas)
    {
        privateLambdas = new Dictionary<int, HashSet<int>>();
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

        if (dependents.Count == 0)
        {
            return dependents;
        }

        // A node that each block of code creates at most once, for example, the result of lambdas, such as Func<T1, T>,
        // or a transient dependency of such results in different lambdas, has no other instance in its block to share
        // the PerBlock instances with. When these PerBlock instances are reachable only through it, no other code of these blocks
        // or of the blocks around them can create them, so a local function creates them just as the code in place, see #175.
        // Otherwise, a transient instance reached from several lambdas would be created in place in each of them, and its code
        // would double per level of such lambdas.
        var consumers = new Dictionary<int, List<DependencyNode>>();
        var instances = new Dictionary<int, List<DependencyNode>>();
        CollectConsumers(graph, root, consumers, instances);
        var blocks = new Dictionary<int, HashSet<int>?>();
        var nodePrivateLambdas = privateLambdas;
        dependents.RemoveWhere(bindingId => IsCreatedOncePerBlockAlone(graph, root, bindingId, dependents, consumers, instances, blocks, freeOverrides, nodePrivateLambdas));
        return dependents;
    }

    private bool IsCreatedOncePerBlockAlone(
        DependencyGraph graph,
        DependencyNode root,
        int bindingId,
        HashSet<int> dependents,
        Dictionary<int, List<DependencyNode>> consumers,
        Dictionary<int, List<DependencyNode>> instances,
        Dictionary<int, HashSet<int>?> blocks,
        IReadOnlyDictionary<int, ImmutableArray<MdOverride>> freeOverrides,
        Dictionary<int, HashSet<int>> privateLambdas)
    {
        if (!instances.TryGetValue(bindingId, out var nodeInstances)
            || GetBlocks(graph, root, bindingId, consumers, blocks) is not {} nodeBlocks)
        {
            return false;
        }

        // The nodes created in place with this node.
        var nodes = new Dictionary<int, DependencyNode>();
        var stack = new Stack<DependencyNode>();
        foreach (var node in nodeInstances)
        {
            stack.Push(node);
        }

        while (stack.Count > 0)
        {
            var node = stack.Pop();
            if (!graph.Graph.TryGetInEdges(node, out var inEdges))
            {
                continue;
            }

            foreach (var edge in inEdges)
            {
                var dep = edge.Source;
                if (dep.BindingId == bindingId)
                {
                    return false;
                }

                if (nodeTools.IsBlock(dep) || nodes.ContainsKey(dep.BindingId))
                {
                    continue;
                }

                nodes.Add(dep.BindingId, dep);

                if (!nodeTools.IsLazy(dep, graph))
                {
                    stack.Push(dep);
                }
            }
        }

        // A lambda, such as Func<T1, T>, keeps no state of its own, so the lambdas of different blocks are interchangeable,
        // and a consumer created only in other blocks does not share them with this node. For example, a node reached
        // both under a delegate and outside it has a copy for each of these scopes, and the copies share the delegates
        // they inject. Otherwise, the local function of a copy got these delegates as arguments, and each call of it
        // created the delegates of all the levels below it again.
        // When another PerBlock instance is shared with the code around this node, the local function gets it as an argument,
        // but it still creates the lambdas that the code around it does not share, see LocalFunctions.GetPerBlockArguments.
        var isAlone = true;
        foreach (var node in nodes.Values)
        {
            if (node.ActualLifetime is not Lifetime.PerBlock && !dependents.Contains(node.BindingId))
            {
                continue;
            }

            var isLazy = nodeTools.IsLazy(node, graph);
            if (consumers.TryGetValue(node.BindingId, out var dependencyConsumers)
                && dependencyConsumers.Any(consumer => consumer.BindingId != bindingId
                                                       && !nodes.ContainsKey(consumer.BindingId)
                                                       // A shared instance creates its dependencies in a block of its own, once,
                                                       // so it does not share the PerBlock instances of the blocks that reach it, see #195.
                                                       && !nodeTools.IsBlock(consumer)
                                                       && !IsResultOfOtherLambdas(graph, consumer, nodeBlocks, consumers)
                                                       && !(isLazy && IsCreatedInOtherBlocks(graph, root, consumer, nodeBlocks, consumers, blocks))))
            {
                isAlone = false;
                continue;
            }

            if (isLazy && node.ActualLifetime is Lifetime.PerBlock && !CapturesPerBlockInstances(graph, node, freeOverrides))
            {
                if (!privateLambdas.TryGetValue(bindingId, out var lambdas))
                {
                    lambdas = [];
                    privateLambdas.Add(bindingId, lambdas);
                }

                lambdas.Add(node.BindingId);
            }
        }

        return isAlone;
    }

    // The code of a lambda reads the PerBlock instances of the block around it, except the ones that read a value
    // the lambda overrides, which it creates anew. A lambda created by a local function cannot read the instances
    // of the block of its caller, so it would create them again with all the instances under them.
    private bool CapturesPerBlockInstances(DependencyGraph graph, DependencyNode lambda, IReadOnlyDictionary<int, ImmutableArray<MdOverride>> freeOverrides)
    {
        var overrideIds = new HashSet<int>(GetDeclaredOverrides(lambda).Select(i => i.Id));
        var visited = new HashSet<int>();
        var stack = new Stack<DependencyNode>();
        stack.Push(lambda);
        while (stack.Count > 0)
        {
            if (!graph.Graph.TryGetInEdges(stack.Pop(), out var dependencies))
            {
                continue;
            }

            foreach (var dependency in dependencies)
            {
                var source = dependency.Source;
                if (!visited.Add(source.BindingId) || nodeTools.IsBlock(source) || nodeTools.IsLazy(source, graph) || source.Arg is not null)
                {
                    continue;
                }

                if (source.ActualLifetime is Lifetime.PerBlock
                    && !(freeOverrides.TryGetValue(source.BindingId, out var overrides) && overrides.Any(i => overrideIds.Contains(i.Id))))
                {
                    return true;
                }

                stack.Push(source);
            }
        }

        return false;
    }

    private bool IsCreatedInOtherBlocks(
        DependencyGraph graph,
        DependencyNode root,
        DependencyNode node,
        HashSet<int> nodeBlocks,
        Dictionary<int, List<DependencyNode>> consumers,
        Dictionary<int, HashSet<int>?> blocks) =>
        GetBlocks(graph, root, node.BindingId, consumers, blocks) is {} otherBlocks
        && !otherBlocks.Overlaps(nodeBlocks);

    // The blocks of code that create a node in place, by the binding ids of the lambdas or of the root whose code they are,
    // when each of these blocks creates it at most once; otherwise null.
    // The result of a lambda is created once in its block, and a transient dependency is created
    // in the blocks of its consumer as many times as its consumer.
    private HashSet<int>? GetBlocks(
        DependencyGraph graph,
        DependencyNode root,
        int bindingId,
        Dictionary<int, List<DependencyNode>> consumers,
        Dictionary<int, HashSet<int>?> blocks)
    {
        if (blocks.TryGetValue(bindingId, out var nodeBlocks))
        {
            return nodeBlocks;
        }

        // A node on a cycle is not created once per block.
        blocks.Add(bindingId, null);
        if (!consumers.TryGetValue(bindingId, out var nodeConsumers) || nodeConsumers.Count == 0)
        {
            return null;
        }

        // The code of the root creates the root once.
        nodeBlocks = bindingId == root.BindingId ? [root.BindingId] : [];
        foreach (var consumer in nodeConsumers)
        {
            HashSet<int>? consumerBlocks;
            if (consumer.BindingId == root.BindingId || nodeTools.IsLazy(consumer, graph))
            {
                consumerBlocks = [consumer.BindingId];
            }
            // The code of a factory can create its dependencies any number of times, for example, in a loop,
            // unless it injects each of them once, as the factory of Owned<T> does, see #195.
            // A PerBlock consumer is created at most once in a block, like a transient one created once there.
            else if (consumer is { ActualLifetime: Lifetime.Transient or Lifetime.PerBlock, Arg: null }
                     && (consumer.Factory is null || InjectsEachDependencyOnce(consumer.Factory)))
            {
                consumerBlocks = GetBlocks(graph, root, consumer.BindingId, consumers, blocks);
            }
            else
            {
                consumerBlocks = null;
            }

            if (consumerBlocks is null || consumerBlocks.Overlaps(nodeBlocks))
            {
                return null;
            }

            nodeBlocks.UnionWith(consumerBlocks);
        }

        blocks[bindingId] = nodeBlocks;
        return nodeBlocks;
    }

    // A factory injects each of its dependencies once when it gets them as the parameters of a simple factory,
    // or by the statements of its body, outside branches, loops and lambdas, as the factory of Owned<T> does.
    private static bool InjectsEachDependencyOnce(DpFactory factory)
    {
        var source = factory.Source;
        if (source.IsSimpleFactory)
        {
            return true;
        }

        if (!source.Initializers.IsDefaultOrEmpty)
        {
            return false;
        }

        if (source.Resolvers.IsDefaultOrEmpty)
        {
            return true;
        }

        // The factory of a generic binding is rewritten for each type, while its resolvers keep the syntax of the binding,
        // so a resolver is checked against the factory in the binding, such as To(ctx => { ... }).
        return source.Factory.Body is BlockSyntax
               && source.Resolvers.All(resolver => IsStatementOfBody(source, resolver));
    }

    // A factory creates its dependencies as a constructor does when it injects each of them once, by the statements of its body,
    // and no statement before the last injection can leave the factory, such as a return in a branch.
    // Then the caller of the local function of its instance can create its PerBlock dependencies before calling it, see #201.
    internal static bool InjectsEachDependencyBeforeAnyExit(DpFactory factory)
    {
        if (!InjectsEachDependencyOnce(factory))
        {
            return false;
        }

        var source = factory.Source;
        if (source.IsSimpleFactory || source.Resolvers.IsDefaultOrEmpty)
        {
            return true;
        }

        // The resolvers of a generic binding keep the syntax of the binding, so the statements are taken from it, see InjectsEachDependencyOnce.
        if (source.Resolvers[0].Source.Parent is not ExpressionStatementSyntax { Parent: BlockSyntax body })
        {
            return false;
        }

        var lastInjection = source.Resolvers.Max(resolver => body.Statements.IndexOf((StatementSyntax)resolver.Source.Parent!));
        return body.Statements.Take(lastInjection).All(statement => !CanLeave(statement));

        static bool CanLeave(StatementSyntax statement) =>
            statement
                .DescendantNodesAndSelf(node => node is not (AnonymousFunctionExpressionSyntax or LocalFunctionStatementSyntax))
                .Any(node => node is ReturnStatementSyntax or ThrowStatementSyntax or ThrowExpressionSyntax or GotoStatementSyntax or YieldStatementSyntax);
    }

    // An injection by a statement of the body of a factory, outside branches, loops and lambdas.
    internal static bool IsStatementOfBody(MdFactory factory, in MdResolver resolver) =>
        resolver.Source.Parent is ExpressionStatementSyntax { Parent: BlockSyntax { Parent: AnonymousFunctionExpressionSyntax factoryLambda } }
        && factoryLambda.Ancestors().TakeWhile(node => node != factory.Source).All(node => node is not AnonymousFunctionExpressionSyntax)
        && factoryLambda.Ancestors().Contains(factory.Source);

    // A node created only as the result of other lambdas is the first code of blocks of its own,
    // so the PerBlock instances it shares with a node created once per block are created anew in its blocks anyway.
    // For example, the copies of a view model made for different override scopes share the delegates that they inject, see #184.
    // A lambda can return the node in a PerBlock instance of its own that injects it once, such as Owned<T>, see #195.
    private bool IsResultOfOtherLambdas(
        DependencyGraph graph,
        DependencyNode node,
        HashSet<int> lambdas,
        Dictionary<int, List<DependencyNode>> consumers,
        HashSet<int>? visited = null) =>
        consumers.TryGetValue(node.BindingId, out var nodeConsumers)
        && nodeConsumers.Count > 0
        && nodeConsumers.All(consumer =>
            nodeTools.IsLazy(consumer, graph)
                ? !lambdas.Contains(consumer.BindingId)
                : consumer is { ActualLifetime: Lifetime.PerBlock, Arg: null, Factory: {} factory }
                  && InjectsEachDependencyOnce(factory)
                  && (visited ??= []).Add(consumer.BindingId)
                  && IsResultOfOtherLambdas(graph, consumer, lambdas, consumers, visited));

    // The consumers of each node by binding id, a consumer once for each of its dependencies,
    // and the instances of each node, because overridden copies of a node share its binding id.
    private static void CollectConsumers(
        DependencyGraph graph,
        DependencyNode root,
        Dictionary<int, List<DependencyNode>> consumers,
        Dictionary<int, List<DependencyNode>> instances)
    {
        var visited = new HashSet<DependencyNode> { root };
        // The root on a cycle through a lambda is an instance too, which the lambda creates again.
        instances.Add(root.BindingId, [root]);
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
                if (!consumers.TryGetValue(dep.BindingId, out var depConsumers))
                {
                    depConsumers = [];
                    consumers.Add(dep.BindingId, depConsumers);
                }

                depConsumers.Add(consumer);
                if (!visited.Add(dep))
                {
                    continue;
                }

                if (!instances.TryGetValue(dep.BindingId, out var depInstances))
                {
                    depInstances = [];
                    instances.Add(dep.BindingId, depInstances);
                }

                depInstances.Add(dep);
                stack.Push(dep);
            }
        }
    }

    // A factory is created by a local function too, unless it creates a lambda, such as Func<T> or Lazy<T>,
    // whose code belongs to the place where it is created.
    private bool IsTransientCandidate(DependencyGraph graph, DependencyNode node) =>
        node is { Arg: null, ActualLifetime: Lifetime.Transient }
        && (node.Implementation is not null || node.Factory is not null && !nodeTools.IsLazy(node, graph));

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

    // The entries of the cycles and the nodes of the cycles that branch. A cycle that has as many dependencies
    // between its nodes as it has nodes is a ring, which has a single path through its nodes.
    private static Dictionary<int, int> GetCycleEntries(DependencyGraph graph, out HashSet<int> branchingCycles)
    {
        var cycleEntries = new Dictionary<int, int>();
        branchingCycles = [];
        foreach (var cycle in GraphCycles.GetCycles(graph.Graph))
        {
            var members = new HashSet<int>(cycle.Select(i => i.BindingId));
            var entries = 0;
            var dependencies = 0;
            foreach (var node in cycle)
            {
                if (graph.Graph.TryGetInEdges(node, out var nodeDependencies))
                {
                    dependencies += nodeDependencies.Count(i => i.IsResolved && members.Contains(i.Source.BindingId));
                }

                if (!graph.Graph.TryGetOutEdges(node, out var consumers))
                {
                    continue;
                }

                entries += consumers.Count(i => !members.Contains(i.Target.BindingId));
            }

            foreach (var bindingId in members)
            {
                cycleEntries[bindingId] = entries;
            }

            if (dependencies > members.Count)
            {
                branchingCycles.UnionWith(members);
            }
        }

        return cycleEntries;
    }

    private static HashSet<int> GetMaybeOverrides(DependencyGraph graph)
    {
        var maybeOverrides = new HashSet<int>();
        foreach (var node in graph.Graph.Vertices)
        {
            if (node.Construct is { Source: { Kind: MdConstructKind.Override, State: DpOverride @override } }
                && graph.Graph.TryGetInEdges(node, out var dependencies)
                && dependencies.Count > 0)
            {
                maybeOverrides.Add(@override.Source.Id);
            }
        }

        return maybeOverrides;
    }

    // The overridden values that the code creating a node reads and does not declare itself, by binding id.
    // A factory declares the values it overrides before a dependency, so they are not free under that dependency.
    // A transient instance that reads overridden values gets them as the arguments of its local function, see #178.
    private static Dictionary<int, ImmutableArray<MdOverride>> GetFreeOverrides(DependencyGraph graph, DependencyNode root)
    {
        // The dependencies come before their consumers, so an acyclic graph is completed in one pass.
        var order = new List<DependencyNode>();
        var visited = new HashSet<int> { root.BindingId };
        var stack = new Stack<(DependencyNode Node, bool IsExpanded)>();
        stack.Push((root, false));
        while (stack.Count > 0)
        {
            var (node, isExpanded) = stack.Pop();
            if (isExpanded)
            {
                order.Add(node);
                continue;
            }

            stack.Push((node, true));
            if (!graph.Graph.TryGetInEdges(node, out var inEdges))
            {
                continue;
            }

            foreach (var edge in inEdges)
            {
                if (visited.Add(edge.Source.BindingId))
                {
                    stack.Push((edge.Source, false));
                }
            }
        }

        var freeOverrides = order.ToDictionary(i => i.BindingId, _ => new Dictionary<int, MdOverride>());
        bool isChanged;
        do
        {
            isChanged = false;
            foreach (var node in order)
            {
                if (!graph.Graph.TryGetInEdges(node, out var inEdges))
                {
                    continue;
                }

                var nodeFreeOverrides = freeOverrides[node.BindingId];
                var declaredOverrides = GetDeclaredOverrides(node);
                foreach (var edge in inEdges)
                {
                    if (edge.Source.Construct is { Source: { Kind: MdConstructKind.Override, State: DpOverride @override } })
                    {
                        isChanged |= AddFreeOverride(nodeFreeOverrides, @override.Source, declaredOverrides, edge.Position);
                        // A value that may be overridden is created where it is not, see MaybeOverrideCodeBuilder.
                        if (!freeOverrides.ContainsKey(edge.Source.BindingId))
                        {
                            continue;
                        }
                    }

                    if (!freeOverrides.TryGetValue(edge.Source.BindingId, out var sourceFreeOverrides))
                    {
                        continue;
                    }

                    foreach (var sourceOverride in sourceFreeOverrides.Values.ToList())
                    {
                        isChanged |= AddFreeOverride(nodeFreeOverrides, sourceOverride, declaredOverrides, edge.Position);
                    }
                }
            }
        } while (isChanged);

        return freeOverrides.ToDictionary(
            i => i.Key,
            i => i.Value.Values.OrderBy(@override => @override.Id).ToImmutableArray());

        static bool AddFreeOverride(
            Dictionary<int, MdOverride> nodeFreeOverrides,
            MdOverride @override,
            List<(int Position, int Id)> declaredOverrides,
            int? position)
        {
            if (nodeFreeOverrides.ContainsKey(@override.Id)
                || position.HasValue && declaredOverrides.Any(i => i.Id == @override.Id && i.Position <= position.Value))
            {
                return false;
            }

            nodeFreeOverrides.Add(@override.Id, @override);
            return true;
        }
    }

    // The ids of the values that a factory overrides, with the 1-based position of the resolver or initializer
    // that overrides them, in the order in which they are applied to the dependencies, see GraphOverrider.
    internal static List<(int Position, int Id)> GetDeclaredOverrides(DependencyNode node)
    {
        var declaredOverrides = new List<(int Position, int Id)>();
        if (node.Factory is not {} factory)
        {
            return declaredOverrides;
        }

        var position = 0;
        foreach (var overrides in factory.Resolvers.Select(i => (i.Source.Position, i.Overrides))
                     .Concat(factory.Initializers.Select(i => (i.Source.Position, i.Overrides)))
                     .OrderBy(i => i.Position)
                     .Select(i => i.Overrides))
        {
            position++;
            foreach (var @override in overrides)
            {
                declaredOverrides.Add((position, factory.ResolveOverride(@override).Source.Id));
            }
        }

        return declaredOverrides;
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
