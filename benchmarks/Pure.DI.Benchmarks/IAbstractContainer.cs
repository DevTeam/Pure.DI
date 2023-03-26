namespace Pure.DI.Benchmarks;

public interface IAbstractContainer<out TActualContainer> : IDisposable
{
    TActualContainer? TryCreate();

    void Register(Type contractType, Type implementationType, AbstractLifetime lifetime = AbstractLifetime.Transient, string? name = default);
}