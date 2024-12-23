/*
$v=true
$p=102
$d=Tracking async disposable instances per a composition root
*/

// ReSharper disable CheckNamespace
// ReSharper disable UnusedMember.Local
// ReSharper disable UnusedParameterInPartialMethod
// ReSharper disable ArrangeTypeModifiers
// ReSharper disable ArrangeTypeMemberModifiers

namespace Pure.DI.UsageTests.Advanced.TrackingAsyncDisposableScenario;

using Xunit;

// {
//# using Pure.DI;
//# using Shouldly;
// }

public class Scenario
{
    [Fact]
    public async Task Run()
    {
// {
        var composition = new Composition();
        var root1 = composition.Root;
        var root2 = composition.Root;

        await root2.DisposeAsync();

        // Checks that the disposable instances
        // associated with root1 have been disposed of
        root2.Value.Dependency.IsDisposed.ShouldBeTrue();

        // Checks that the disposable instances
        // associated with root2 have not been disposed of
        root1.Value.Dependency.IsDisposed.ShouldBeFalse();

        await root1.DisposeAsync();

        // Checks that the disposable instances
        // associated with root2 have been disposed of
        root1.Value.Dependency.IsDisposed.ShouldBeTrue();
// }
        new Composition().SaveClassDiagram();
    }
}

// {
interface IDependency
{
    bool IsDisposed { get; }
}

class Dependency : IDependency, IAsyncDisposable
{
    public bool IsDisposed { get; private set; }

    public ValueTask DisposeAsync()
    {
        IsDisposed = true;
        return ValueTask.CompletedTask;
    }
}

interface IService
{
    public IDependency Dependency { get; }
}

class Service(IDependency dependency) : IService
{
    public IDependency Dependency { get; } = dependency;
}

partial class Composition
{
    static void Setup() =>
        DI.Setup()
            .Bind().To<Dependency>()
            .Bind().To<Service>()

            // A special composition root
            // that allows to manage disposable dependencies
            .Root<Owned<IService>>("Root");
}
// }