// ReSharper disable ClassNeverInstantiated.Global
// ReSharper disable MemberCanBePrivate.Global
// ReSharper disable CA1816
// ReSharper disable UnusedMemberInSuper.Global
namespace Pure.DI.UsageScenarios.Tests;

using System.Collections.Generic;
using static Lifetime;

public class PerResolveLifetimeViaType
{
    [Fact]
    // $visible=false
    // $tag=2 Lifetimes
    // $priority=01
    // $description=Per resolve lifetime
    // {
    public void Run()
    {
        DI.Setup()
            .Bind<IDependency>().As(PerResolve).To<Dependency>()
            .Bind<IService>().To<Service>();

        // Track disposables
        var disposables = new List<IDisposable>();
        PerResolveLifetimeViaTypeDI.OnDisposable += e => disposables.Add(e.Disposable);

        var instance = (IService)PerResolveLifetimeViaTypeDI.Resolve(typeof(IService));

        // Check that dependencies are equal
        instance.Dependency1.ShouldBe(instance.Dependency2);

        // Check disposable instances created
        disposables.Count.ShouldBe(1);
    }

    public interface IDependency
    {
    }

    public class Dependency : IDependency, IDisposable
    {
        public void Dispose() => GC.SuppressFinalize(this);
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