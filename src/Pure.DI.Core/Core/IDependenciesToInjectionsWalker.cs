namespace Pure.DI.Core;

internal interface IDependenciesToInjectionsWalker
{
    IReadOnlyList<InjectionInfo> GetResult();
    void VisitDependencyNode(in Unit ctx, DependencyNode node);
}