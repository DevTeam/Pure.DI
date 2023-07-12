/*
$v=true
$p=9
$d=Field injection
$h=To use dependency injection for a field, make sure the field is writable and simply add the _Ordinal_ attribute to that field, specifying an ordinal that will be used to determine the injection order:
*/

// ReSharper disable ClassNeverInstantiated.Local
// ReSharper disable CheckNamespace
// ReSharper disable UnusedParameter.Local
// ReSharper disable ArrangeTypeModifiers
namespace Pure.DI.UsageTests.Basics.FieldInjectionScenario;

using Shouldly;
using Xunit;

// {
interface IDependency { }

class Dependency : IDependency { }

interface IService
{
    IDependency? Dependency { get; }
}

class Service : IService
{
    [Ordinal(0)]
    internal IDependency? DependencyVal;
    
    public IDependency? Dependency => DependencyVal;
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
        TestTools.SaveClassDiagram(composition, nameof(FieldInjectionScenario));
    }
}