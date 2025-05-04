namespace Pure.DI.Core;

sealed class InjectionsWalker(ILocationProvider locationProvider)
    : DependenciesWalker<Unit>(locationProvider), IInjectionsWalker
{
    private readonly List<InjectionInfo> _result = [];

    public IReadOnlyCollection<InjectionInfo> GetResult() => _result;

    public override void VisitInjection(
        in Unit ctx,
        in Injection injection,
        bool hasExplicitDefaultValue,
        object? explicitDefaultValue,
        in ImmutableArray<Location> locations,
        int? position)
    {
        _result.Add(new InjectionInfo(injection, hasExplicitDefaultValue, explicitDefaultValue, position));
        base.VisitInjection(Unit.Shared, in injection, hasExplicitDefaultValue, explicitDefaultValue, locations, position);
    }
}