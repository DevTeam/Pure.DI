/*
$v=true
$t=HighPerformance
$p=17
$d=Non-boxing union result
$n=11.0
$h=The compact `union` declaration stores its value as `object`, which is convenient but boxes value-type cases. On a hot path, a custom union can keep each value-type case in a dedicated field and expose the optional `HasValue`/`TryGetValue` pattern. Pure.DI still treats the selected case as the union DI contract through the compiler's implicit union conversion; no reflection, runtime lookup, or wrapper allocation is introduced by the composition.
$h=This cache lookup example binds the value-type `CacheHit` case to the `CacheLookupResult` contract. The generated root constructs `CacheHit` directly and the C# compiler invokes the union creation constructor. The consumer reads it with `TryGetValue(out CacheHit)`, so the normal success path does not access the object-typed `Value` fallback and does not box the case.
$f=>[!NOTE]
$f=>The `Value` property is mandatory for a custom union and necessarily boxes value-type cases when it is read. Use the strongly typed `TryGetValue` members on performance-sensitive paths; reserve `Value` for diagnostics and general-purpose inspection.
$f=This scenario requires the preview language version and .NET 11 Preview 5 or later, where the union runtime types are available.
$r=Shouldly
*/

// ReSharper disable CheckNamespace
// ReSharper disable ArrangeTypeModifiers
// ReSharper disable ClassNeverInstantiated.Local

namespace Pure.DI.UsageTests.HighPerformance.NonBoxingUnionScenario;

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
            // CacheHit is a value-type case of CacheLookupResult
            .Bind<CacheLookupResult>().To<CacheHit>()
            .Bind<int>("product id").To(() => 42)
            .Bind<decimal>("price").To(() => 19.95m)
            .Root<CacheLookupResult>("CachedProduct");

        var result = new Composition().CachedProduct;

        result.TryGetValue(out CacheHit hit).ShouldBeTrue();
        hit.ProductId.ShouldBe(42);
        hit.Price.ShouldBe(19.95m);
// }
        result.SaveClassDiagram();
    }
}

// {
readonly record struct CacheHit(
    [Tag("product id")] int ProductId,
    [Tag("price")] decimal Price);

readonly record struct CacheMiss(int ProductId);

// A custom union stores value-type cases directly. Value is the required
// general fallback; TryGetValue is the allocation-free access path.
[System.Runtime.CompilerServices.Union]
readonly struct CacheLookupResult : System.Runtime.CompilerServices.IUnion
{
    private readonly byte _kind;
    private readonly CacheHit _hit;
    private readonly CacheMiss _miss;

    public CacheLookupResult(CacheHit hit) =>
        (_kind, _hit, _miss) = (1, hit, default);

    public CacheLookupResult(CacheMiss miss) =>
        (_kind, _hit, _miss) = (2, default, miss);

    public object? Value => _kind switch
    {
        1 => _hit,
        2 => _miss,
        _ => null
    };

    public bool HasValue => _kind != 0;

    public bool TryGetValue(out CacheHit value)
    {
        value = _hit;
        return _kind == 1;
    }

    public bool TryGetValue(out CacheMiss value)
    {
        value = _miss;
        return _kind == 2;
    }
}
// }
