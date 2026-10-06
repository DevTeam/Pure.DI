namespace Pure.DI.Core.Code;

using static Lifetime;

class LocalFunctions(INodeTools nodeTools): ILocalFunctions
{
    private const int MinBodyCost = 4;
    private const int MinUseSites = 2;

    public bool UseFor(CodeContext ctx)
    {
        // A local function is shared by every use site in the root, so it can neither add to
        // the accumulator of one particular resolve nor read a value overridden inside one lambda.
        if (ctx.Accumulators.Length != 0)
        {
            return false;
        }

        var node = ctx.VarInjection.Var.AbstractNode;
        if (!nodeTools.IsBlock(node))
        {
            return false;
        }

        var bindingId = ctx.VarInjection.Var.Declaration.Node.Node.BindingId;
        var useSites = ctx.RootContext.UseSites;
        if (ctx.HasOverrides && useSites.OverrideConsumers.Contains(bindingId))
        {
            return false;
        }

        // Inside a shared instance, the accumulators it set aside may be declared in a lambda the
        // local function cannot see, so a node that injects one of them stays inline.
        if (!ctx.SetAsideAccumulators.IsDefaultOrEmpty && useSites.AccumulatorConsumers.Contains(bindingId))
        {
            return false;
        }

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
}
