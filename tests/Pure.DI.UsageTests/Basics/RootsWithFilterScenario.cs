/*
$v=true
$p=31
$d=Roots with filter
$sa=Roots
$h=`Roots<T>(name, filter)` creates a composition root for every implementation of `T` whose type name matches a wildcard filter, with `{type}` in the name template replaced by each type's name.
$h=Filtering is how a plugin host exposes only the implementations it can actually build: here `filter: "*Email*"` picks up `EmailService` and skips `SmsService`, whose `string apiKey` dependency has no binding and therefore cannot be resolved.
$f=Without a filter, an implementation that the graph cannot satisfy fails the compilation; the filter is therefore a statement about which implementations are in scope, not a cosmetic option.
$f=The filter is a case-sensitive wildcard over the type name and is evaluated at compile time; a filter that matches nothing is reported as `DIE016`.
$f=Every generated root is an ordinary property or method, so name the template with a recognizable prefix (`My{type}`) to keep generated members distinct from hand-written ones.
$f=Prefer `Roots<T>` when implementations are discovered from the assembly; use explicit `Root<T>` calls when the set is small and stable.
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