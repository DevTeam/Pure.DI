/*
$v=true
$p=9
$d=Ignore members in generated interface
$h=This example shows how to exclude selected members from the generated interface.
$f=The example shows how to:
$f=- Mark members with IgnoreInterface
$f=- Keep only the public contract surface
$f=- Use the generated interface in Pure.DI
$r=Shouldly
*/

// ReSharper disable ClassNeverInstantiated.Global
// ReSharper disable CheckNamespace
// ReSharper disable UnusedType.Global
// ReSharper disable UnusedMember.Global

namespace Pure.DI.UsageTests.Interface.IgnoreInterfaceScenario;

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
            .Bind().To<Service>()
            .Root<App>(nameof(App));

        var composition = new Composition();
        var app = composition.App;

        app.Name.ShouldBe("visible");
        // }

        composition.SaveClassDiagram();
    }
}

// {
public partial interface IService;

[GenerateInterface]
public class Service : IService
{
    public string Name => "visible";

    [IgnoreInterface]
    public string Secret() => "hidden";
}

public class App(IService service)
{
    public string Name { get; } = service.Name;
}
// }
