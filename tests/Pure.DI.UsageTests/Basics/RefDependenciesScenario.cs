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
            // Data
            .Bind().To<int[]>(_ => [ 1, 2, 3])
            .Root<Service>("MyService");

        var composition = new Composition();
        var service = composition.MyService;
        service.Sum.ShouldBe(6);
// }
        composition.SaveClassDiagram();
    }
}

// {
class Service
{
    public int Sum { get; private set; }

    [Ordinal]
    public void Initialize(ref Data data) =>
        Sum = data.Sum();
}

readonly ref struct Data(ref int[] data)
{
    private readonly ref int[] _dep = ref data;

    public int Sum() => _dep.Sum();
}
// }