﻿namespace Pure.DI.Benchmarks.Containers;

using IoC;
using IoC.Features;
using IoC.Lifetimes;

// ReSharper disable once ClassNeverInstantiated.Global
sealed class IoCContainer : BaseAbstractContainer<Container>
{
    private readonly Container _container = Container.Create(CollectionFeature.Set, FuncFeature.Set);

    public override Container CreateContainer() => _container;

    public override IAbstractContainer<Container> Bind(
        Type contractType,
        Type implementationType,
        AbstractLifetime lifetime = AbstractLifetime.Transient,
        string? name = null)
    {
        var bind = _container.Bind(contractType);
        switch (lifetime)
        {
            case AbstractLifetime.Transient:
                break;

            case AbstractLifetime.Singleton:
                bind = bind.Lifetime(new SingletonLifetime(false));
                break;

            default:
                throw new ArgumentOutOfRangeException(nameof(lifetime), lifetime, null);
        }

        if (name != null)
        {
            bind = bind.Tag(name);
        }

        bind.To(implementationType);
        return this;
    }

    public override T Resolve<T>() where T : class => _container.Resolve<T>();

    public override void Dispose() => _container.Dispose();
}