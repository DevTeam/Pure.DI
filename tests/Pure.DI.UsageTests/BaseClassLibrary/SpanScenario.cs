/*
$v=true
$p=6
$d=Span and ReadOnlySpan
$sa=Array
$h=Specifying `Span<T>` and `ReadOnlySpan<T>` work the same as with the array `T[]` for immediate constructor or method use.
$f=This scenario is even more efficient in the case of `Span<T>` or `ReadOnlySpan<T>` when `T` is a value type. In this case, there is no heap allocation, and the composition root `IPath` looks like this:
$f=```c#
$f=public IPath Path
$f={
$f=  get
$f=  {
$f=    ReadOnlySpan<Point> points = stackalloc Point[3] { new Point(1, 1), new Point(2, 2), new Point(3, 3) };
$f=    return new Path(points);
$f=  }
$f=}
$f=```
$f=Constructor injection into a heap type is available for compatibility and reports warning `DIW012`. Prefer method injection for new code when the stack-only value is only needed during initialization.
$f=Generic root arguments with `where T : allows ref struct` follow the same rules. Pure.DI treats such `T` as maybe stack-only and emits `scoped T` in generated root signatures.
$f=When a root has several arguments, `scoped` is applied only to arguments that are stack-only or maybe stack-only themselves. Heap-safe wrappers such as `Wrapper<T>` remain regular parameters even when `T` allows ref structs.
$f=Factory bodies may resolve and consume stack-only values immediately via `ctx.Inject<T>(...)` when the API target supports `allows ref struct`. Pure.DI reports `DIE049` when such values are captured behind a generated delegate or deferred factory.
$f=Factory overrides may pass stack-only values with `ctx.Override<T>(...)` or `ctx.Let<T>(...)` only inside the current synchronous factory frame or the current delegate invocation. Delegate arguments such as `T text` can be consumed immediately by method injection, but Pure.DI still reports `DIE049` if an outer stack-only value is captured by the delegate or if `text` is passed into a nested/returned delegate.
$f=When such overrides are used in factory delegates, the usual thread-safety rule still applies: wrap the override and the following injection in `lock (ctx.Lock)` or disable thread safety for known single-threaded compositions. Otherwise Pure.DI reports `DIW013`.
$f=Generic interfaces are allowed when the implementation is heap-safe, for example `class Parser<T> : IParser<T>`. Pure.DI reports `DIE048` only when the implementation itself is stack-only, such as `ref struct Parser<T> : IParser<T>`.
$f=Pure.DI reports errors when `Span<T>`, `ReadOnlySpan<T>`, or custom `ref struct` values are injected into fields, properties, stored lifetimes, delegate captures, or stack-only interface conversions.
$r=Shouldly
*/

// ReSharper disable ClassNeverInstantiated.Local
// ReSharper disable CheckNamespace
// ReSharper disable ArrangeTypeModifiers
// ReSharper disable UnusedMember.Global
[assembly: System.Diagnostics.CodeAnalysis.SuppressMessage("WRN", "DIW012:WRN")]

namespace Pure.DI.UsageTests.BCL.SpanScenario;

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
            .Bind<Point>('a').To(() => new Point(1, 1))
            .Bind<Point>('b').To(() => new Point(2, 2))
            .Bind<Point>('c').To(() => new Point(3, 3))
            .Bind<IPath>().To<Path>()

            // Composition root
            .Root<IPath>("Path");

        var composition = new Composition();
        var path = composition.Path;
        path.PointCount.ShouldBe(3);
// }
        composition.SaveClassDiagram();
    }
}

// {
readonly struct Point(int x, int y)
{
    public int X { get; } = x;

    public int Y { get; } = y;
}

interface IPath
{
    int PointCount { get; }
}

class Path(ReadOnlySpan<Point> points) : IPath
{
    // The 'points' span is allocated on the stack, so it's very efficient.
    // However, we cannot store it in a field because it's a ref struct.
    // We can process it here in the constructor.
    public int PointCount { get; } = points.Length;
}
// }
