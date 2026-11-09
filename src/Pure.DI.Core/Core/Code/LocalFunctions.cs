namespace Pure.DI.Core.Code;

using static Lifetime;

class LocalFunctions(INodeTools nodeTools, IAccumulators accumulators, IRefSafety refSafety, ICycleTools cycleTools): ILocalFunctions
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
        var nodeAccumulators = GetAccumulators(ctx);
        if (nodeAccumulators.Count != 0
            && !nodeAccumulators.All(ctx.LocalFunctionScope.CanSee)
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
    public LocalFunctionScope GetScope(CodeContext ctx) =>
        IsOverrideConsumer(ctx) && !IsOverrideArgumentsConsumer(ctx) || GetAccumulators(ctx).Count != 0
            ? ctx.LocalFunctionScope
            : ctx.RootContext.LocalFunctionScope;

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
        var isDeferredCycle = ctx.VarInjection.Var.HasCycle == true && GetAccumulators(ctx).Count == 0;
        var arguments = new Dictionary<int, Dependency>();
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

            // Factories may inject only in a branch or loop, so their PerBlock dependencies
            // must not be evaluated eagerly at the call site of a helper.
            if (node.Factory is not null && ctx.RootContext.UseSites.PerBlockDependents.Contains(node.BindingId))
            {
                return default;
            }

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

                if (source.ActualLifetime is PerBlock)
                {
                    if (source.Construct is { Source.Kind: MdConstructKind.Accumulator }
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
                    stack.Push(source);
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
            key = $"{key}:{string.Join(",", nodeAccumulators.Select(i => i.VarInjection.Var.Name))}";
        }

        // A transient instance is created by the code of each injection, which can depend on the tag of this injection.
        if (!nodeTools.IsBlock(var.AbstractNode))
        {
            var tag = ReferenceEquals(injection.Tag, MdTag.ContextTag) ? ctx.ContextTag : injection.Tag;
            key = $"{key}#{tag}";
        }

        return key;
    }

    // A shared instance does not feed the per-resolve accumulators, see RootCodeBuilder.
    private IReadOnlyCollection<Accumulator> GetAccumulators(CodeContext ctx)
    {
        if (ctx.Accumulators.IsDefaultOrEmpty)
        {
            return [];
        }

        var node = ctx.VarInjection.Var.AbstractNode;
        var graph = ctx.RootContext.Graph;
        return nodeTools.IsSharedInstance(node.Node, graph) && !accumulators.GetBoundaryAccumulators(graph, node).Any()
            ? ctx.Accumulators.Where(i => i.Lifetime is Singleton or Scoped).ToList()
            : ctx.Accumulators;
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
        return var.AbstractNode is { ActualLifetime: Transient, Arg: null }
               && (node.Implementation is not null || node.Factory is not null && !nodeTools.IsLazy(node, ctx.RootContext.Graph))
               && (var.HasCycle != true || IsCycleCandidate(ctx))
               && !refSafety.IsMaybeRefLike(var.InstanceType)
               && !GetPerBlockArguments(ctx).IsDefault;
    }

    // An instance that depends on itself only through lambdas, such as Func<T> or Lazy<T>.
    public bool IsDeferredCycle(CodeContext ctx) =>
        ctx.VarInjection.Var.HasCycle == true
        && IsTransientCandidate(ctx);

    // A cycle that does not pass through a lambda would create the instance again on each call of its local function.
    // An instance created under accumulators gets the scope of the code that declares them, and a lambda declares accumulators
    // of its own, so the lambdas on the cycle could not find the local function of the instance by its scope.
    private bool IsCycleCandidate(CodeContext ctx)
    {
        if (GetAccumulators(ctx).Count != 0)
        {
            return false;
        }

        var graph = ctx.RootContext.Graph;
        var root = ctx.VarInjection.Var.AbstractNode.Node;
        return !cycleTools.IsCyclic(graph.Graph, root, node => !nodeTools.IsLazy(node, graph))
               && CountLambdasBackTo(graph, root) >= MinCycleLambdas;
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
    private static bool IsOverrideConsumer(CodeContext ctx) =>
        ctx.HasOverrides
        && ctx.RootContext.UseSites.FreeOverrides.TryGetValue(ctx.VarInjection.Var.Declaration.Node.Node.BindingId, out var overrides)
        && !overrides.IsDefaultOrEmpty;
}
