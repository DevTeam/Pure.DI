namespace Pure.DI.Core.Code;

using static Lifetime;

class LocalFunctions(INodeTools nodeTools, IAccumulators accumulators, IRefSafety refSafety): ILocalFunctions
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

        // A node that reads an overridden value needs a scope declared next to that value.
        if (IsOverrideConsumer(ctx) && ReferenceEquals(ctx.LocalFunctionScope, ctx.RootContext.LocalFunctionScope))
        {
            return false;
        }

        var bindingId = ctx.VarInjection.Var.Declaration.Node.Node.BindingId;
        var useSites = ctx.RootContext.UseSites;
        if (!useSites.UseSiteCount.TryGetValue(bindingId, out var count) || count < MinUseSites)
        {
            return false;
        }

        var mode = ctx.RootContext.Graph.Source.Hints.LocalFunctions;
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
    // A node created under accumulators gets the scope of the code that declares them,
    // so its local function is not called where other accumulators collect the instances.
    public LocalFunctionScope GetScope(CodeContext ctx) =>
        IsOverrideConsumer(ctx) || GetAccumulators(ctx).Count != 0
            ? ctx.LocalFunctionScope
            : ctx.RootContext.LocalFunctionScope;

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

    // Only an instance created by its constructor is returned by a local function. A ref-like value
    // cannot be returned this way in general, and a PerBlock dependency would be created anew on each call.
    private bool IsTransientCandidate(CodeContext ctx)
    {
        var var = ctx.VarInjection.Var;
        return var.AbstractNode is { ActualLifetime: Transient, Arg: null, Node.Implementation: not null }
               && var.HasCycle != true
               && !refSafety.IsMaybeRefLike(var.InstanceType)
               && !ctx.RootContext.UseSites.PerBlockDependents.Contains(var.AbstractNode.BindingId);
    }

    private static bool IsOverrideConsumer(CodeContext ctx) =>
        ctx.HasOverrides
        && ctx.RootContext.UseSites.OverrideConsumers.Contains(ctx.VarInjection.Var.Declaration.Node.Node.BindingId);
}
