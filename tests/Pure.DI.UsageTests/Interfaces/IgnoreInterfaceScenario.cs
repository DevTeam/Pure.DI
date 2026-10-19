/*
$v=true
$p=2
$d=Ignore members in the generated interface
$h=This example shows how to exclude internal-only members from a generated interface. `[IgnoreInterface]` on `GetAccessToken` keeps the token lookup inside the class while the endpoint stays part of the contract consumers depend on.
$f=The example shows how to:
$f=- Mark members with IgnoreInterface
$f=- Keep only the intended contract surface
$f=- Use the generated interface in Pure.DI
$f=
$f=This is how a client exposes what callers may do and hides how it authenticates: the contract stays small, the implementation keeps its helpers, and no consumer can call an operation that was meant to stay internal.
$f=The exclusion is driven by an attribute, not by accessibility, so a public method can be absent from the contract while still being callable on the concrete type. That is intended for a class consumed through the interface, not for one used directly in many places.
$f=>[!WARNING]
$f=>A member marked `[IgnoreInterface]` silently disappears from the contract that an existing consumer may already use. Removing the attribute is a source-breaking change for such consumers, so treat it as an API change.
$f=
$f=Prefer this when only one or two members must stay private to the implementation; use [Control generated interfaces by members](control-generated-interfaces-by-members.md) when the inclusion is the exception and exclusion the rule.
$r=Shouldly
*/

// ReSharper disable ClassNeverInstantiated.Global
// ReSharper disable CheckNamespace
// ReSharper disable UnusedType.Global
// ReSharper disable UnusedMember.Global

#pragma warning disable CA1822
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
