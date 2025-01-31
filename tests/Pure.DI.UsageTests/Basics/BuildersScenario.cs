/*
$v=true
$p=9
$d=Builders
$h=Sometimes you need builders for all types inherited from <see cref=“T”/> available at compile time at the point where the method is called.
$f=The default builder method name is `BuildUp`. The first argument to this method will always be the instance to be built.
$r=Shouldly
*/

// ReSharper disable ClassNeverInstantiated.Local
// ReSharper disable CheckNamespace
// ReSharper disable UnusedParameter.Local
// ReSharper disable ArrangeTypeModifiers
// ReSharper disable RedundantArgumentDefaultValue
// ReSharper disable UnusedMemberInSuper.Global
// ReSharper disable UnusedAutoPropertyAccessor.Global
// ReSharper disable UnusedMember.Global
// ReSharper disable ClassNeverInstantiated.Global
namespace Pure.DI.UsageTests.Basics.BuildersScenario;

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
            // Creates a builder for each type inherited from IService.
            // These types must be available at this point in the code.
            .Builders<IService>("BuildUp");

        var composition = new Composition();
        
        var service1 = composition.BuildUp(new Service1());
        service1.Id.ShouldNotBe(Guid.Empty);
        service1.Dependency.ShouldBeOfType<Dependency>();

        var service2 = composition.BuildUp(new Service2());
        service2.Id.ShouldBe(Guid.Empty);
        service2.Dependency.ShouldBeOfType<Dependency>();
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

record Service1: IService
{
    public Guid Id { get; private set; } = Guid.Empty;
    
    // The Dependency attribute specifies to perform an injection
    [Dependency]
    public IDependency? Dependency { get; set; }

    [Dependency]
    public void SetId(Guid id) => Id = id;
}

record Service2 : IService
{
    public Guid Id => Guid.Empty;

    [Dependency]
    public IDependency? Dependency { get; set; }
}
// }