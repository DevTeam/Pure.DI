/*
$v=true
$p=2
$d=Simplified factory
$h=This example shows how to create and initialize an instance manually in a simplified form. When you use a lambda function to specify custom instance initialization logic, each parameter of that function represents an injection of a dependency. Starting with C# 10, you can also put the `Tag(...)` attribute in front of the parameter to specify the tag of the injected dependency.
*/

// ReSharper disable ClassNeverInstantiated.Local
// ReSharper disable CheckNamespace
// ReSharper disable UnusedMemberInSuper.Global
// ReSharper disable ArrangeTypeModifiers
// ReSharper disable UnusedMember.Global
// ReSharper disable ClassNeverInstantiated.Global

namespace Pure.DI.UsageTests.Basics.SimplifiedFactoryScenario;

using Shouldly;
using Xunit;

// {
//# using Pure.DI;
//# using Shouldly;
// }

public class Scenario
{
    [Fact]
    public void Run()
    {
        // Resolve = Off
// {
        DI.Setup(nameof(Composition))
            .Bind("now datetime").To(_ => DateTimeOffset.Now)
            // Injects Dependency and DateTimeOffset instances
            // and performs further initialization logic
            // defined in the lambda function
            .Bind<IDependency>().To((
                Dependency dependency,
                [Tag("now datetime")] DateTimeOffset time) =>
            {
                dependency.Initialize(time);
                return dependency;
            })
            .Bind().To<Service>()

            // Composition root
            .Root<IService>("MyService");

        var composition = new Composition();
        var service = composition.MyService;
        service.Dependency.IsInitialized.ShouldBeTrue();
// }
        composition.SaveClassDiagram();
    }
}

// {
interface IDependency
{
    DateTimeOffset Time { get; }

    bool IsInitialized { get; }
}

class Dependency : IDependency
{
    public DateTimeOffset Time { get; private set; }

    public bool IsInitialized { get; private set; }

    public void Initialize(DateTimeOffset time)
    {
        Time = time;
        IsInitialized = true;
    }
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