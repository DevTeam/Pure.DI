namespace Pure.DI.Core;

internal sealed class DependencyGraphLocationsWalker(in Injection injection)
    : DependenciesWalker<Unit>
{
    private readonly Injection _injection = injection;
    private readonly ImmutableArray<Location>.Builder _locationsBuilder = ImmutableArray.CreateBuilder<Location>();

    public ImmutableArray<Location> Locations => _locationsBuilder.ToImmutableArray();

    public override void VisitInjection(
        in Unit ctx,
        in Injection injection,
        bool hasExplicitDefaultValue,
        object? explicitDefaultValue,
        in ImmutableArray<Location> locations)
    {
        if (injection.Equals(_injection))
        {
            _locationsBuilder.AddRange(locations);
        }

        base.VisitInjection(ctx, in injection, hasExplicitDefaultValue, explicitDefaultValue, in locations);
    }
}