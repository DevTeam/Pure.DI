namespace Pure.DI.Core;

internal sealed class DependenciesToInjectionsWalker: DependenciesWalker<Unit>, IEnumerable<InjectionInfo>
{
    private readonly List<InjectionInfo> _injections = [];

    public override void VisitInjection(
        in Unit ctx,
        in Injection injection,
        bool hasExplicitDefaultValue,
        object? explicitDefaultValue,
        in ImmutableArray<Location> locations)
    {
        _injections.Add(new InjectionInfo(injection, hasExplicitDefaultValue, explicitDefaultValue));
        base.VisitInjection(Unit.Shared, in injection, hasExplicitDefaultValue, explicitDefaultValue, locations);
    }

    public IEnumerator<InjectionInfo> GetEnumerator() => _injections.GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}