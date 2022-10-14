// ReSharper disable ClassNeverInstantiated.Global
// ReSharper disable MemberCanBePrivate.Global
// ReSharper disable UnusedMember.Global
// ReSharper disable ArrangeNamespaceBody
namespace Pure.DI.UsageScenarios.Tests
{
    using Shouldly;
    using System;
    using System.Collections.Concurrent;
    using Xunit;
    using static Lifetime;

    public class CustomSingleton
    {
        [Fact]
        // $visible=true
        // $tag=2 Lifetimes
        // $priority=99
        // $description=Custom singleton lifetime
        // $header=*__IFactory__* is a powerful tool that allows controlling most the aspects while resolving dependencies.
        // {
        public void Run()
        {
            DI.Setup()
                // Registers the custom lifetime for all implementations with a class name ending by word "Singleton"
                .Bind<IFactory>().Bind<ISingletonsContainer>().As(Singleton).To<CustomSingletonLifetime>()
                .Bind<IDependency>().To<DependencySingleton>()
                .Bind<IService>().To<Service>();

            IService instance1;
            using (CustomSingletonDI.Resolve<ISingletonsContainer>())
            {
                instance1 = CustomSingletonDI.Resolve<IService>();
                var instance2 = CustomSingletonDI.Resolve<IService>();

                // Check that dependencies are singletons
                instance1.Dependency.ShouldBe(instance2.Dependency);
                instance1.ShouldNotBe(instance2);
            }

            instance1.Dependency.ShouldBeOfType<DependencySingleton>();
            ((DependencySingleton)instance1.Dependency).IsDisposed.ShouldBeTrue();
        }
        
        public interface ISingletonsContainer: IDisposable { }

        // A pattern of the class name ending by word "Singleton"
        [Include(".*Singleton$")]
        internal class CustomSingletonLifetime: IFactory, ISingletonsContainer
        {
            // Stores singleton instances by key
            private readonly ConcurrentDictionary<Key, object> _instances = new();

            // Gets an existing instance or creates a new
            public T Create<T>(Func<T> factory, Type implementationType, object tag, Lifetime lifetime) =>
                (T)_instances.GetOrAdd(new Key(implementationType, tag), _ => factory()!);

            public void Dispose()
            {
                foreach (var disposable in _instances.Values.OfType<IDisposable>())
                {
                    disposable.Dispose();
                }
                
                _instances.Clear();
            }

            // Represents an instance key
            [SuppressMessage("ReSharper", "NotAccessedPositionalProperty.Local")]
            private record Key(Type Type, object? Tag);
        }
        
        public interface IDependency { }

        public class DependencySingleton : IDependency, IDisposable
        {
            public bool IsDisposed { get; private set; }

            public void Dispose()
            {
                IsDisposed = true;
            }
        }

        public interface IService { IDependency Dependency { get; } }

        public class Service : IService
        {
            public Service(IDependency dependency) { Dependency = dependency; }

            public IDependency Dependency { get; }
        }
        // }
    }
}