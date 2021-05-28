// ReSharper disable ClassNeverInstantiated.Global
// ReSharper disable MemberCanBePrivate.Global
namespace Pure.DI.UsageScenarios.Tests
{
    using System;
    using Shouldly;
    using Xunit;
    using static Lifetime;

    public class SingletonLifetime
    {
        [Fact]
        
        // $visible=true
        // $tag=2 Lifetimes
        // $priority=01
        // $description=Singleton lifetime
        // $header=[Singleton](https://en.wikipedia.org/wiki/Singleton_pattern) is a design pattern that supposes for having only one instance of some class during the whole application lifetime. The main complaint about Singleton is that it contradicts the Dependency Injection principle and thus hinders testability. It essentially acts as a global constant, and it is hard to substitute it with a test when needed. The _Singleton lifetime_ is indispensable in this case.
        // {
        public void Run()
        {
            DI.Setup()
                .Bind<IDependency>().As(Singleton).To<Dependency>()
                // Use the Singleton lifetime
                .Bind<IService>().To<Service>();
            
            // Resolve the singleton twice
            var instance = SingletonLifetimeDI.Resolve<IService>();

            // Check that instances are equal
            instance.Dependency1.ShouldBe(instance.Dependency2);
            
            SingletonLifetimeDI.FinalDispose();
            instance.Dependency1.IsDisposed.ShouldBeTrue();
        }
        
        public interface IDependency : IDisposable
        {
            bool IsDisposed { get; }
        }

        public class Dependency : IDependency
        {
            public bool IsDisposed { get; private set; }
            
            public void Dispose() => IsDisposed = true;
        }

        public interface IService
        {
            IDependency Dependency1 { get; }
            
            IDependency Dependency2 { get; }
        }

        public class Service : IService
        {
            public Service(IDependency dependency1, IDependency dependency2)
            {
                Dependency1 = dependency1;
                Dependency2 = dependency2;
            }

            public IDependency Dependency1 { get; }
            
            public IDependency Dependency2 { get; }
        }
        // }
    }
}
