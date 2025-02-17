namespace Pure.DI.Core;

interface IInjectionsWalker
{
    IReadOnlyCollection<InjectionInfo> GetResult();

    void VisitDependencyNode(in Unit ctx, DependencyNode node);
}