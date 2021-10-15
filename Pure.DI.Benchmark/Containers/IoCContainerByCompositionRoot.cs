namespace Pure.DI.Benchmark.Containers
{
    using System;
    using IoC;

    // ReSharper disable once ClassNeverInstantiated.Global
    internal sealed class IoCContainerByCompositionRoot<TContract> : BaseAbstractContainer<Func<TContract>>
    {
        private readonly IoCContainer _container = new();

        public override Func<TContract> CreateContainer() => _container.CreateContainer().Resolve<Func<TContract>>();

        public override void Register(Type contractType, Type implementationType, AbstractLifetime lifetime = AbstractLifetime.Transient, string name = null) =>
            _container.Register(contractType, implementationType, lifetime, name);

        public override T Resolve<T>() where T : class => _container.Resolve<T>();

        public override void Dispose() => _container.Dispose();
    }
}