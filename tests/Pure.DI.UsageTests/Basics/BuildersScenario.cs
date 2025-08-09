/*
$v=true
$p=9
$d=Builders
$h=Sometimes you need builders for all types inherited from <see cref=“T”/> available at compile time at the point where the method is called.
$f=Important Notes:
$f=- The default builder method name is `BuildUp`
$f=- The first argument to the builder method is always the instance to be built
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
        // This hint indicates to not generate methods such as Resolve
        // Resolve = Off
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

        // Uses a common method to build an instance
        IService abstractService = new Service1();
        abstractService = composition.BuildUp(abstractService);
        abstractService.ShouldBeOfType<Service1>();
        abstractService.Id.ShouldNotBe(Guid.Empty);
        abstractService.Dependency.ShouldBeOfType<Dependency>();
// }
        composition.SaveClassDiagram();
    }
}

// {
interface IDependency { }

class Dependency : IDependency { }

interface IService
{
    Guid Id { get; }
    
    IDependency? Dependency { get; }
}

class Service1: IService
{
    public Guid Id { get; private set; } = Guid.Empty;

    // The Dependency attribute specifies to perform an injection
    [Dependency]
    public IDependency? Dependency { get; set; }

    [Dependency]
    public void SetId(Guid id) => Id = id;
}

class Service11: Service1 { }
class Service12: Service1 { }

class Service2 : IService
{
    public Guid Id => Guid.Empty;

    [Dependency]
    public IDependency? Dependency { get; set; }
}

class Service21: Service2 { }
class Service22: Service2 { }
// }