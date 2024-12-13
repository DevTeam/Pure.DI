// ReSharper disable UnusedType.Global
namespace Pure.DI.Benchmarks.Containers;

using global::Ninject;

// ReSharper disable once ClassNeverInstantiated.Global
internal sealed class Ninject : BaseAbstractContainer<StandardKernel>
{
    private readonly StandardKernel _container = new();

    public override StandardKernel CreateContainer() => _container;

    public override IAbstractContainer<StandardKernel> Bind(
        Type contractType,
        Type implementationType,
        AbstractLifetime lifetime = AbstractLifetime.Transient,
        string? name = null)
    {
        var bind = _container.Bind(contractType).To(implementationType);
        switch (lifetime)
        {
            case AbstractLifetime.Transient:
                break;

            case AbstractLifetime.Singleton:
                bind.InSingletonScope();
                break;

            default:
                throw new ArgumentOutOfRangeException(nameof(lifetime), lifetime, null);
        }

        if (name != null)
        {
            bind.Named(name);
        }

        return this;
    }

    public override T Resolve<T>() where T : class => _container.Get<T>();

    public override void Dispose() => _container.Dispose();
}