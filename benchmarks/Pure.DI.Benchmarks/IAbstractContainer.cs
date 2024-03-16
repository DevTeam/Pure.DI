namespace Pure.DI.Benchmarks;

public interface IAbstractContainer<out TActualContainer> : IDisposable
{
    TActualContainer? TryCreate();

    IAbstractContainer<TActualContainer> Bind(
        Type contractType,
        Type implementationType,
        AbstractLifetime lifetime = AbstractLifetime.Transient,
        string? name = default);
}