namespace Pure.DI.Core;

class ProcessingNodeBuilder(
    IFastBuilder<ContractsBuildContext, ISet<Injection>> contractsBuilder,
    Func<IInjectionsWalker> injectionsWalkerFactory)
    : IFastBuilder<ProcessingNodeContext, IProcessingNode>
{
    public IProcessingNode Build(in ProcessingNodeContext ctx)
    {
        var node = ctx.Node;
        var tag = ctx.ContextTag;
        var contracts = ctx.Contracts ?? contractsBuilder.Build(new ContractsBuildContext(node.Binding, tag, tag));
        var injectionsWalker = injectionsWalkerFactory();
        injectionsWalker.VisitDependencyNode(Unit.Shared, node);
        var injections = injectionsWalker.GetResult();
        return new ProcessingNode(node, contracts, injections);
    }
}