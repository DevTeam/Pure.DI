/*
$v=true
$p=0
$d=Injections of abstractions
$h=This example demonstrates the recommended approach of using abstractions instead of implementations when injecting dependencies.
$f=Usually the biggest block in the setup is the chain of bindings, which describes which implementation corresponds to which abstraction. This is necessary so that the code generator can build a composition of objects using only NOT abstract types. This is true because the cornerstone of DI technology implementation is the principle of abstraction-based programming rather than concrete class-based programming. Thanks to it, it is possible to replace one concrete implementation by another. And each implementation can correspond to an arbitrary number of abstractions.
$f=> [!TIP]
$f=> Even if the binding is not defined, there is no problem with the injection, but obviously under the condition that the consumer requests an injection NOT of abstract type.
$f=
*/

// ReSharper disable ClassNeverInstantiated.Local
// ReSharper disable CheckNamespace
// ReSharper disable UnusedMember.Local
// ReSharper disable ArrangeTypeModifiers
// ReSharper disable ArrangeTypeMemberModifiers

#pragma warning disable CS9113 // Parameter is unread.
namespace Pure.DI.UsageTests.Basics.InjectionsOfAbstractionsScenario;

using Xunit;

// {
//# using Pure.DI;
// }

public class Scenario
{
    [Fact]
    public void Run()
    {
        // Resolve = Off
// {
        DI.Setup(nameof(Composition))
            // Binding abstractions to their implementations
            .Bind<IDependency>().To<Dependency>()
            .Bind<IService>().To<Service>()

            // Specifies to create a composition root
            // of type "Program" with the name "Root"
            .Root<Program>("Root");

        var composition = new Composition();

        // var root = new Program(new Service(new Dependency()));
        var root = composition.Root;

        root.Run();
// }
        composition.SaveClassDiagram();
    }
}

// {
interface IDependency;

class Dependency : IDependency;

interface IService
{
    void DoSomething();
}

class Service(IDependency dependency) : IService
{
    public void DoSomething()
    {
    }
}

partial class Program(IService service)
{
    public void Run() => service.DoSomething();
}
// }