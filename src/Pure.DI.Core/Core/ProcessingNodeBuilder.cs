namespace Pure.DI.Core;

class ProcessingNodeBuilder(
    IBuilder<ContractsBuildContext, ISet<Injection>> contractsBuilder,
    Func<IInjectionsWalker> injectionsWalkerFactory)
    : IBuilder<ProcessingNodeContext, IProcessingNode>
{
    public IProcessingNode Build(ProcessingNodeContext ctx)
    {
        var node = ctx.Node;
        return ctx.Cache.Get(new ProcessingNodeKey(node, node.Variation, ctx.ContextTag), key => {
            var contracts = ctx.Contracts ?? contractsBuilder.Build(new ContractsBuildContext(key.Node.Binding, key.ContextTag, key.ContextTag));
            var injectionsWalker = injectionsWalkerFactory();
            injectionsWalker.VisitDependencyNode(Unit.Shared, node);
            var injections = injectionsWalker.GetResult();
            return new ProcessingNode(node, contracts, injections);
        });
    }
}