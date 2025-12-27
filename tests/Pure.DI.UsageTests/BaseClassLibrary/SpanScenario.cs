/*
$v=true
$p=4
$d=Span and ReadOnlySpan
$h=Specifying `Span<T>` and `ReadOnlySpan<T>` work the same as with the array `T[]`.
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
$r=Shouldly
*/

// ReSharper disable ClassNeverInstantiated.Local
// ReSharper disable CheckNamespace
// ReSharper disable ArrangeTypeModifiers
// ReSharper disable UnusedMember.Global
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
        // This hint indicates to not generate methods such as Resolve
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