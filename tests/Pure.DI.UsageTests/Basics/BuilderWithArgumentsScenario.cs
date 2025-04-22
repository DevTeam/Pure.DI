/*
$v=true
$p=9
$d=Builder with arguments
$h=This example demonstrates how to use builders with custom arguments in dependency injection. It shows how to pass additional parameters during the build-up process.
$f=Important Notes:
$f=- The default builder method name is `BuildUp`
$f=- The first argument to the builder method is always the instance to be built
$f=- Additional arguments are passed in the order they are defined in the setup
$f=- Root arguments can be used to provide custom values during build-up
$f=
$f=Use Cases:
$f=- When additional parameters are required during object construction
$f=- For scenarios where dependencies depend on runtime values
$f=- When specific initialization data is needed
$f=- For conditional injection based on provided arguments
$f=
$f=Best Practices
$f=- Keep the number of builder arguments minimal
$f=- Use meaningful names for root arguments
$r=Shouldly
*/

// ReSharper disable ClassNeverInstantiated.Local
// ReSharper disable CheckNamespace
// ReSharper disable UnusedParameter.Local
// ReSharper disable ArrangeTypeModifiers
// ReSharper disable RedundantArgumentDefaultValue
// ReSharper disable UnusedMemberInSuper.Global
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
        // This hint indicates to not generate methods such as Resolve
        // Resolve = Off
// {
        DI.Setup(nameof(Composition))
            .RootArg<Guid>("serviceId")
            .Bind().To<Dependency>()
            .Builder<Service>("BuildUpService");

        var composition = new Composition();

        var id = Guid.NewGuid();
        var service = composition.BuildUpService(new Service(), id);
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

    [Dependency]
    public void SetId(Guid id) => Id = id;
}
// }