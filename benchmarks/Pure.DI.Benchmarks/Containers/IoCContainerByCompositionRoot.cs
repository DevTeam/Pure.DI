// ReSharper disable UnusedType.Global
namespace Pure.DI.Benchmarks.Containers;

using IoC;

// ReSharper disable once ClassNeverInstantiated.Global
internal sealed class IoCContainerByCompositionRoot<TContract> : BaseAbstractContainer<Func<TContract>>
{
    private readonly IoCContainer _container = new();

    public override Func<TContract> CreateContainer() => _container.CreateContainer().Resolve<Func<TContract>>();

    public override IAbstractContainer<Func<TContract>> Bind(
        Type contractType,
        Type implementationType,
        AbstractLifetime lifetime = AbstractLifetime.Transient,
        string? name = default)
    {
        _container.Bind(contractType, implementationType, lifetime, name);
        return this;
    }

    public override T Resolve<T>() where T : class => _container.Resolve<T>();

    public override void Dispose() => _container.Dispose();
}