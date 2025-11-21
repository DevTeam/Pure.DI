/*
$v=true
$p=20
$d=Roots with filter
$r=Shouldly
*/

// ReSharper disable ClassNeverInstantiated.Local
// ReSharper disable CheckNamespace
// ReSharper disable UnusedParameter.Local
// ReSharper disable ArrangeTypeModifiers

// ReSharper disable UnusedType.Global
#pragma warning disable CS9113 // Parameter is unread.
namespace Pure.DI.UsageTests.Basics.RootsWithFilterScenario;

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
        // This hint indicates to not generate methods such as Resolve
        // Resolve = Off
// {
        DI.Setup(nameof(Composition))
            .Bind().As(Lifetime.Singleton).To<Configuration>()
            .Roots<INotificationService>("My{type}", filter: "*Email*");

        var composition = new Composition();
        composition.MyEmailService.ShouldBeOfType<EmailService>();
// }
        composition.SaveClassDiagram();
    }
}

// {
interface IConfiguration;

class Configuration : IConfiguration;

interface INotificationService;

// This service requires an API key which is not bound,
// so it cannot be resolved and should be filtered out.
class SmsService(string apiKey) : INotificationService;

class EmailService(IConfiguration config) : INotificationService;
// }