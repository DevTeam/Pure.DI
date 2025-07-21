namespace Pure.DI.Core;

sealed class DependencyGraphLocationsWalker(ILocationProvider locationProvider, Injection targetInjection)
    : DependenciesWalker<Unit>(locationProvider), IDependencyGraphLocationsWalker
{
    private readonly ImmutableArray<Location>.Builder _locationsBuilder = ImmutableArray.CreateBuilder<Location>();

    public IReadOnlyCollection<Location> Locations => _locationsBuilder.ToList();

    public override void VisitInjection(
        in Unit ctx,
        in Injection injection,
        bool hasExplicitDefaultValue,
        object? explicitDefaultValue,
        in ImmutableArray<Location> locations,
        int? position)
    {
        if (injection.Equals(targetInjection))
        {
            _locationsBuilder.AddRange(locations);
        }

        base.VisitInjection(ctx, in injection, hasExplicitDefaultValue, explicitDefaultValue, in locations, position);
    }
}