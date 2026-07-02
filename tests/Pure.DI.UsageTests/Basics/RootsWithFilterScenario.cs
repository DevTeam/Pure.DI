/*
$v=true
$p=31
$d=Roots with filter
$sa=Roots
$h=`Roots<T>(name, filter)` creates a composition root for every implementation of `T` whose type name matches a wildcard filter, with `{type}` in the name template replaced by each type's name.
$h=Filtering matters when some implementations should not be exposed: here `filter: "*Email*"` picks up `EmailService` but skips `SmsService`, whose `string apiKey` dependency has no binding and therefore cannot be resolved.
$f=>[!NOTE]
$f=>Filtering roots provides fine-grained control over which implementations are exposed, useful for conditional feature activation.
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
        // Disable Resolve methods to keep the public API minimal
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