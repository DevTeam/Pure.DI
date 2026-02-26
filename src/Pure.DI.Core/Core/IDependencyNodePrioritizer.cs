namespace Pure.DI.Core;

interface IDependencyNodePrioritizer
{
    IEnumerable<DependencyNode> SortByPriority(IEnumerable<DependencyNode> nodes);
}