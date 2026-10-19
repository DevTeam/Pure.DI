/*
$v=true
$p=5
$d=Generate several interfaces from one class
$h=This example shows how one implementation can generate multiple interfaces with shared and selective members. The class declares `[GenerateInterface]` twice, each member names the interface it belongs to, and members listed for both end up in both contracts.
$f=The example shows how to:
$f=- Generate multiple interfaces from one class
$f=- Select members per interface using member attributes
$f=- Reuse one member in several generated interfaces
$f=
$f=A gateway is the usual case: one class talks to a service, while consumers get a read contract and a write contract and the composition decides who receives which. The same instance can be injected for both if the lifetimes allow it.
$f=Each interface is generated separately, so the members must be attributed explicitly - the class-level attribute names the interfaces but does not add members to them.
$f=>[!NOTE]
$f=>Both interfaces are bound to the same implementation, so two consumers that depend on different contracts still share one object when its lifetime is not transient. Give it `Singleton` or `Scoped` when the state must be shared.
$f=
$f=Prefer this when the split is by capability and the implementation is genuinely one unit. When the two sides have different dependencies or lifetimes, split the classes instead.
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
