// ReSharper disable ClassNeverInstantiated.Global
// ReSharper disable MemberCanBePrivate.Global
// ReSharper disable UnusedMember.Global
// ReSharper disable ArrangeNamespaceBody
#pragma warning disable CA1816
namespace Pure.DI.UsageScenarios.Tests
{
    using Shouldly;
    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using Xunit;
    using static Lifetime;

    public class CustomContainer
    {
        [Fact]
        // $visible=true
        // $tag=2 Lifetimes
        // $priority=99
        // $description=Custom Container
        // $header=*__Factory__* is a powerful tool that allows you to control most aspects of dependency resolution, including the implementation of classic container-style dependency injection.
        // {
        public void Run()
        {
            DI.Setup()
                // Specifies that we should pass in a container instance each time we call the `Resolve()` method.
                .Arg<IFactory>()
                .Bind<IDependency>().To<Dependency>()
                .Bind<IService>().To<Service>();

            IService instance1;
            // Creates a container and passes in a list of types that must be singletons.
            using (var container = new Container(typeof(IDependency)))
            {
                instance1 = CustomContainerDI.Resolve<IService>(container);
                var instance2 = CustomContainerDI.Resolve<IService>(container);

                // Check that dependencies are singletons
                instance1.Dependency.ShouldBe(instance2.Dependency);
                instance1.ShouldNotBe(instance2);
            }
            
            instance1.Dependency.ShouldBeOfType<Dependency>();
            // Checks that container disposed disposable instances.
            ((Dependency)instance1.Dependency).IsDisposed.ShouldBeTrue();
        }
        
        internal class Container: IFactory, IDisposable
        {
            private readonly HashSet<Type> _singletonTypes;

            // Stores singleton instances by key
            private readonly ConcurrentDictionary<Key, object> _instances = new();

            public Container(params Type[] singletonTypes) => 
                _singletonTypes = new HashSet<Type>(singletonTypes);

            // Gets an existing instance or creates a new
            public T Create<T>(Func<T> factory, Type implementationType, object tag, Lifetime lifetime) =>
                _singletonTypes.Contains(typeof(T))
                    // Resolves a singleton manually
                    ? (T)_instances.GetOrAdd(new Key(implementationType, tag), _ => factory()!)
                    // Resolves the instance using the default way
                    : factory();

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

        public class Dependency : IDependency, IDisposable
        {
            public bool IsDisposed { get; private set; }
            
            public void Dispose() => IsDisposed = true;
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
#pragma warning restore CA1816