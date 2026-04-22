/*
$v=true
$p=9
$d=Ignore members in the generated interface
$h=This example shows how to exclude internal-only members from a generated interface.
$f=The example shows how to:
$f=- Mark members with IgnoreInterface
$f=- Keep only the intended contract surface
$f=- Use the generated interface in Pure.DI
$r=Shouldly
*/

// ReSharper disable ClassNeverInstantiated.Global
// ReSharper disable CheckNamespace
// ReSharper disable UnusedType.Global
// ReSharper disable UnusedMember.Global

namespace Pure.DI.UsageTests.Interfaces.IgnoreInterfaceScenario;

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
            .Bind().To<ApiClient>()
            .Root<App>(nameof(App));

        var composition = new Composition();
        var app = composition.App;

        app.Endpoint.ShouldBe("https://api.contoso.com");
        // }

        composition.SaveClassDiagram();
    }
}

// {
public partial interface IApiClient;

[GenerateInterface]
public class ApiClient : IApiClient
{
    public string Endpoint => "https://api.contoso.com";

    [IgnoreInterface]
    public string GetAccessToken() => "internal-token";
}

public class App(IApiClient client)
{
    public string Endpoint { get; } = client.Endpoint;
}
// }
