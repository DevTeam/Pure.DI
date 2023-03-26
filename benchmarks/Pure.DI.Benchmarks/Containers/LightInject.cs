namespace Pure.DI.Benchmarks.Containers;

using global::LightInject;

// ReSharper disable once ClassNeverInstantiated.Global
internal sealed class LightInject : BaseAbstractContainer<ServiceContainer>
{
    private readonly ServiceContainer _container = new();

    public override ServiceContainer CreateContainer() => _container;

    public override void Register(Type contractType, Type implementationType, AbstractLifetime lifetime = AbstractLifetime.Transient, string? name = default)
    {
        switch (lifetime)
        {
            case AbstractLifetime.Transient:
                if (string.IsNullOrEmpty(name))
                {
                    _container.Register(contractType, implementationType);
                }
                else
                {
                    _container.Register(contractType, implementationType, name);
                }

                break;

            case AbstractLifetime.Singleton:
                if (string.IsNullOrEmpty(name))
                {
                    _container.Register(contractType, implementationType, new PerContainerLifetime());
                }
                else
                {
                    _container.Register(contractType, implementationType, name, new PerContainerLifetime());
                }

                break;

            default:
                throw new ArgumentOutOfRangeException(nameof(lifetime), lifetime, null);
        }
    }

    public override T Resolve<T>() where T : class => _container.GetInstance<T>();

    public override void Dispose() => _container.Dispose();
}