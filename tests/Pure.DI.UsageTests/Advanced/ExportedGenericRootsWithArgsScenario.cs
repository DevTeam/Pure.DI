/*
$v=true
$p=211
$d=Exported generic roots with args
$sa=Exported generic roots
$h=Composition roots from other assemblies or projects can be used as a source of bindings. When you add a binding to a composition from another assembly or project, the roots of the composition with the `RootKind.Exported` type will be used in the bindings automatically. For example, in some assembly a composition is defined as:
$h=```c#
$h=public partial class CompositionWithGenericRootsAndArgsInOtherProject
$h={
$h=    private static void Setup() =>
$h=        DI.Setup()
$h=            .Hint(Hint.Resolve, "Off")
$h=            .RootArg<int>("id")
$h=            .Bind().As(Lifetime.Singleton).To<MyDependency>()
$h=            .Bind().To<MyGenericService<TT>>()
$h=            .Root<IMyGenericService<TT>>("GetMyService", kind: RootKinds.Exported);
$h=}
$h=```
$f=>[!WARNING]
$f=>The root arguments an exported root declares become parameters of the generated member on the consuming side, so the consumer's call must supply them for every instantiation.
$f=>[!IMPORTANT]
$f=>At this point, a composition from another assembly or another project can be used for this purpose. Compositions from the current project cannot be used in this way due to limitations of the source code generators.
$f=
$f=This is the usual shape of a library service that needs both the caller's type and a per-call value, for example a store scoped to one tenant id plus the model it manages.
$f=Because the value is a root argument rather than a composition argument, each call can pass a different id while the imported graph stays shared.
$f=Prefer [exported generic roots](exported-generic-roots.md) when the library can pick the value itself, and a composition argument when it is fixed for the whole application.
$r=Shouldly
*/

// ReSharper disable ClassNeverInstantiated.Local
// ReSharper disable CheckNamespace
// ReSharper disable UnusedParameter.Local
// ReSharper disable RedundantAssignment
// ReSharper disable ArrangeTypeModifiers

// ReSharper disable PartialTypeWithSinglePart
#pragma warning disable CS9113 // Parameter is unread.
namespace Pure.DI.UsageTests.Advanced.ExposedGenericRootsWithArgsScenario;

using OtherAssembly;
using Pure.DI;
using Xunit;
using static Lifetime;

// {
//# using Pure.DI;
//# using static Pure.DI.Lifetime;
//# using OtherAssembly;
// }

public class Scenario
{
    [Fact]
    public void Run()
    {
        // Disable Resolve methods to keep the public API minimal
        // Resolve = Off
// {
        DI.Setup(nameof(Composition))
            .RootArg<int>("id")
            // Binds to exposed composition roots from other project
            .Bind().As(Singleton).To<CompositionWithGenericRootsAndArgsInOtherProject>()
            .Root<Program>("GetProgram");

        var composition = new Composition();
        var program = composition.GetProgram(33);
        program.DoSomething(99);
// }
    }
}

// {
partial class Program(IMyGenericService<int> myService)
{
    public void DoSomething(int value) => myService.DoSomething(value);
}
// }