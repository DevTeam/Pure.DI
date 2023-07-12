/*
$v=true
$p=4
$d=Span and ReadOnlySpan
$h=Specifying `Span<T>` and `ReadOnlySpan<T>` work the same as with the array `T[]`.
$f=This scenario is even more efficient in the case of `Span<T>` or `ReadOnlySpan<T>` when `T` is a value type. In this case, there is no heap allocation, and the composition root `IService` looks like this:
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
// ReSharper disable ArrangeTypeModifiers
namespace Pure.DI.UsageTests.BCL.SpanScenario;

using Shouldly;
using Xunit;

// {
struct Dependency
{
}

interface IService
{
    int Count { get; }
}

class Service : IService
{
    public Service(ReadOnlySpan<Dependency> dependencies) => 
        Count = dependencies.Length;

    public int Count { get; }
}
// }

public class Scenario
{
    [Fact]
    public void Run()
    {
        // ToString = On
        // FormatCode = On
// {            
        DI.Setup("Composition")
            .Bind<Dependency>('a').To<Dependency>()
            .Bind<Dependency>('b').To<Dependency>()
            .Bind<Dependency>('c').To<Dependency>()
            .Bind<IService>().To<Service>().Root<IService>("Root");

        var composition = new Composition();
        var service = composition.Root;
        service.Count.ShouldBe(3);
// }            
        TestTools.SaveClassDiagram(new Composition(), nameof(SpanScenario));
    }
}