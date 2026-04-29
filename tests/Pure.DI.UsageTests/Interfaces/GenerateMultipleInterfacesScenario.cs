/*
$v=true
$p=5
$d=Generate several interfaces from one class
$h=This example shows how one implementation can generate multiple interfaces with shared and selective members.
$f=The example shows how to:
$f=- Generate multiple interfaces from one class
$f=- Select members per interface using member attributes
$f=- Reuse one member in several generated interfaces
$r=Shouldly
*/

// ReSharper disable ClassNeverInstantiated.Global
// ReSharper disable CheckNamespace
// ReSharper disable UnusedType.Global
// ReSharper disable UnusedMember.Global

namespace Pure.DI.UsageTests.Interfaces.GenerateMultipleInterfacesScenario;

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
        DI.Setup(nameof(Composition))
            .Bind().To<Gateway>()
            .Root<App>(nameof(App));

        var composition = new Composition();
        var app = composition.App;

        app.ReadResult.ShouldBe("GET:/orders");
        app.SharedResult.ShouldBe("ok");
        // }

        composition.SaveClassDiagram();
    }
}

// {
public partial interface IReadGateway;

public partial interface IWriteGateway;

[GenerateInterface(interfaceName: nameof(IReadGateway))]
[GenerateInterface(interfaceName: nameof(IWriteGateway))]
public class Gateway : IReadGateway, IWriteGateway
{
    [GenerateInterface(interfaceName: nameof(IReadGateway))]
    public string Get(string path) => $"GET:{path}";

    [GenerateInterface(interfaceName: nameof(IWriteGateway))]
    public void Post(string path)
    {
    }

    [GenerateInterface(interfaceName: nameof(IReadGateway))]
    [GenerateInterface(interfaceName: nameof(IWriteGateway))]
    public string Ping() => "ok";
}

public class App(IReadGateway readGateway, IWriteGateway writeGateway)
{
    public string ReadResult { get; } = readGateway.Get("/orders");

    public string SharedResult { get; } = writeGateway.Ping();
}
// }
