namespace Pure.DI.Core.Code;

interface ICycleTools
{
    bool IsCyclic(
        IGraph<DependencyNode, Dependency> graph,
        DependencyNode node);

    /// <summary>
    /// Determines whether the node depends on itself through the nodes that <paramref name="canPass"/> lets through.
    /// </summary>
    bool IsCyclic(
        IGraph<DependencyNode, Dependency> graph,
        DependencyNode node,
        Func<DependencyNode, bool> canPass);

    /// <summary>
    /// Determines whether the node <paramref name="from"/> depends on the node <paramref name="to"/>.
    /// </summary>
    bool DependsOn(
        IGraph<DependencyNode, Dependency> graph,
        DependencyNode from,
        DependencyNode to);
}
