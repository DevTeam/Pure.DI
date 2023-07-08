/*
$v=true
$p=9
$d=Method injection
$h=To use dependency implementation for a method, simply add the _Ordinal_ attribute to that method, specifying the sequence number that will be used to define the call to that method:
*/

// ReSharper disable ClassNeverInstantiated.Local
// ReSharper disable CheckNamespace
// ReSharper disable UnusedParameter.Local
namespace Pure.DI.UsageTests.Basics.MethodInjectionScenario;

using Shouldly;
using Xunit;

// {
internal interface IDependency { }

internal class Dependency : IDependency { }

internal interface IService
{
    IDependency? Dependency { get; }
}

internal class Service : IService
{
    [Ordinal(0)]
    public void SetDependency(IDependency dependency)
    {
        Dependency = dependency;
    }

    public IDependency? Dependency { get; private set; }
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
            .Bind<IDependency>().To<Dependency>()
            .Bind<IService>().To<Service>().Root<IService>("Root");

        var composition = new Composition();
        var service = composition.Root;
        service.Dependency.ShouldBeOfType<Dependency>();
// }            
        TestTools.SaveClassDiagram(composition, nameof(MethodInjectionScenario));
    }
}