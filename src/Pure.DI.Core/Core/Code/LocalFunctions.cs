namespace Pure.DI.Core.Code;

using static Lifetime;

class LocalFunctions(INodeTools nodeTools): ILocalFunctions
{
    private const int MinBodyCost = 4;
    private const int MinUseSites = 2;

    public bool UseFor(CodeContext ctx)
    {
        // A local function is shared by every use site in its scope, so it cannot add to
        // the accumulator of one particular resolve.
        if (ctx.Accumulators.Length != 0)
        {
            return false;
        }

        var node = ctx.VarInjection.Var.AbstractNode;
        if (!nodeTools.IsBlock(node))
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

        var isLockRequired = ctx.RootContext.IsThreadSafeEnabled
                             && node.ActualLifetime is Singleton or Scoped;
        return nodeTools.EstimateBodyCost(node, isLockRequired) >= MinBodyCost;
    }

    // A local function of the root cannot see a value overridden inside a lambda,
    // so a node that reads such a value gets the scope of the injection that overrides it.
    public LocalFunctionScope GetScope(CodeContext ctx) =>
        IsOverrideConsumer(ctx) ? ctx.LocalFunctionScope : ctx.RootContext.LocalFunctionScope;

    private static bool IsOverrideConsumer(CodeContext ctx) =>
        ctx.HasOverrides
        && ctx.RootContext.UseSites.OverrideConsumers.Contains(ctx.VarInjection.Var.Declaration.Node.Node.BindingId);
}
