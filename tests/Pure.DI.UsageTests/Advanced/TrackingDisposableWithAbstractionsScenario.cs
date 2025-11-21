/*
$v=true
$p=101
$d=Tracking disposable instances using pre-built classes
$h=If you want ready-made classes for tracking disposable objects in your libraries but don't want to create your own, you can add this package to your projects:
$h=
$h=[![NuGet](https://img.shields.io/nuget/v/Pure.DI.Abstractions)](https://www.nuget.org/packages/Pure.DI.Abstractions)
$h=
$h=It contains attributes like `Inject` and `Inject<T>` that work for constructors and their arguments, methods and their arguments, properties and fields. They allow you to setup all injection parameters.
$f=This package should also be included in a project:
$f=
$f=[![NuGet](https://img.shields.io/nuget/v/Pure.DI)](https://www.nuget.org/packages/Pure.DI)
$r=Shouldly;Pure.DI.Abstractions
*/

// ReSharper disable CheckNamespace
// ReSharper disable UnusedMember.Local
// ReSharper disable UnusedParameterInPartialMethod
// ReSharper disable ArrangeTypeModifiers
// ReSharper disable ArrangeTypeMemberModifiers
// ReSharper disable UnusedMemberInSuper.Global

namespace Pure.DI.UsageTests.Advanced.TrackingDisposableWithAbstractionsScenario;

using Abstractions;
using Xunit;

// {
//# using Pure.DI;
// }

public class Scenario
{
    [Fact]
    public void Run()
    {
// {
        var composition = new Composition();
        var dataService1 = composition.DataService;
        var dataService2 = composition.DataService;

        // The dedicated connection should be unique for each root
        dataService1.Connection.ShouldNotBe(dataService2.Connection);

        // The shared connection should be the same instance
        dataService1.SharedConnection.ShouldBe(dataService2.SharedConnection);

        dataService2.Dispose();

        // Checks that the disposable instances
        // associated with dataService2 have been disposed of
        dataService2.Connection.IsDisposed.ShouldBeTrue();

        // But the singleton is still not disposed of
        // because it is shared and tracked by the composition
        dataService2.SharedConnection.IsDisposed.ShouldBeFalse();

        // Checks that the disposable instances
        // associated with dataService1 have not been disposed of
        dataService1.Connection.IsDisposed.ShouldBeFalse();
        dataService1.SharedConnection.IsDisposed.ShouldBeFalse();

        dataService1.Dispose();

        // Checks that the disposable instances
        // associated with dataService1 have been disposed of
        dataService1.Connection.IsDisposed.ShouldBeTrue();

        // But the singleton is still not disposed of
        dataService1.SharedConnection.IsDisposed.ShouldBeFalse();

        composition.Dispose();

        // The shared singleton is disposed only when the composition is disposed
        dataService1.SharedConnection.IsDisposed.ShouldBeTrue();
// }
        new Composition().SaveClassDiagram();
    }
}

// {
interface IDbConnection
{
    bool IsDisposed { get; }
}

class DbConnection : IDbConnection, IDisposable
{
    public bool IsDisposed { get; private set; }

    public void Dispose() => IsDisposed = true;
}

interface IDataService
{
    public IDbConnection Connection { get; }

    public IDbConnection SharedConnection { get; }
}

class DataService(
    Func<Own<IDbConnection>> connectionFactory,
    [Tag("shared")] Func<Own<IDbConnection>> sharedConnectionFactory)
    : IDataService, IDisposable
{
    // Own<T> is a wrapper from Pure.DI.Abstractions that owns the value.
    // It ensures that the value is disposed when Own<T> is disposed,
    // but only if the value is not a singleton or externally owned.
    private readonly Own<IDbConnection> _connection = connectionFactory();
    private readonly Own<IDbConnection> _sharedConnection = sharedConnectionFactory();

    public IDbConnection Connection => _connection.Value;

    public IDbConnection SharedConnection => _sharedConnection.Value;

    public void Dispose()
    {
        // Disposes the dedicated connection
        _connection.Dispose();

        // Notifies that we are done with the shared connection.
        // However, since it's a singleton, the underlying instance won't be disposed here.
        _sharedConnection.Dispose();
    }
}

partial class Composition
{
    static void Setup() =>
        // }
        // This hint indicates to not generate methods such as Resolve
        // Resolve = Off
        // {
        DI.Setup()
            .Bind().To<DbConnection>()
            .Bind("shared").As(Lifetime.Singleton).To<DbConnection>()
            .Bind().To<DataService>()

            // Composition root
            .Root<DataService>("DataService");
}
// }