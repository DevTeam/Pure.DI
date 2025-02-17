namespace Pure.DI.Core;

interface IDependencyGraphLocationsWalker
{
    IReadOnlyCollection<Location> Locations { get; }

    IDependencyGraphLocationsWalker Initialize(Injection injection);

    void VisitDependencyNode(in Unit ctx, DependencyNode node);
}