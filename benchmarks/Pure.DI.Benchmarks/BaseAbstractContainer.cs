namespace Pure.DI.Benchmarks;

using Model;

abstract class BaseAbstractContainer<TActualContainer> : IAbstractContainer<TActualContainer>
{
    public TActualContainer? TryCreate()
    {
        try
        {
            return Resolve<ICompositionRoot>().Verify() ? CreateContainer() : default;
        }
        catch
        {
            return default;
        }
    }

    public abstract IAbstractContainer<TActualContainer> Bind(
        Type contractType,
        Type implementationType,
        AbstractLifetime lifetime = AbstractLifetime.Transient,
        string? name = null);

    public abstract void Dispose();

    public abstract TActualContainer CreateContainer();

    public abstract T Resolve<T>() where T : class;
}