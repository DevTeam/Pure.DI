// ReSharper disable ClassNeverInstantiated.Global
// ReSharper disable MemberCanBePrivate.Global
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

            // Track disposables
            var disposables = new List<IDisposable>();
            TransientLifetimeDI.OnDisposable += e => disposables.Add(e.Disposable);

            var instance = TransientLifetimeDI.Resolve<IService>();

            // Check that dependencies are not equal
            instance.Dependency1.ShouldNotBe(instance.Dependency2);
            
            // Check disposable instances created
            disposables.Count.ShouldBe(2);
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
