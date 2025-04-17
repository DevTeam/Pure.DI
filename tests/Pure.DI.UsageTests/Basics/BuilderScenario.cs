/*
$v=true
$p=9
$d=Builder
$h=Sometimes you need to build up an existing composition root and inject all of its dependencies, in which case the `Builder` method will be useful, as in the example below:
$f=Important Notes:
$f=- The default builder method name is `BuildUp`
$f=- The first argument to the builder method is always the instance to be built
$f=
$f=Advantages:
$f=- Allows working with pre-existing objects
$f=- Provides flexibility in dependency injection
$f=- Supports partial injection of dependencies
$f=- Can be used with legacy code
$f=
$f=Use Cases:
$f=- When objects are created outside the DI container
$f=- For working with third-party libraries
$f=- When migrating existing code to DI
$f=- For complex object graphs where full construction is not feasible
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
            .Builder<Service>("BuildUpService");

        var composition = new Composition();
        
        var service = composition.BuildUpService(new Service());
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

    [Dependency]
    public void SetId(Guid id) => Id = id;
}
// }