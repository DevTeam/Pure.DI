namespace Pure.DI.Core;

internal sealed class DependencyGraphLocationsWalker: DependenciesWalker<Unit>
{
    private readonly Injection _injection;
    private readonly ImmutableArray<Location>.Builder _locationsBuilder = ImmutableArray.CreateBuilder<Location>();

    public DependencyGraphLocationsWalker(in Injection injection)
    {
        _injection = injection;
    }

    public ImmutableArray<Location> Locations => _locationsBuilder.ToImmutableArray();

    public override void VisitInjection(in Unit ctx, in Injection injection, in ImmutableArray<Location> locations)
    {
        if (injection.Equals(_injection))
        {
            _locationsBuilder.AddRange(locations);
        }

        base.VisitInjection(ctx, in injection, in locations);
    }
}