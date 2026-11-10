namespace Pure.DI.Core.Code;

using static Lifetime;

class LocalFunctions(
    INodeTools nodeTools,
    IAccumulators accumulators,
    IRefSafety refSafety,
    ICycleTools cycleTools,
    ITypeSymbolComparer typeSymbolComparer): ILocalFunctions
{
    internal const int MinBodyCost = 4;
    internal const int MinUseSites = 2;
    // The size of the code a transient instance may repeat across its use sites before it gets a local function.
    private const int MaxRepeatedTransientCost = 32;

    public bool UseFor(CodeContext ctx)
    {
        var node = ctx.VarInjection.Var.AbstractNode;
        var isBlock = nodeTools.IsBlock(node);
        if (!isBlock && !IsTransientCandidate(ctx))
        {
            return false;
        }

        // A local function that adds to accumulators or injects one refers to their variables,
        // so they must be declared before the place of its scope.
        // An instance on a cycle through lambdas gets the accumulators as arguments instead.
        var nodeAccumulators = GetAccumulators(ctx);
        if (nodeAccumulators.Count != 0
            && !nodeAccumulators.All(ctx.LocalFunctionScope.CanSee)
            && !IsDeferredCycle(ctx)
            && (nodeAccumulators.Any(i => !i.IsEmpty)
                || accumulators.InjectsAccumulator(
                    ctx.RootContext.Graph,
                    node,
                    nodeAccumulators.Select(i => i.VarInjection.Var.AbstractNode.BindingId).ToImmutableHashSet())))
        {
            return false;
        }

        // A node that reads an overridden value needs a scope declared next to that value,
        // unless it is a transient instance that gets the overridden values as arguments.
        if (IsOverrideConsumer(ctx) && !IsOverrideArgumentsConsumer(ctx) && ReferenceEquals(ctx.LocalFunctionScope, ctx.RootContext.LocalFunctionScope))
        {
            return false;
        }

        var mode = ctx.RootContext.Graph.Source.Hints.LocalFunctions;

        // A transient instance on a cycle through lambdas, such as screens that open each other by Func<T1, T>,
        // is created by one local function that the lambdas call. Otherwise, each lambda creates it again
        // with the lambdas of its own dependencies, and the code grows with the number of paths on the cycle, see #192.
        if (!isBlock && mode != LocalFunctionsMode.Off && IsDeferredCycle(ctx))
        {
            return true;
        }

        // A shared instance on a cycle with several paths through it is checked by each of the lambdas that lead to it.
        // It can have a single use site, for example, under Lazy<T> or Owned<T>, whose code is repeated in each of these lambdas,
        // so the code of the instance created in place grew with the number of paths on the cycle.
        if (isBlock && mode != LocalFunctionsMode.Off && IsOnBranchingCycle(ctx, node.Node))
        {
            return true;
        }

        var bindingId = ctx.VarInjection.Var.Declaration.Node.Node.BindingId;
        var useSites = ctx.RootContext.UseSites;
        if (!useSites.UseSiteCount.TryGetValue(bindingId, out var count) || count < MinUseSites)
        {
            return false;
        }

        if (!isBlock)
        {
            return mode switch
            {
                LocalFunctionsMode.On => true,
                LocalFunctionsMode.Off => false,
                _ => useSites.TransientCost.TryGetValue(bindingId, out var transientCost)
                     && IsTransientCostTooHigh(count, transientCost)
            };
        }

        // A factory anywhere up the dependency chain may contain branching (if/else, switch),
        // which breaks the inline "first use initializes; later uses reuse" assumption.
        // Always wrap in a local function so every use site re-checks init.
        if (useSites.FactoryDownstream.Contains(bindingId))
        {
            return true;
        }

        switch (mode)
        {
            case LocalFunctionsMode.On:
                return true;

            case LocalFunctionsMode.Off:
                return false;
        }

        if (useSites.InlineCost.TryGetValue(bindingId, out var inlineCost))
        {
            return inlineCost >= MinBodyCost;
        }

        var isLockRequired = ctx.RootContext.IsThreadSafeEnabled
                             && node.ActualLifetime is Singleton or Scoped;
        return nodeTools.EstimateBodyCost(node, isLockRequired) >= MinBodyCost;
    }

    // A local function of the root cannot see a value overridden inside a lambda,
    // so a node that reads such a value gets the scope of the injection that overrides it.
    // A transient instance gets such values as arguments instead, so the lambdas that create it share its local function, see #178.
    // A node created under accumulators gets the scope of the code that declares them,
    // so its local function is not called where other accumulators collect the instances.
    // An instance on a cycle through lambdas gets the accumulators as arguments, so the lambdas on the cycle,
    // which declare accumulators of their own, such as Owned<T>, call the same local function, see #192.
    public LocalFunctionScope GetScope(CodeContext ctx) =>
        IsOverrideConsumer(ctx) && !IsOverrideArgumentsConsumer(ctx) || GetAccumulators(ctx).Count != 0 && !IsDeferredCycle(ctx)
            ? ctx.LocalFunctionScope
            : ctx.RootContext.LocalFunctionScope;

    // The accumulators that the local function of an instance on a cycle through lambdas gets as arguments.
    public ImmutableArray<Accumulator> GetAccumulatorArguments(CodeContext ctx) =>
        IsDeferredCycle(ctx)
            ? GetAccumulators(ctx).ToImmutableArray()
            : ImmutableArray<Accumulator>.Empty;

    // The overridden values that the local function of a transient instance gets as arguments.
    public ImmutableArray<MdOverride> GetOverrideArguments(CodeContext ctx) =>
        IsOverrideConsumer(ctx)
        && IsOverrideArgumentsConsumer(ctx)
        && ctx.RootContext.UseSites.FreeOverrides.TryGetValue(ctx.VarInjection.Var.Declaration.Node.Node.BindingId, out var overrides)
            ? overrides
            : ImmutableArray<MdOverride>.Empty;

    public ImmutableArray<Dependency> GetPerBlockArguments(CodeContext ctx)
    {
        var root = ctx.VarInjection.Var.AbstractNode.Node;
        if (!ctx.RootContext.UseSites.PerBlockDependents.Contains(root.BindingId))
        {
            return ImmutableArray<Dependency>.Empty;
        }

        var graph = ctx.RootContext.Graph;
        var isDeferredCycle = ctx.VarInjection.Var.HasCycle == true;
        var arguments = new Dictionary<int, Dependency>();
        var visited = new HashSet<int>();
        var visitedUnderFactory = new HashSet<int>();
        var stack = new Stack<(DependencyNode Node, bool IsUnderFactory)>();
        stack.Push((root, false));
        while (stack.Count > 0)
        {
            var (node, isUnderFactory) = stack.Pop();
            if (!visited.Add(node.BindingId))
            {
                // The PerBlock dependencies of a node reached again under a factory are checked under the factory too.
                if (!isUnderFactory || !visitedUnderFactory.Add(node.BindingId))
                {
                    continue;
                }
            }
            else if (isUnderFactory)
            {
                visitedUnderFactory.Add(node.BindingId);
            }

            isUnderFactory |= node.Factory is not null && ctx.RootContext.UseSites.PerBlockDependents.Contains(node.BindingId);
            if (!graph.Graph.TryGetInEdges(node, out var dependencies))
            {
                continue;
            }

            foreach (var dependency in dependencies)
            {
                var source = dependency.Source;
                // A lambda that leads back to the instance on a cycle is created inside its local function,
                // so the local function does not need the lambda from its caller, which would need the same lambdas again.
                if (isDeferredCycle
                    && source.ActualLifetime is PerBlock
                    && nodeTools.IsLazy(source, graph)
                    && cycleTools.DependsOn(graph.Graph, source, root))
                {
                    continue;
                }

                // A lambda that the code around this instance does not share with it is created by its local function too.
                if (source.ActualLifetime is PerBlock
                    && nodeTools.IsLazy(source, graph)
                    && ctx.RootContext.UseSites.PrivateLambdas.TryGetValue(root.BindingId, out var privateLambdas)
                    && privateLambdas.Contains(source.BindingId))
                {
                    continue;
                }

                // A PerBlock dependency that only this instance holds in each block is created by its local function too.
                if (source.ActualLifetime is PerBlock
                    && source.Arg is null
                    && !isUnderFactory
                    && ReadsOverrides(ctx, source)
                    && IsCreatedOncePerBlock(ctx, source))
                {
                    stack.Push((source, isUnderFactory));
                    continue;
                }

                if (source.ActualLifetime is PerBlock)
                {
                    // Factories may inject only in a branch or loop, so their PerBlock dependencies
                    // must not be evaluated eagerly at the call site of a helper. A factory that injects
                    // only the lambdas created inside the local function has no such dependencies.
                    if (isUnderFactory
                        || source.Construct is { Source.Kind: MdConstructKind.Accumulator }
                        || refSafety.IsMaybeRefLike(source.Type)
                        || ReferenceEquals(dependency.Injection.Tag, MdTag.ContextTag))
                    {
                        return default;
                    }

                    // A shared binding can be injected with different tags. Its first creation
                    // chooses the tag, so keep the original traversal instead of choosing a tag
                    // while collecting arguments for a helper.
                    if (arguments.TryGetValue(source.BindingId, out var previous)
                        && !Equals(previous.Injection.Tag, dependency.Injection.Tag))
                    {
                        return default;
                    }

                    arguments[source.BindingId] = dependency;
                }
                else if (!nodeTools.IsBlock(source) && !nodeTools.IsLazy(source, graph))
                {
                    stack.Push((source, isUnderFactory));
                }
            }
        }

        return arguments.OrderBy(i => i.Key).Select(i => i.Value).ToImmutableArray();
    }

    // An instance can be created under different accumulators in the same scope,
    // for example, inside a shared instance that does not feed the per-resolve ones,
    // so each set of accumulators gets a local function of its own.
    public string GetKey(CodeContext ctx)
    {
        var (var, injection) = ctx.VarInjection;
        var key = var.AbstractNode.BindingId.ToString(System.Globalization.CultureInfo.InvariantCulture);
        var nodeAccumulators = GetAccumulators(ctx);
        if (nodeAccumulators.Count != 0)
        {
            // The local function of an instance on a cycle gets the accumulators as arguments, so it depends only on their kinds.
            key = IsDeferredCycle(ctx)
                ? $"{key}:{string.Join(",", nodeAccumulators.Select(i => i.VarInjection.Var.AbstractNode.BindingId))}"
                : $"{key}:{string.Join(",", nodeAccumulators.Select(i => i.VarInjection.Var.Name))}";
        }

        // A transient instance is created by the code of each injection, which depends on the tag of this injection
        // when a factory reads the tag. Otherwise, the injections of an instance by different tags share its local function.
        // A factory that only passes the tag to its injections, such as Lazy<T> or Owned<T>, is a node of its own for each tag,
        // and its code is the same for the tags that lead to the same dependencies, so their nodes share a local function.
        if (!nodeTools.IsBlock(var.AbstractNode) && nodeTools.ReadsContextMember(var.AbstractNode.Node, nameof(IContext.Tag)))
        {
            if (TryGetTagFreeKey(ctx.RootContext.Graph, var.AbstractNode.Node, [], out var tagFreeKey))
            {
                return $"{tagFreeKey}{key.Substring(var.AbstractNode.BindingId.ToString(System.Globalization.CultureInfo.InvariantCulture).Length)}";
            }

            var tag = ReferenceEquals(injection.Tag, MdTag.ContextTag) ? ctx.ContextTag : injection.Tag;
            key = $"{key}#{tag}";
        }

        return key;
    }

    // The key of a factory that passes the tag of its injection only to its injections by ctx.Inject(ctx.Tag, ...):
    // the factory binding and the nodes that these injections lead to.
    private bool TryGetTagFreeKey(DependencyGraph graph, DependencyNode node, HashSet<int> visited, out string key)
    {
        key = "";
        if (node.Factory is not {} factory || ReadsTagAsValue(factory) || !visited.Add(node.BindingId))
        {
            return false;
        }

        var sources = new List<string>();
        if (graph.Graph.TryGetInEdges(node, out var dependencies))
        {
            foreach (var dependency in dependencies)
            {
                var source = dependency.Source;
                // A dependency that reads the tag, such as Func<T> of Lazy<T>, has the same code for the tags
                // that lead to the same dependencies of its own.
                if (nodeTools.ReadsContextMember(source, nameof(IContext.Tag)))
                {
                    if (!TryGetTagFreeKey(graph, source, visited, out var sourceKey))
                    {
                        return false;
                    }

                    sources.Add(sourceKey);
                    continue;
                }

                sources.Add(source.BindingId.ToString(System.Globalization.CultureInfo.InvariantCulture));
            }
        }

        var bindingId = node.Binding.OriginalIds.IsDefaultOrEmpty ? node.Binding.Id : node.Binding.OriginalIds[0];
        key = $"t{bindingId}:{node.Type}({string.Join(",", sources)})";
        return true;
    }

    // ctx.Tag outside ctx.Inject(ctx.Tag, out ...) is a value the code of the factory depends on.
    private static bool ReadsTagAsValue(DpFactory factory)
    {
        var contextName = factory.Source.Context.Identifier.ValueText;
        foreach (var member in factory.Source.Factory.DescendantNodes().OfType<MemberAccessExpressionSyntax>())
        {
            if (member.Expression is not IdentifierNameSyntax identifier
                || identifier.Identifier.ValueText != contextName
                || member.Name.Identifier.ValueText != nameof(IContext.Tag))
            {
                continue;
            }

            if (member.Parent is ArgumentSyntax { Parent: ArgumentListSyntax { Parent: InvocationExpressionSyntax { Expression: MemberAccessExpressionSyntax inject } } }
                && inject.Expression is IdentifierNameSyntax injectContext
                && injectContext.Identifier.ValueText == contextName
                && inject.Name.Identifier.ValueText == nameof(IContext.Inject))
            {
                continue;
            }

            return true;
        }

        return false;
    }

    // A shared instance does not feed the per-resolve accumulators, see RootCodeBuilder.
    // A node that neither feeds the accumulators nor injects them, together with the nodes created in place with it,
    // does not depend on them, so its local function is shared by the code under different accumulators,
    // for example, by the screens that lambdas create under Owned<T>.
    private IReadOnlyCollection<Accumulator> GetAccumulators(CodeContext ctx)
    {
        if (ctx.Accumulators.IsDefaultOrEmpty)
        {
            return [];
        }

        var node = ctx.VarInjection.Var.AbstractNode;
        var graph = ctx.RootContext.Graph;
        var nodeAccumulators = nodeTools.IsSharedInstance(node.Node, graph) && !accumulators.GetBoundaryAccumulators(graph, node).Any()
            ? ctx.Accumulators.Where(i => i.Lifetime is Singleton or Scoped).ToList()
            : (IReadOnlyCollection<Accumulator>)ctx.Accumulators;
        if (nodeAccumulators.Count == 0 || UsesAccumulators(ctx, nodeAccumulators))
        {
            return nodeAccumulators;
        }

        return [];
    }

    private bool UsesAccumulators(CodeContext ctx, IReadOnlyCollection<Accumulator> nodeAccumulators)
    {
        var graph = ctx.RootContext.Graph;
        var root = ctx.VarInjection.Var.AbstractNode.Node;
        var bindingIds = nodeAccumulators.Select(i => i.VarInjection.Var.AbstractNode.BindingId).ToImmutableHashSet();
        if (accumulators.InjectsAccumulator(graph, root, bindingIds))
        {
            return true;
        }

        var visited = new HashSet<int>();
        var stack = new Stack<DependencyNode>();
        stack.Push(root);
        while (stack.Count > 0)
        {
            var node = stack.Pop();
            if (!visited.Add(node.BindingId))
            {
                continue;
            }

            // A lambda or a boundary, such as Owned<T>, declares accumulators of its own, so the code under it uses them too.
            if (nodeTools.IsLazy(node, graph)
                || node.Construct is { Source.Kind: MdConstructKind.Accumulator }
                || accumulators.GetBoundaryAccumulators(graph, node).Any())
            {
                return true;
            }

            if (node.Arg is null && nodeAccumulators.Any(accumulator => !accumulator.IsEmpty && accumulator.Lifetime == node.Lifetime && IsAssignableTo(node.Type, accumulator.Type)))
            {
                return true;
            }

            if (!graph.Graph.TryGetInEdges(node, out var dependencies))
            {
                continue;
            }

            foreach (var dependency in dependencies)
            {
                stack.Push(dependency.Source);
            }
        }

        return false;
    }

    private bool IsAssignableTo(ITypeSymbol type, ITypeSymbol contract)
    {
        for (var current = type; current is not null; current = current.BaseType)
        {
            if (typeSymbolComparer.RuntimeEquals(current, contract))
            {
                return true;
            }
        }

        return type.AllInterfaces.Any(i => typeSymbolComparer.RuntimeEquals(i, contract));
    }

    // A transient instance is created anew at each use site, so a local function that returns it
    // is called at each of them. Repeating a small piece of code is cheaper than a call.
    internal static bool IsTransientCostTooHigh(int useSiteCount, int cost) =>
        useSiteCount >= MinUseSites
        && (long)(useSiteCount - 1) * cost >= MaxRepeatedTransientCost;

    // An instance created by its constructor or by a factory is returned by a local function,
    // but not a lambda, such as Func<T> or Lazy<T>, whose code belongs to the place where it is created.
    // A ref-like value cannot be returned this way in general. Shared PerBlock dependencies
    // must be supplied by the caller, without moving a conditional injection out of a factory.
    private bool IsTransientCandidate(CodeContext ctx)
    {
        var var = ctx.VarInjection.Var;
        var node = var.AbstractNode.Node;
        return var.AbstractNode.Arg is null
               // An instance created for its consumers, for example, by ctx.ConsumerType, is created in the code of each of them.
               && !ctx.RootContext.UseSites.ConsumerDependents.Contains(node.BindingId)
               && (var.AbstractNode.ActualLifetime is Transient || IsPerBlockResultOfLambdasOnCycle(ctx))
               && (node.Implementation is not null || node.Factory is not null && !nodeTools.IsLazy(node, ctx.RootContext.Graph))
               && (var.HasCycle != true || IsCycleCandidate(ctx))
               && !refSafety.IsMaybeRefLike(var.InstanceType)
               && !GetPerBlockArguments(ctx).IsDefault;
    }

    // A PerBlock instance that only lambdas return, such as screens that open each other by Func<T>, is created once
    // in the block of each lambda, so it is created like a transient instance there. Its local function on a cycle
    // creates the lambdas of the instance, rather than each lambda repeating the code of the lambdas it leads to.
    // A lambda can return the instance in a PerBlock instance of its own, such as Owned<T>, which its block creates once too,
    // or in a transient instance that only this lambda returns, such as a router that the screen of the lambda gets.
    private bool IsPerBlockResultOfLambdasOnCycle(CodeContext ctx)
    {
        var var = ctx.VarInjection.Var;
        return var.AbstractNode.ActualLifetime is PerBlock
               && (var.HasCycle == true || ReadsOverrides(ctx, var.AbstractNode.Node))
               && IsCreatedOncePerBlock(ctx, var.AbstractNode.Node);
    }

    // Outside a cycle, only an instance that reads a value overridden by a lambda is created again by the code of the lambdas,
    // for example, a view model reached both under Func<T1, T> and outside it, which the lambdas of each level repeated.
    private static bool ReadsOverrides(CodeContext ctx, DependencyNode node) =>
        ctx.RootContext.UseSites.FreeOverrides.TryGetValue(node.BindingId, out var overrides) && !overrides.IsDefaultOrEmpty;

    private bool IsCreatedOncePerBlock(CodeContext ctx, DependencyNode root)
    {
        var graph = ctx.RootContext.Graph;
        var lambdas = new HashSet<int>();
        var holders = new HashSet<int>();
        var nodes = new Stack<DependencyNode>();
        nodes.Push(root);
        while (nodes.Count > 0)
        {
            if (!graph.Graph.TryGetOutEdges(nodes.Pop(), out var consumers))
            {
                continue;
            }

            foreach (var consumer in consumers)
            {
                var target = consumer.Target;
                // The code of a root creates the instance once, like the block of a lambda.
                if (target.Root is not null)
                {
                    continue;
                }

                if (nodeTools.IsLazy(target, graph))
                {
                    if (!lambdas.Add(target.BindingId))
                    {
                        return false;
                    }

                    continue;
                }

                // An instance that holds this one is reached once, so each block that creates it creates this one once.
                if (target is not { ActualLifetime: PerBlock or Transient, Arg: null } || !holders.Add(target.BindingId))
                {
                    return false;
                }

                nodes.Push(target);
            }
        }

        return lambdas.Count > 0;
    }

    // An instance that depends on itself only through lambdas, such as Func<T> or Lazy<T>.
    public bool IsDeferredCycle(CodeContext ctx) =>
        ctx.VarInjection.Var.HasCycle == true
        && IsTransientCandidate(ctx);

    // A cycle that does not pass through a lambda would create the instance again on each call of its local function.
    private bool IsCycleCandidate(CodeContext ctx)
    {
        // A detached accumulator is declared inside a lambda that the local function cannot see.
        if (!ctx.DetachedAccumulators.IsDefaultOrEmpty)
        {
            return false;
        }

        var graph = ctx.RootContext.Graph;
        var root = ctx.VarInjection.Var.AbstractNode.Node;
        return !cycleTools.IsCyclic(graph.Graph, root, node => !nodeTools.IsLazy(node, graph))
               && (CountLambdasBackTo(graph, root) >= MinCycleLambdas
                   || IsOnBranchingCycle(ctx, root)
                   || IsEnteredInSeveralPlaces(ctx, root)
                   || IsOverriddenOnCycle(ctx, root));
    }

    // The code of a cycle created in place is repeated on each path through the nodes of the cycle up to the lambda
    // that closes it. A single lambda can lead back to the instance by several paths, such as a lazy collection
    // of the screens that each screen gets, or a lambda under instances that share their dependencies,
    // so the code grew with the number of these paths.
    private static bool IsOnBranchingCycle(CodeContext ctx, DependencyNode root) =>
        ctx.RootContext.UseSites.BranchingCycles.Contains(root.BindingId);

    // The code of a cycle created in place is repeated from each place that enters the cycle up to the lambda that closes it,
    // such as a hub that opens each screen of a ring, so its code grew with the square of the number of screens.
    // Repeating the code of a small cycle is cheaper than a call.
    private static bool IsEnteredInSeveralPlaces(CodeContext ctx, DependencyNode root) =>
        ctx.RootContext.UseSites.CycleEntries.TryGetValue(root.BindingId, out var entries)
        && entries >= MinCycleLambdas
        && ctx.RootContext.UseSites.TransientCost.TryGetValue(root.BindingId, out var cost)
        && IsTransientCostTooHigh(entries, cost);

    // The code created in place closes the cycle by a lambda created on an earlier pass of the cycle.
    // If that lambda captures a value that another lambda on the cycle overrides, the lambda of the earlier pass
    // reads the value of that pass, rather than the value of the current one. The local function of the instance
    // creates the lambdas anew from the values it gets as arguments.
    // A lambda that overrides the value itself, such as Func<T1, T> that creates T with T1, reads its own argument.
    private bool IsOverriddenOnCycle(CodeContext ctx, DependencyNode root)
    {
        var graph = ctx.RootContext.Graph.Graph;
        var freeOverrides = ctx.RootContext.UseSites.FreeOverrides;
        var capturedIds = new HashSet<int>();
        var declaredIds = new HashSet<int>();
        var visited = new HashSet<int> { root.BindingId };
        var stack = new Stack<DependencyNode>();
        stack.Push(root);
        while (stack.Count > 0)
        {
            var node = stack.Pop();
            if (!graph.TryGetInEdges(node, out var dependencies))
            {
                continue;
            }

            foreach (var dependency in dependencies)
            {
                var source = dependency.Source;
                if (!visited.Add(source.BindingId))
                {
                    continue;
                }

                stack.Push(source);
                if (source.Factory is null || !cycleTools.DependsOn(graph, source, root))
                {
                    continue;
                }

                foreach (var (_, id) in RootUseSiteCounter.GetDeclaredOverrides(source))
                {
                    declaredIds.Add(id);
                }

                if (nodeTools.IsLazy(source, ctx.RootContext.Graph)
                    && freeOverrides.TryGetValue(source.BindingId, out var captured))
                {
                    foreach (var @override in captured)
                    {
                        capturedIds.Add(@override.Id);
                    }
                }
            }
        }

        return capturedIds.Overlaps(declaredIds);
    }

    // A single lambda that leads back to the instance repeats its code once per level of the cycle,
    // which the code reaches only once, so the code is linear and is created in place.
    // Several such lambdas double the paths to the instance on each level of the cycle.
    private const int MinCycleLambdas = 2;

    private int CountLambdasBackTo(DependencyGraph graph, DependencyNode root)
    {
        var count = 0;
        var visited = new HashSet<int>();
        var stack = new Stack<DependencyNode>();
        stack.Push(root);
        while (stack.Count > 0)
        {
            var node = stack.Pop();
            if (!visited.Add(node.BindingId) || !graph.Graph.TryGetInEdges(node, out var dependencies))
            {
                continue;
            }

            foreach (var dependency in dependencies)
            {
                var source = dependency.Source;
                if (nodeTools.IsLazy(source, graph))
                {
                    if (visited.Add(source.BindingId) && cycleTools.DependsOn(graph.Graph, source, root))
                    {
                        count++;
                    }
                }
                else if (!nodeTools.IsBlock(source))
                {
                    stack.Push(source);
                }
            }
        }

        return count;
    }

    private bool IsOverrideArgumentsConsumer(CodeContext ctx) =>
        !nodeTools.IsBlock(ctx.VarInjection.Var.AbstractNode) && IsTransientCandidate(ctx);

    // A node reads an overridden value of the lambda it is reached from only if it does not override that value itself
    // on the way to the value, for example, by a nested Func<T1, T>. Otherwise, a shared instance reached
    // from several such lambdas would get a local function in each of them, and its code would double per level, see #186.
    // A value that may be overridden is read by the code that no lambda above it overrides the value for too,
    // so the local function of a node that reads such a value gets it from each of its callers.
    private static bool IsOverrideConsumer(CodeContext ctx) =>
        ctx.RootContext.UseSites.FreeOverrides.TryGetValue(ctx.VarInjection.Var.Declaration.Node.Node.BindingId, out var overrides)
        && !overrides.IsDefaultOrEmpty
        && (ctx.HasOverrides || overrides.Any(i => ctx.RootContext.UseSites.MaybeOverrides.Contains(i.Id)));
}
