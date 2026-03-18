// ReSharper disable once CheckNamespace
// ReSharper disable UnusedVariable
// ReSharper disable UnusedMember.Global
// ReSharper disable UnusedMember.Local
// ReSharper disable UnusedAutoPropertyAccessor.Global
namespace Pure.DI.UsageTests.IntegrationTests.ExposedRootTests;

using Pure.DI;

public class ExposedRootTests
{
    [Fact]
    public void Run()
    {
        var composition = new Composition();
    }
}

public class MyDependency;

public class MyService(MyDependency dependency) : IMyService
{
    public MyDependency Dependency { get; } = dependency;

    public void DoSomething()
    {
    }
}

public interface IMyService
{
    void DoSomething();
}

// ReSharper disable once ClassNeverInstantiated.Global
public partial class CompositionInOtherProject
{
    private static void Setup() =>
        DI.Setup()
            .Bind().As(Lifetime.Singleton).To<MyDependency>()
            .Bind().To<MyService>()
            .Root<IMyService>("MyService", kind: RootKinds.Exposed);
}

public partial class Composition
{
    private static void Setup() =>
        DI.Setup()
            .Bind().As(Lifetime.Singleton).To<CompositionInOtherProject>()
            .Root<Root>("Root");
}


public class Root
{
    public IMyService Service { get; }

    public Root(IMyService service)
    {
        Service = service;
    }
}
