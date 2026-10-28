namespace Pure.DI.Core.Code;

using static Lifetime;

class LocalFunctions(INodeTools nodeTools, IAccumulators accumulators): ILocalFunctions
{
    internal const int MinBodyCost = 4;
    internal const int MinUseSites = 2;

    public bool UseFor(CodeContext ctx)
    {
        var node = ctx.VarInjection.Var.AbstractNode;
        if (!nodeTools.IsBlock(node))
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

        // A factory anywhere up the dependency chain may contain branching (if/else, switch),
        // which breaks the inline "first use initializes; later uses reuse" assumption.
        // Always wrap in a local function so every use site re-checks init.
        if (useSites.FactoryDownstream.Contains(bindingId))
        {
            return true;
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
        var bindingId = ctx.VarInjection.Var.AbstractNode.BindingId.ToString(System.Globalization.CultureInfo.InvariantCulture);
        var nodeAccumulators = GetAccumulators(ctx);
        return nodeAccumulators.Count == 0
            ? bindingId
            : $"{bindingId}:{string.Join(",", nodeAccumulators.Select(i => i.VarInjection.Var.Name))}";
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

    private static bool IsOverrideConsumer(CodeContext ctx) =>
        ctx.HasOverrides
        && ctx.RootContext.UseSites.OverrideConsumers.Contains(ctx.VarInjection.Var.Declaration.Node.Node.BindingId);
}
