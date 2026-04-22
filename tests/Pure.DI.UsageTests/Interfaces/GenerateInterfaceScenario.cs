/*
$v=true
$p=9
$d=Generate an interface from a class
$h=This example shows how a concrete service can generate a matching interface and be consumed through Pure.DI.
$f=The example shows how to:
$f=- Generate an interface from a class
$f=- Bind the generated contract in Pure.DI
$f=- Resolve a consumer that depends on the interface
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
