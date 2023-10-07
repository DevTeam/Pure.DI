/*
$v=true
$p=0
$d=Decorator
$h=_Decorator_ is a well-known and useful design pattern. It is convenient to use tagged dependencies to build a chain of nested decorators, as in the example below:
$f=Here an instance of the _Service_ type, labeled _"base"_, is embedded in the decorator _DecoratorService_. You can use any tag that semantically reflects the feature of the abstraction being embedded. The tag can be a constant, a type, or a value of an enumerated type.
*/

// ReSharper disable ClassNeverInstantiated.Local
// ReSharper disable CheckNamespace
// ReSharper disable UnusedParameter.Local
namespace Pure.DI.UsageTests.Interception.DecoratorScenario;

using Shouldly;
using Xunit;

// {
interface IService { string GetMessage(); }

class Service : IService 
{
    public string GetMessage() => "Hello World";
}

class GreetingService : IService
{
    private readonly IService _baseService;

    public GreetingService([Tag("base")] IService baseService) =>
        _baseService = baseService;

    public string GetMessage() => $"{_baseService.GetMessage()} !!!";
}

// }

public class Scenario
{
    [Fact]
    public void Run()
    {
// {            
        DI.Setup("Composition")
            .Bind<IService>("base").To<Service>()
            .Bind<IService>().To<GreetingService>().Root<IService>("Root");

        var composition = new Composition();
        var service = composition.Root;
        service.GetMessage().ShouldBe("Hello World !!!");
// }
        composition.SaveClassDiagram();
    }
}