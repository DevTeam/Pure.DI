/*
$v=true
$p=7
$d=Auto-bindings
$h=This approach works great even if DI doesn't have the appropriate bindings. :warning: But it can't be recommended if you follow the dependency inversion principle and want to make sure your types only depend on abstractions. 
*/

// ReSharper disable ClassNeverInstantiated.Local
// ReSharper disable UnusedVariable
// ReSharper disable CheckNamespace
namespace Pure.DI.UsageTests.Basics.AutoBindingScenario;

using Xunit;

// {
internal class Dependency
{
}

internal class Service
{
    public Service(Dependency dependency)
    {
    }
}
// }

public class Scenario
{
    [Fact]
    public void Run()
    {
// {            
        DI.Setup("Composition")
            .Root<Service>("Root");

        var composition = new Composition();
        var service1 = composition.Root;
        var service2 = composition.Resolve<Service>();
        var service3 = composition.Resolve(typeof(Service));
// }
    }
}