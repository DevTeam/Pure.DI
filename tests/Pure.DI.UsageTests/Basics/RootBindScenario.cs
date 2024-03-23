/*
$v=true
$p=17
$d=RootBind
$h=You might want to register some services as roots. You can use `RootBind<T>()` method in order to reduce repetitions. The registration `composition.RootBind<IDependency>().To<Dependency>()` is an equivalent to `composition.Bind<IDependency>().To<Dependency>().Root<IDependency>()`.
*/

// ReSharper disable ClassNeverInstantiated.Local
// ReSharper disable CheckNamespace
// ReSharper disable UnusedParameter.Local
// ReSharper disable ArrangeTypeModifiers
namespace Pure.DI.UsageTests.Basics.RootBindScenario;

using Shouldly;
using Xunit;

// {
interface IDependency;

class Dependency : IDependency;
// }

public class Scenario
{
    [Fact]
    public void Run()
    {
        // {            
        DI.Setup(nameof(Composition))
            .RootBind<IDependency>("Root").To<Dependency>();

        var composition = new Composition();
        composition.Root.ShouldBeOfType<Dependency>();
        // }
        composition.SaveClassDiagram();
    }
}