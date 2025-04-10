namespace Pure.DI.Core;

sealed class DependencyGraphLocationsWalker : DependenciesWalker<Unit>, IDependencyGraphLocationsWalker
{
    private readonly ImmutableArray<Location>.Builder _locationsBuilder = ImmutableArray.CreateBuilder<Location>();
    private Injection? _injection;

    public IDependencyGraphLocationsWalker Initialize(Injection injection)
    {
        _injection = injection;
        return this;
    }

    public IReadOnlyCollection<Location> Locations => _locationsBuilder.ToList();

    public override void VisitInjection(
        in Unit ctx,
        in Injection injection,
        bool hasExplicitDefaultValue,
        object? explicitDefaultValue,
        in ImmutableArray<Location> locations,
        int? position)
    {
        if (injection.Equals(_injection))
        {
            _locationsBuilder.AddRange(locations);
        }

        base.VisitInjection(ctx, in injection, hasExplicitDefaultValue, explicitDefaultValue, in locations, position);
    }
}