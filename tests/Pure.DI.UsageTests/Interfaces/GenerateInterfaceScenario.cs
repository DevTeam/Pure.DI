/*
$v=true
$p=1
$d=Generate an interface from a class
$h=This example shows how a concrete service can generate a matching interface and be consumed through Pure.DI. Consumers depend on the generated contract while the class keeps its implementation, so the rest of the graph never sees the concrete type.
$h=Use it when a class is the only implementation and you still want the graph to depend on an abstraction - a seam for tests and for a later replacement, without writing the interface by hand.
$f=The example shows how to:
$f=- Generate an interface from a class
$f=- Bind the generated contract in Pure.DI
$f=- Resolve a consumer that depends on the interface
$f=
$f=Declare the interface as `partial` and annotate the class with `[GenerateInterface]`: the generator fills the interface with the public members of the class and adds the interface to its base list. The binding `.Bind().To<EmailSender>()` then serves `IEmailSender` without an extra registration.
$f=The interface is regenerated from the class, so a renamed or removed public member changes the contract on the next build. That is the point - the contract cannot drift - but it also means the interface is a poor boundary for an assembly that others compile against.
$f=>[!NOTE]
$f=>The generated interface lives next to the class in the same assembly and namespace unless you move it. Prefer [Customize the generated interface](customize-the-generated-interface.md) when the contract must sit in a separate namespace.
$r=Shouldly
*/

// ReSharper disable ClassNeverInstantiated.Global
// ReSharper disable CheckNamespace
// ReSharper disable UnusedType.Global
// ReSharper disable UnusedMember.Global

namespace Pure.DI.UsageTests.Interfaces.GenerateInterfaceScenario;

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
            .Bind().To<EmailSender>()
            .Root<App>(nameof(App));

        var composition = new Composition();
        var app = composition.App;

        app.Provider.ShouldBe("smtp");
        app.Result.ShouldBe("sent:ops@contoso.com");
        // }

        composition.SaveClassDiagram();
    }
}

// {
public partial interface IEmailSender;

[GenerateInterface]
public class EmailSender : IEmailSender
{
    public string Provider => "smtp";

    public string Send(string address) => $"sent:{address}";
}

public class App(IEmailSender sender)
{
    public string Provider { get; } = sender.Provider;

    public string Result { get; } = sender.Send("ops@contoso.com");
}
// }
