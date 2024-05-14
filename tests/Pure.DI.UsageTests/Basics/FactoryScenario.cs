/*
$v=true
$p=2
$d=Factory
$h=This example demonstrates how to create and initialize an instance manually. This approach is more expensive to maintain, but allows you to create objects more flexibly by passing them some state and introducing dependencies. As in the case of automatic dependency embedding, objects give up control on embedding, and the whole process takes place when the object graph is created. 
*/

// ReSharper disable ClassNeverInstantiated.Local
// ReSharper disable CheckNamespace
// ReSharper disable UnusedMemberInSuper.Global
// ReSharper disable ArrangeTypeModifiers
// ReSharper disable UnusedMember.Global
namespace Pure.DI.UsageTests.Basics.FactoryScenario;

using Shouldly;
using Xunit;

// {
interface IDependency
{
    DateTimeOffset Time { get; }

    bool IsInitialized { get; }
}

class Dependency(DateTimeOffset time) : IDependency
{
    public DateTimeOffset Time { get; } = time;

    public bool IsInitialized { get; private set; }

    public void Initialize() => IsInitialized = true;
}

interface IService
{
    IDependency Dependency { get; }
}

class Service(IDependency dependency) : IService
{
    public IDependency Dependency { get; } = dependency;
}
// }

public class Scenario
{
    [Fact]
    public void Run()
    {
        // Resolve = Off
// {            
        DI.Setup(nameof(Composition))
            .Bind().To(_ => DateTimeOffset.Now)
            .Bind<IDependency>().To(ctx =>
            {
                // When building a composition of objects,
                // all of this code will be outside the lambda function:
                    
                // Some custom logic for creating an instance.
                // For example, here's how you can inject
                // an instance of a particular type
                ctx.Inject(out Dependency dependency);
                
                // And do something about it.
                dependency.Initialize();
                
                // And at the end return an instance
                return dependency;
            })
            .Bind<IService>().To<Service>()
            
            // Composition root
            .Root<IService>("Root");

        var composition = new Composition();
        var service = composition.Root;
        service.Dependency.IsInitialized.ShouldBeTrue();
// }
        composition.SaveClassDiagram();
    }
}