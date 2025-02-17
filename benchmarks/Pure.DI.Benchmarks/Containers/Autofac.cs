// ReSharper disable UnusedType.Global
namespace Pure.DI.Benchmarks.Containers;

using global::Autofac;

// ReSharper disable once ClassNeverInstantiated.Global
sealed class Autofac : BaseAbstractContainer<IContainer>
{

    public Autofac() => _container = new Lazy<IContainer>(() => _builder.Build());
    private readonly ContainerBuilder _builder = new();
    private readonly Lazy<IContainer> _container;

    public override IContainer CreateContainer() => _container.Value;

    public override IAbstractContainer<IContainer> Bind(
        Type contractType,
        Type implementationType,
        AbstractLifetime lifetime = AbstractLifetime.Transient,
        string? name = null)
    {
        var registration = _builder.RegisterType(implementationType).As(contractType);
        switch (lifetime)
        {
            case AbstractLifetime.Transient:
                break;

            case AbstractLifetime.Singleton:
                registration.SingleInstance();
                break;

            default:
                throw new ArgumentOutOfRangeException(nameof(lifetime), lifetime, null);
        }

        if (name != null)
        {
            registration.Keyed<object>(name);
        }

        return this;
    }

    public override T Resolve<T>() where T : class => _container.Value.Resolve<T>();

    public override void Dispose() => _container.Value.Dispose();
}