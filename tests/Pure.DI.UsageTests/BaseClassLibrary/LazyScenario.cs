/*
$v=true
$p=3
$d=Lazy 
*/

// ReSharper disable ClassNeverInstantiated.Local
// ReSharper disable CheckNamespace
namespace Pure.DI.UsageTests.BCL.LazyScenario;

using Shouldly;
using Xunit;

// {
internal interface IDependency { }

internal class Dependency : IDependency { }

internal interface IService
{
    IDependency Dependency { get; }
}

internal class Service : IService
{
    private readonly Lazy<IDependency> _dependency;

    public Service(Lazy<IDependency> dependency)
    {
        _dependency = dependency;
    }

    public IDependency Dependency => _dependency.Value;
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
            .Bind<IService>().To<Service>()
            .Root<IService>("Root");

        var composition = new Composition();
        var service = composition.Root;
        service.Dependency.ShouldBe(service.Dependency);
// }            
    }
}