/*
$v=true
$p=1
$d=Singleton
$h=The _Singleton_ lifetime ensures that there will be a single instance of the dependency for each composition.
$f=Some articles advise using objects with a _Singleton_ lifetime as often as possible, but the following details must be considered:
$f=
$f=- For .NET the default behavior is to create a new instance of the type each time it is needed, other behavior requires, additional logic that is not free and requires additional resources.
$f=
$f=- The use of _Singleton_, adds a requirement for thread-safety controls on their use, since singletons are more likely to share their state between different threads without even realizing it.
$f=
$f=- The thread-safety control should be automatically extended to all dependencies that _Singleton_ uses, since their state is also now shared.
$f=
$f=- Logic for thread-safety control can be resource-costly, error-prone, interlocking, and difficult to test.
$f=
$f=- _Singleton_ can retain dependency references longer than their expected lifetime, this is especially significant for objects that hold "non-renewable" resources, such as the operating system Handler.
$f=
$f=- Sometimes additional logic is required to dispose of _Singleton_.
$r=Shouldly
*/

// ReSharper disable ClassNeverInstantiated.Local
// ReSharper disable CheckNamespace
// ReSharper disable ArrangeTypeModifiers

namespace Pure.DI.UsageTests.Lifetimes.SingletonScenario;

using Xunit;
using static Lifetime;

// {
//# using Pure.DI;
//# using static Pure.DI.Lifetime;
// }

public class Scenario
{
    [Fact]
    public void Run()
    {
// {
        DI.Setup(nameof(Composition))
            // This hint indicates to not generate methods such as Resolve
            .Hint(Hint.Resolve, "Off")
            .Bind().As(Singleton).To<Dependency>()
            .Bind().To<Service>()
            .Root<IService>("Root");

        var composition = new Composition();
        var service1 = composition.Root;
        var service2 = composition.Root;
        service1.Dependency1.ShouldBe(service1.Dependency2);
        service2.Dependency1.ShouldBe(service1.Dependency1);
// }
        composition.SaveClassDiagram();
    }
}

// {
interface IDependency;

class Dependency : IDependency;

interface IService
{
    public IDependency Dependency1 { get; }

    public IDependency Dependency2 { get; }
}

class Service(
    IDependency dependency1,
    IDependency dependency2)
    : IService
{
    public IDependency Dependency1 { get; } = dependency1;

    public IDependency Dependency2 { get; } = dependency2;
}
// }