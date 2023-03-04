namespace Pure.DI.Core;

using System.Collections;

internal class DependenciesToInjectionsWalker: DependenciesWalker, IEnumerable<Injection>
{
    private readonly List<Injection> _injections = new();
    
    public override void VisitInjection(in Injection injection)
    {
        _injections.Add(injection);
        base.VisitInjection(in injection);
    }

    public IEnumerator<Injection> GetEnumerator() => _injections.GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}