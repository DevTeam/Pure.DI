/*
$v=true
$p=9
$d=Generate an interface from a class
$h=This example shows how a public class can generate a matching interface and be used through Pure.DI.
$f=The example shows how to:
$f=- Generate an interface from a class
$f=- Bind the generated contract in Pure.DI
$f=- Resolve the consumer from a composition root
$r=Shouldly
*/

// ReSharper disable ClassNeverInstantiated.Global
// ReSharper disable CheckNamespace
// ReSharper disable UnusedType.Global
// ReSharper disable UnusedMember.Global

namespace Pure.DI.UsageTests.Interface.GenerateInterfaceScenario;

using Pure.DI.UsageTests;
using Pure.DI;
using Shouldly;
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
            .Bind<IService>().To<Service>()
            .Root<App>(nameof(App));

        var composition = new Composition();
        var app = composition.App;

        app.Message.ShouldBe("ok");
        app.Text.ShouldBe("pong");
        // }

        composition.SaveClassDiagram();
    }
}

// {
public partial interface IService;

[GenerateInterface]
public partial class Service : IService
{
    public string Message => "ok";

    public string Ping() => "pong";
}

public class App(IService service)
{
    public string Message { get; } = service.Message;

    public string Text { get; } = service.Ping();
}
// }
