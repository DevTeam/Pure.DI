namespace Pure.DI.Benchmark.Containers
{
    using System;
    using global::Unity;
    using global::Unity.Lifetime;

    // ReSharper disable once ClassNeverInstantiated.Global
    internal sealed class Unity: BaseAbstractContainer<UnityContainer>
    {
        private readonly UnityContainer _container = new();

        public override UnityContainer CreateContainer() => _container;

        public override void Register(Type contractType, Type implementationType, AbstractLifetime lifetime = AbstractLifetime.Transient, string name = null)
        {
            ITypeLifetimeManager lifetimeManager = null;
            switch (lifetime)
            {
                case AbstractLifetime.Transient:
                    break;

                case AbstractLifetime.Singleton:
                    lifetimeManager = new ContainerControlledLifetimeManager();
                    break;

                default:
                    throw new ArgumentOutOfRangeException(nameof(lifetime), lifetime, null);
            }

           ((IUnityContainer)_container).RegisterType(contractType, implementationType, name, lifetimeManager);
        }

        public override T Resolve<T>() where T : class => _container.Resolve<T>();
            
        public override void Dispose() => _container.Dispose();
    }
}