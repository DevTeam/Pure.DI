/*
$v=true
$p=9
$d=Initialization
$h=It is possible to create an object yourself and then inject the required dependencies via methods, properties or fields.
*/

// ReSharper disable ClassNeverInstantiated.Local
// ReSharper disable CheckNamespace
// ReSharper disable UnusedParameter.Local
// ReSharper disable ArrangeTypeModifiers

namespace Pure.DI.UsageTests.Basics.InitializationScenario;

using Shouldly;
using Xunit;

// {
interface IDependency
{
    string Name { get; }

    Guid Id { get; }
}

class Dependency : IDependency
{
    // The Ordinal attribute specifies to perform an injection and its order
    [Ordinal(1)]
    public string Name { get; set; } = "";
    
    public Guid Id { get; private set; } = Guid.Empty;

    // The Ordinal attribute specifies to perform an injection and its order
    [Ordinal(0)]
    public void SetId(Guid id) => Id = id;
}

interface IService
{
    IDependency Dependency { get; }
}

record Service(IDependency Dependency) : IService
{
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
            .RootArg<string>("name")
            .Bind().To(_ => Guid.NewGuid())
            .Bind<IDependency>().To(ctx =>
            {
                var dependency = new Dependency();
                ctx.Initialize(dependency);
                return dependency;
            })
            .Bind<IService>().To<Service>()

            // Composition root
            .Root<IService>("GetMyService");

        var composition = new Composition();
        var service = composition.GetMyService("Some name");
        service.Dependency.Name.ShouldBe("Some name");
        service.Dependency.Id.ShouldNotBe(Guid.Empty);
// }
        composition.SaveClassDiagram();
    }
}