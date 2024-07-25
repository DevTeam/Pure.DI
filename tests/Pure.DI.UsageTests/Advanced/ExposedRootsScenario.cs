/*
$v=true
$p=200
$d=Exposed roots
$h=Composition roots from other assemblies or projects can be used as a source of bindings. When you add a binding to a composition from another assembly or project, the roots of the composition with the `RootKind.Exposed` type will be used in the bindings automatically. For example, in some assembly a composition is defined as:
$h=```c#
$h=public partial class CompositionInOtherProject
$h={
$h=    private static void Setup() => 
$h=        DI.Setup()
$h=            .Bind().As(Lifetime.Singleton).To<MyDependency>()
$h=            .Bind().To<MyService>()
$h=            .Root<IMyService>("MyService", kind: RootKinds.Exposed);
$h=}
$h=```
$f=> [!IMPORTANT]
$f=> At this point, a composition from another assembly or another project can be used for this purpose. Compositions from the current project cannot be used in this way due to limitations of the source code generators.
*/

// ReSharper disable ClassNeverInstantiated.Local
// ReSharper disable CheckNamespace
// ReSharper disable UnusedParameter.Local
// ReSharper disable RedundantAssignment
// ReSharper disable ArrangeTypeModifiers
#pragma warning disable CS9113 // Parameter is unread.
namespace Pure.DI.UsageTests.Advanced.ExposedRootsScenario;

using Integration;
using Pure.DI;
using Xunit;

// {
interface IDependency;

class Dependency : IDependency;

interface IService;

class Service(IDependency dependency) : IService;

class Program(IService service, IMyService myService)
{
    public IService Service { get; } = service;

    public void DoSomething() => myService.DoSomething();
}
// }

public class Scenario
{
    [Fact]
    public void Run()
    {
// {    
        DI.Setup(nameof(Composition))
            .Bind<IDependency>().To<Dependency>()
            .Bind<IService>().To<Service>()
            // Binds to exposed composition roots from other project
            .Bind().As(Lifetime.Singleton).To<CompositionInOtherProject>()
            .Root<Program>("Program");

        var composition = new Composition();
        var program = composition.Program;
        program.DoSomething();
// }
    }
}