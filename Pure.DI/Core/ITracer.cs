namespace Pure.DI.Core;

internal interface ITracer
{
    IEnumerable<Dependency[]> Paths { get; }

    IDisposable RegisterResolving(Dependency dependency);

    void Save();

    void Reset();
}