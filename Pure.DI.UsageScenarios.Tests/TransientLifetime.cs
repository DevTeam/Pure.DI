// ReSharper disable ClassNeverInstantiated.Global
// ReSharper disable MemberCanBePrivate.Global
// ReSharper disable CA1816
// ReSharper disable UnusedMemberInSuper.Global
namespace Pure.DI.UsageScenarios.Tests
{
    using System;
    using System.Collections.Generic;
    using Shouldly;
    using Xunit;

    public class TransientLifetime
    {
        [Fact]
        // $visible=true
        // $tag=2 Lifetimes
        // $priority=01
        // $description=Transient lifetime
        // {
        public void Run()
        {
            DI.Setup()
                .Bind<IDependency>().To<Dependency>()
                .Bind<IService>().To<Service>();

            // Track disposables to dispose of instances manually
            var disposables = new List<IDisposable>();
            TransientLifetimeDI.OnDisposable += e =>
            {
                if (e.Lifetime == Lifetime.Transient) disposables.Add(e.Disposable);
            };

            var instance = TransientLifetimeDI.Resolve<IService>();

            // Check that dependencies are not equal
            instance.Dependency1.ShouldNotBe(instance.Dependency2);
            
            // Check the number of transient disposable instances
            disposables.Count.ShouldBe(2);
            
            // Dispose instances
            disposables.ForEach(disposable => disposable.Dispose());
            disposables.Clear();
        }

        public interface IDependency { }

        public class Dependency : IDependency, IDisposable
        {
            public void Dispose() { }
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
