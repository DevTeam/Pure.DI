namespace Pure.DI.Core;

interface IDependencyGraphLocationsWalker
{
    IReadOnlyCollection<Location> Locations { get; }

    void VisitDependencyNode(in Unit ctx, DependencyNode node);
}