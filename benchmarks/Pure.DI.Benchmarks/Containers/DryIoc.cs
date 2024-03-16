// ReSharper disable UnusedType.Global
namespace Pure.DI.Benchmarks.Containers;

using global::DryIoc;

internal sealed class DryIoc : BaseAbstractContainer<Container>
{
    private readonly Container _container = new();

    public override Container CreateContainer() => _container;

    public override IAbstractContainer<Container> Bind(
        Type contractType,
        Type implementationType,
        AbstractLifetime lifetime = AbstractLifetime.Transient,
        string? name = default)
    {
        var reuse = lifetime switch
        {
            AbstractLifetime.Transient => Reuse.Transient,
            AbstractLifetime.Singleton => Reuse.Singleton,
            _ => throw new ArgumentOutOfRangeException(nameof(lifetime), lifetime, null)
        };

        _container.Register(ReflectionFactory.Of(implementationType, reuse), contractType, name, null, true);
        return this;
    }

    public override T Resolve<T>() where T : class => _container.Resolve<T>();

    public override void Dispose() => _container.Dispose();
}