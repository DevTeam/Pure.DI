/*
$v=true
$p=10
$d=Instance Initialization
$h=This example shows how to build up an instance with all the necessary dependencies and manually prepare it for further use. 
*/

// ReSharper disable ClassNeverInstantiated.Local
// ReSharper disable CheckNamespace
// ReSharper disable UnusedMemberInSuper.Global
// ReSharper disable ArrangeTypeModifiers
namespace Pure.DI.UsageTests.Basics.InstanceInitializationScenario;

using Shouldly;
using Xunit;

// {
interface IDependency
{
}

class Dependency : IDependency { }

interface IService
{
    string ServiceName { get; }
    
    IDependency Dependency { get; }
    
    bool IsInitialized { get; }
}

class Service : IService
{
    public Service(string serviceName, IDependency dependency)
    {
        ServiceName = serviceName;
        Dependency = dependency;
    }

    public string ServiceName { get; }
    
    public IDependency Dependency { get; }
    
    public bool IsInitialized { get; private set; }

    public void Initialize() =>
        IsInitialized = true;
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
            .Arg<string>("serviceName")
            .Bind<IService>()
                .To<Service>(ctx =>
                {
                    // Builds up an instance with all necessary dependencies
                    ctx.Inject<Service>(out var service);

                    // Executing all the necessary logic
                    // to prepare the instance for further use
                    service.Initialize();
                    return service;
                })
                .Root<IService>("Root");

        var composition = new Composition("My Service");
        var service = composition.Root;
        service.ServiceName.ShouldBe("My Service");
        service.IsInitialized.ShouldBeTrue();
// }
        composition.SaveClassDiagram();
    }
}