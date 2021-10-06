// ReSharper disable ClassNeverInstantiated.Global
// ReSharper disable MemberCanBePrivate.Global
// ReSharper disable UnusedMember.Global
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
                .Bind<IFactory>().As(Singleton).To<CustomSingletonLifetime>()
                .Bind<IDependency>().To<DependencySingleton>()
                .Bind<IService>().To<Service>();
            
            var instance1 = CustomSingletonDI.Resolve<IService>();
            var instance2 = CustomSingletonDI.Resolve<IService>();

            // Check that dependencies are singletons
            instance1.Dependency.ShouldBe(instance2.Dependency);
            instance1.ShouldNotBe(instance2);
        }

        // A pattern of the class name ending by word "Singleton"
        [Include(".*Singleton$")]
        public class CustomSingletonLifetime: IFactory
        {
            // Stores singleton instances by key
            private readonly ConcurrentDictionary<Key, object> _instances = new();

            // Gets an existing instance or creates a new
            public T Create<T>(Func<T> factory, Type implementationType, object tag) =>
                (T)_instances.GetOrAdd(new Key(implementationType, tag), i => factory()!);

            // Represents an instance key
            private record Key(Type Type, object? Tag);
        }
        
        public interface IDependency { }

        public class DependencySingleton : IDependency { }

        public interface IService { IDependency Dependency { get; } }

        public class Service : IService
        {
            public Service(IDependency dependency) { Dependency = dependency; }

            public IDependency Dependency { get; }
        }
        // }
    }
}