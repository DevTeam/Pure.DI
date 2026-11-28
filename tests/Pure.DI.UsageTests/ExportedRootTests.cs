// ReSharper disable once CheckNamespace
// ReSharper disable UnusedVariable
// ReSharper disable UnusedMember.Global
// ReSharper disable UnusedMember.Local
// ReSharper disable UnusedAutoPropertyAccessor.Global
// ReSharper disable once ConvertToPrimaryConstructor
// The root type is a plain documented class, so its constructor stays explicit next to the members
// that the generator fills in; the scenario is embedded into the documentation as is.

namespace Pure.DI.UsageTests.IntegrationTests.ExportedRootTests;

using Pure.DI;

public class ExportedRootTests
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
            .Root<IMyService>("MyService", kind: RootKinds.Exported);
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

    // ReSharper disable once ConvertToPrimaryConstructor
    // The root is a plain documented class, so the constructor stays explicit next to the members.
    public Root(IMyService service)
    {
        Service = service;
    }
}
