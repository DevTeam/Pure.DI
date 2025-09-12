/*
$v=true
$p=21
$d=Ref dependencies
$r=Shouldly
*/

// ReSharper disable CheckNamespace
// ReSharper disable ArrangeTypeModifiers
// ReSharper disable UnusedVariable
// ReSharper disable UnusedMember.Local
// ReSharper disable ArrangeTypeMemberModifiers
// ReSharper disable MemberCanBeMadeStatic.Global
#pragma warning disable CS9113 // Parameter is unread.
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