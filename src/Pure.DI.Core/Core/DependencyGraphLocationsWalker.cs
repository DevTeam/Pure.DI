namespace Pure.DI.Core;

internal class DependencyGraphLocationsWalker: DependenciesWalker
{
    private readonly Injection _injection;
    private readonly ImmutableArray<Location>.Builder _locationsBuilder = ImmutableArray.CreateBuilder<Location>();

    public DependencyGraphLocationsWalker(in Injection injection)
    {
        _injection = injection;
    }

    public ImmutableArray<Location> Locations => _locationsBuilder.ToImmutableArray();

    public override void VisitInjection(in Injection injection, in ImmutableArray<Location> locations)
    {
        if (injection.Equals(_injection))
        {
            _locationsBuilder.AddRange(locations);
        }

        base.VisitInjection(in injection, in locations);
    }
}