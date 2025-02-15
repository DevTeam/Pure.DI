namespace Pure.DI.Core;

internal interface IInjectionsWalker
{
    IReadOnlyCollection<InjectionInfo> GetResult();

    void VisitDependencyNode(in Unit ctx, DependencyNode node);
}