namespace Pure.DI.Core;

class ProcessingNodeBuilder(
    IFastBuilder<ContractsBuildContext, ISet<Injection>> contractsBuilder,
    Func<IInjectionsWalker> injectionsWalkerFactory)
    : IFastBuilder<ProcessingNodeContext, IProcessingNode>
{
    public IProcessingNode Build(in ProcessingNodeContext ctx) =>
        ctx.Cache.Get(new ProcessingNodeKey(ctx.Node.Variation, ctx.Node, ctx.ContextTag) { Contracts =  ctx.Contracts }, Build);

    private IProcessingNode Build(ProcessingNodeKey key)
    {
        var contracts = key.Contracts ?? contractsBuilder.Build(new ContractsBuildContext(key.Node.Binding, key.ContextTag, key.ContextTag));
        var injectionsWalker = injectionsWalkerFactory();
        injectionsWalker.VisitDependencyNode(Unit.Shared, key.Node);
        var injections = injectionsWalker.GetResult();
        return new ProcessingNode(key.Node, contracts, injections);
    }
}