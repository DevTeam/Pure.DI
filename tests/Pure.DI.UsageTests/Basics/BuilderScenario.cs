/*
$v=true
$p=9
$d=Builders
$h=Sometimes you need to build up an existing composition root and inject all of its dependencies, in which case the `Builder` method will be useful, as in the example below:
$f=The default builder method name is `BuildUp`. The first argument to this method will always be the instance to be built.
$r=Shouldly
*/

// ReSharper disable ClassNeverInstantiated.Local
// ReSharper disable CheckNamespace
// ReSharper disable UnusedParameter.Local
// ReSharper disable ArrangeTypeModifiers
// ReSharper disable RedundantArgumentDefaultValue
// ReSharper disable UnusedMemberInSuper.Global
namespace Pure.DI.UsageTests.Basics.BuilderScenario;

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
            .Bind().To(_ => Guid.NewGuid())
            .Bind().To<Dependency>()
            .Builder<Service>("BuildUp");

        var composition = new Composition();
        
        var service = composition.BuildUp(new Service());
        service.Id.ShouldNotBe(Guid.Empty);
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