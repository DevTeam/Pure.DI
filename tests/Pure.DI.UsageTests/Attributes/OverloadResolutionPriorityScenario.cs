/*
$v=true
$p=2
$d=Overload resolution priority
$sa=Constructor ordinal attribute
$sa=Auto-bindings
$h=Library types sometimes keep an older constructor for compatibility while introducing a better overload for newly compiled applications. Starting with C# 13, `OverloadResolutionPriorityAttribute` tells the compiler which overload should be preferred. Pure.DI follows the same priority when it chooses a constructor and builds its dependency graph. For a primary constructor, place the attribute on the type declaration with the `method:` target.
$f=Higher integer values are preferred; unannotated constructors have priority `0`, and negative values can de-prioritize legacy overloads. If the preferred constructor cannot be resolved, Pure.DI continues with the next applicable constructor.
$f=A primary constructor participates with the same rules as an explicitly declared constructor. Use `[method: OverloadResolutionPriority(...)]` or `[method: Ordinal(...)]` to attach a constructor attribute to a class or positional record primary constructor.
$f=When neither `OrdinalAttribute` nor an explicit `OverloadResolutionPriorityAttribute` is present, Pure.DI uses the primary constructor only as the final tie-breaker after the number of injections and constructor accessibility. This makes an otherwise equal choice deterministic without displacing a constructor designed for richer dependency injection.
$f=`OrdinalAttribute` remains the explicit Pure.DI override. When any accessible constructor is marked with `Ordinal`, only marked constructors participate, and their ordinal order takes precedence over `OverloadResolutionPriorityAttribute`.
$r=Shouldly
*/

// ReSharper disable ClassNeverInstantiated.Local
// ReSharper disable CheckNamespace
// ReSharper disable ArrangeTypeModifiers
// ReSharper disable UnusedParameter.Local
// ReSharper disable ClassNeverInstantiated.Global
// ReSharper disable UnusedMember.Global
// ReSharper disable AutoPropertyCanBeMadeGetOnly.Local
namespace Pure.DI.UsageTests.Attributes.OverloadResolutionPriorityScenario;

using System.Runtime.CompilerServices;
using Shouldly;
using Xunit;

// {
//# using Pure.DI;
//# using System.Runtime.CompilerServices;
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
            // Both constructor dependencies can be created automatically.
            // The constructor attribute determines which graph is preferred.
            .Root<BillingApiClient>("Client");

        var composition = new Composition();
        var client = composition.Client;

        client.Transport.ShouldBe("resilient-http");
// }
        composition.SaveClassDiagram();
    }
}

// {
class LegacyHttpOptions;

class ResilientHttpOptions;

[method: OverloadResolutionPriority(1)]
class BillingApiClient(ResilientHttpOptions options)
{
    // Kept so existing callers compiled against the old API continue to work.
    public BillingApiClient(LegacyHttpOptions options)
        : this(new ResilientHttpOptions()) => Transport = "legacy-http";

    // New compilations, including generated Pure.DI code, prefer the primary constructor.
    public ResilientHttpOptions Options { get; } = options;

    public string Transport { get; private set; } = "resilient-http";
}
// }
