namespace Pure.DI.Core;

internal sealed class DependenciesToInjectionsWalker: DependenciesWalker<Unit>, IEnumerable<Injection>
{
    private readonly List<Injection> _injections = new();

    public override void VisitInjection(in Unit ctx, in Injection injection, in ImmutableArray<Location> locations)
    {
        _injections.Add(injection);
        base.VisitInjection(Unit.Shared, in injection, locations);
    }

    public IEnumerator<Injection> GetEnumerator() => _injections.GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}