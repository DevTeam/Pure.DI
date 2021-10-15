namespace Pure.DI.Benchmark.Containers
{
    using System;
    using global::DryIoc;

    internal sealed class DryIoc: BaseAbstractContainer<Container>
    {
        private readonly Container _container = new();

        public override Container CreateContainer() => _container;

        public override void Register(Type contractType, Type implementationType, AbstractLifetime lifetime = AbstractLifetime.Transient, string name = null)
        {
            var reuse = lifetime switch
            {
                AbstractLifetime.Transient => Reuse.Transient,
                AbstractLifetime.Singleton => Reuse.Singleton,
                _ => throw new ArgumentOutOfRangeException(nameof(lifetime), lifetime, null)
            };

            _container.Register(new ReflectionFactory(implementationType, reuse), contractType, name, null, true);
        }

        public override T Resolve<T>() where T : class => _container.Resolve<T>();

        public override void Dispose() => _container.Dispose();
    }
}