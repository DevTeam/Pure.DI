/*
$v=true
$p=6
$d=OnNewInstance regular expression hint
$sa=OnNewInstance wildcard hint
$h=Hints are used to fine-tune code generation. The `OnNewInstance` hint determines whether to generate partial `OnNewInstance` method.
$h=The hook fires when an instance is created, so it measures construction rather than injection: it is where startup timing, instance counting and "who built this object" logging belong.
$h=In addition, setup hints can be comments before the `Setup` method in the form `hint = value`, for example: `// OnNewInstance = On`.
$f=The `OnNewInstanceLifetimeRegularExpression` hint helps you define a set of lifetimes that require instance creation control. You can use it to specify a regular expression to filter bindings by lifetime name.
$f=With `(Singleton|PerBlock)` only those two lifetimes are reported here, which is exactly what you want when the question is "how often is my shared state created".
$f=The value is passed by `ref`, so the hook may replace the instance before it is stored or injected - and it runs before disposal ownership is transferred to the composition.
$f=A singleton is created once per composition, so two resolutions of `OrderProcessor` report one `GlobalCache` and two processors: the count itself is the documentation of the lifetimes.
$f=Prefer this hook for measurements and nothing else in release builds; the filter keeps the instrumentation off the hot transient path where it would distort what you measure.
$f=For more hints, see [this](../README.md#setup-hints) page.
$r=Shouldly
*/

// ReSharper disable ClassNeverInstantiated.Local
// ReSharper disable CheckNamespace
// ReSharper disable UnusedParameterInPartialMethod
// ReSharper disable UnusedVariable
// ReSharper disable UnusedMemberInSuper.Global
// ReSharper disable ArrangeTypeModifiers
// ReSharper disable UnusedMember.Global

namespace Pure.DI.UsageTests.Hints.OnNewInstanceRegularExpressionHintScenario;

using Shouldly;
using Xunit;
using static Hint;

// {
//# using Pure.DI;
//# using static Pure.DI.Hint;
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
            .Hint(OnNewInstance, "On")
            .Hint(OnNewInstanceLifetimeRegularExpression, "(Singleton|PerBlock)")
            .Bind().As(Lifetime.Singleton).To<GlobalCache>()
            .Bind().As(Lifetime.PerBlock).To<OrderProcessor>()
            .Root<IOrderProcessor>("OrderProcessor");

        var log = new List<string>();
        var composition = new Composition(log);

        // Create the OrderProcessor twice
        var processor1 = composition.OrderProcessor;
        var processor2 = composition.OrderProcessor;

        log.ShouldBe([
            "GlobalCache created",
            "OrderProcessor created",
            "OrderProcessor created"
        ]);
        // }
        composition.SaveClassDiagram();
    }
}

// {
interface IGlobalCache;

class GlobalCache : IGlobalCache;

interface IOrderProcessor
{
    IGlobalCache Cache { get; }
}

class OrderProcessor(IGlobalCache cache) : IOrderProcessor
{
    public IGlobalCache Cache { get; } = cache;
}

internal partial class Composition(List<string> log)
{
    partial void OnNewInstance<T>(
        ref T value,
        object? tag,
        Lifetime lifetime) =>
        log.Add($"{typeof(T).Name} created");
}
// }