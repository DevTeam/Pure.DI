/*
$v=true
$p=9
$d=Builders with arguments
$h=Builders can be used with arguments as in the example below:
$f=The default builder method name is `BuildUp`. The first argument to this method will always be the instance to be built. The remaining arguments of this method will be listed in the order in which they are defined in the setup.
$r=Shouldly
*/

// ReSharper disable ClassNeverInstantiated.Local
// ReSharper disable CheckNamespace
// ReSharper disable UnusedParameter.Local
// ReSharper disable ArrangeTypeModifiers
// ReSharper disable RedundantArgumentDefaultValue
namespace Pure.DI.UsageTests.Basics.BuilderWithArgumentsScenario;

using Shouldly;
using Xunit;

// {
//# using Pure.DI;
// }

public class Scenario
{
    [Fact]
    public void Run()
    {
// {
        DI.Setup(nameof(Composition))
            .RootArg<Guid>("serviceId")
            .Bind().To<Dependency>()
            .Builder<Service>("BuildUp");

        var composition = new Composition();

        var id = Guid.NewGuid();
        var service = composition.BuildUp(new Service(), id);
        service.Id.ShouldBe(id);
        service.Dependency.ShouldBeOfType<Dependency>();
// }
        composition.SaveClassDiagram();
    }
}

// {
interface IDependency;

class Dependency : IDependency;

interface IService
{
    Guid Id { get; }
    
    IDependency? Dependency { get; }
}

record Service: IService
{
    public Guid Id { get; private set; } = Guid.Empty;
    
    // The Dependency attribute specifies to perform an injection
    [Dependency]
    public IDependency? Dependency { get; set; }

    // The Dependency attribute specifies to perform an injection
    [Dependency]
    public void SetId(Guid id) => Id = id;
}
// }