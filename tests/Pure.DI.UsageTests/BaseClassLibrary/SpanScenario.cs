/*
$v=true
$p=4
$d=Span and ReadOnlySpan
$h=Specifying `Span<T>` and `ReadOnlySpan<T>` work the same as with the array `T[]`.
$f=This scenario is even more efficient when the `Span[]` or `ReadOnlySpan[]` element has a value type. In this case, there are no heap allocations, and the composition root `IService` looks like this:
$f=```c#
$f=public IService Root
$f={
$f=  get
$f=  {
$f=    ReadOnlySpan<Dependency> dependencies = stackalloc Dependency[3] { new Dependency(), new Dependency(), new Dependency() };
$f=    return new Service(dependencies);
$f=  }
$f=}
$f=```
*/

// ReSharper disable ClassNeverInstantiated.Local
// ReSharper disable CheckNamespace
namespace Pure.DI.UsageTests.BCL.SpanScenario;

using Shouldly;
using Xunit;

// {
internal class Dependency
{
}

internal interface IService
{
    int Count { get; }
}

internal class Service : IService
{
    public Service(ReadOnlySpan<Dependency> dependencies)
    {
        Count = dependencies.Length;
    }

    public int Count { get; }
}
// }

public class Scenario
{
    [Fact]
    public void Run()
    {
        // ToString = On
// {            
        DI.Setup("Composition")
            .Bind<Dependency>('a').To<Dependency>()
            .Bind<Dependency>('b').To<Dependency>()
            .Bind<Dependency>('c').To<Dependency>()
            .Bind<IService>().To<Service>()
            .Root<IService>("Root");

        var composition = new Composition();
        var service = composition.Root;
        service.Count.ShouldBe(3);
// }            
        TestTools.SaveClassDiagram(new Composition(), nameof(SpanScenario));
    }
}