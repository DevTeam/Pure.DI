/*
$v=true
$p=203
$d=Exposed roots via root arg
$h=Composition roots from other assemblies or projects can be used as a source of bindings passed through root arguments. When you add a binding to a composition from another assembly or project, the roots of the composition with the `RootKind.Exposed` type will be used in the bindings automatically. For example, in some assembly a composition is defined as:
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
*/

// ReSharper disable ClassNeverInstantiated.Local
// ReSharper disable CheckNamespace
// ReSharper disable UnusedParameter.Local
// ReSharper disable RedundantAssignment
// ReSharper disable ArrangeTypeModifiers

// ReSharper disable PartialTypeWithSinglePart
#pragma warning disable CS9113 // Parameter is unread.
namespace Pure.DI.UsageTests.Advanced.ExposedRootsViaRootArgScenario;

using OtherAssembly;
using Pure.DI;
using Xunit;

// {
//# using Pure.DI;
//# using OtherAssembly;
// }

public class Scenario
{
    [Fact]
    public void Run()
    {
// {
        DI.Setup(nameof(Composition))
            .Hint(Hint.Resolve, "Off")
            // Binds to exposed composition roots from other project
            .RootArg<CompositionInOtherProject>("baseComposition")
            .Root<Program>("GetProgram");

        var baseComposition = new CompositionInOtherProject();
        var composition = new Composition();
        var program = composition.GetProgram(baseComposition);
        program.DoSomething();
// }
    }
}

// {
partial class Program(IMyService myService)
{
    public void DoSomething() => myService.DoSomething();
}
// }