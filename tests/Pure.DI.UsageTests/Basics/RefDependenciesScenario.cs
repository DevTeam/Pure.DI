/*
$v=true
$p=20
$d=Ref dependencies
$r=Shouldly
*/

// ReSharper disable once CheckNamespace
// ReSharper disable ClassNeverInstantiated.Global
namespace Pure.DI.UsageTests.Basics.RefDependenciesScenario;

using Xunit;

// {
//# using Pure.DI;
// }

public class Scenario
{
    [Fact]
    public void Run()
    {
// {        
        DI.Setup("Composition")
            // Represents a large data set or buffer
            .Bind().To<int[]>(() => [10, 20, 30])
            .Root<Service>("MyService");

        var composition = new Composition();
        var service = composition.MyService;
        service.Sum.ShouldBe(60);
// }
        composition.SaveClassDiagram();
    }
}

// {
class Service
{
    public int Sum { get; private set; }

    // Ref structs cannot be fields, so they are injected via a method
    // with the [Ordinal] attribute. This allows working with
    // high-performance types like Span<T> or other ref structs.
    [Ordinal]
    public void Initialize(ref Data data) =>
        Sum = data.Sum();
}

// A ref struct that holds a reference to the data
// to process it without additional memory allocations
readonly ref struct Data(ref int[] data)
{
    private readonly ref int[] _dep = ref data;

    public int Sum() => _dep.Sum();
}
// }