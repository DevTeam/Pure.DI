/*
$v=true
$p=2
$d=Factory
$h=This example demonstrates how to create and initialize an instance manually.
$h=At the compilation stage, the set of dependencies that an object needs in order to be created is determined. In most cases, this happens automatically according to the set of constructors and their arguments and does not require any additional customization efforts. But sometimes it is necessary to manually create an object, as in lines of code:
$f=This approach is more expensive to maintain, but allows you to create objects more flexibly by passing them some state and introducing dependencies. As in the case of automatic dependency injecting, objects give up control on embedding, and the whole process takes place when the object graph is created.
$f=> [!IMPORTANT]
$f=> The method `Inject()`cannot be used outside of the binding setup.
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
            .Bind().To(_ => DateTimeOffset.Now)
            .RootArg<bool>("isFake", "FakeArgTag")
            .Bind<IDependency>().To<IDependency>(ctx =>
            {
                // When building a composition of objects,
                // all of this code will be outside the lambda function.

                // Some custom logic for creating an instance.
                // For example, here's how you can inject and initialize
                // an instance of a particular type:

                ctx.Inject<bool>("FakeArgTag", out var isFake);
                if (isFake)
                {
                    return new FakeDependency();
                }

                ctx.Inject(out Dependency dependency);
                dependency.Initialize();
                return dependency;

            })
            .Bind<IService>().To<Service>()

            // Composition root
            .Root<IService>("GetMyService");

        var composition = new Composition();

        var service = composition.GetMyService(isFake: false);
        service.Dependency.ShouldBeOfType<Dependency>();
        service.Dependency.IsInitialized.ShouldBeTrue();
        
        var serviceWithFakeDependency = composition.GetMyService(isFake: true);
        serviceWithFakeDependency.Dependency.ShouldBeOfType<FakeDependency>();
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

class Dependency(DateTimeOffset time) : IDependency
{
    public DateTimeOffset Time { get; } = time;

    public bool IsInitialized { get; private set; }

    public void Initialize() => IsInitialized = true;
}

class FakeDependency : IDependency
{
    public DateTimeOffset Time => DateTimeOffset.MinValue;

    public bool IsInitialized => true;
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