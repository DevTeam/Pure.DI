// ReSharper disable UnusedType.Global
namespace Pure.DI.Benchmarks.Containers;

using Castle.MicroKernel.Registration;
using Castle.Windsor;

// ReSharper disable once ClassNeverInstantiated.Global
sealed class CastleWindsor : BaseAbstractContainer<WindsorContainer>
{
    private readonly WindsorContainer _container = new();

    public override WindsorContainer CreateContainer() => _container;

    public override IAbstractContainer<WindsorContainer> Bind(
        Type contractType,
        Type implementationType,
        AbstractLifetime lifetime = AbstractLifetime.Transient,
        string? name = null)
    {
        var registration = Component.For(contractType).ImplementedBy(implementationType);
        switch (lifetime)
        {
            case AbstractLifetime.Transient:
                registration.LifestyleTransient();
                break;

            case AbstractLifetime.Singleton:
                registration.LifestyleSingleton();
                break;

            default:
                throw new ArgumentOutOfRangeException(nameof(lifetime), lifetime, null);
        }

        if (name != null)
        {
            registration.Named(name);
        }

        _container.Register(registration);
        return this;
    }

    public override T Resolve<T>() where T : class => _container.Resolve<T>();

    public override void Dispose() => _container.Dispose();
}