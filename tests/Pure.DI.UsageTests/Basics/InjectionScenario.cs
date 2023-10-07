/*
$v=true
$p=3
$d=Injection
$h=This example shows how to manually create and initialize an instance by injecting the necessary dependencies.
$f=In addition to the dependency type, you can specify the dependency tag in the first parameter. Then the overloaded method `void Inject<T>(object tag, out T value)` is used. Where the first argument is the tag, the second is the dependency instance.
*/

// ReSharper disable ClassNeverInstantiated.Local
// ReSharper disable CheckNamespace
// ReSharper disable UnusedVariable
// ReSharper disable UnusedMemberInSuper.Global
// ReSharper disable ArrangeTypeModifiers
namespace Pure.DI.UsageTests.Basics.InjectionScenario;

using Xunit;

// {
interface IDependency { }

class Dependency : IDependency
{
}

interface IService
{
    IDependency Dependency { get; }
}

class Service : IService
{
    public Service(IDependency dependency) =>
        Dependency = dependency;

    public IDependency Dependency { get; }
}
// }

public class Scenario
{
    [Fact]
    public void Run()
    {
// {            
        DI.Setup("Composition")
            .Bind<IDependency>().To<Dependency>()
            .Bind<IService>()
                .To(ctx =>
                {
                    ctx.Inject<IDependency>(out var dependency);
                    return new Service(dependency);
                })
                .Root<IService>("Root");

        var composition = new Composition();
        var service = composition.Root;
// }            
        composition.SaveClassDiagram();
    }
}