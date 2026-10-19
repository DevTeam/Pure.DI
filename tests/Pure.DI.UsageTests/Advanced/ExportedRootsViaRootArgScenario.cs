/*
$v=true
$p=203
$d=Exported roots via root arg
$sa=Exported roots
$h=Composition roots from other assemblies or projects can be used as a source of bindings passed through root arguments. When you add a binding to a composition from another assembly or project, the roots of the composition with the `RootKind.Exported` type will be used in the bindings automatically. For example, in some assembly a composition is defined as:
$h=```c#
$h=public partial class CompositionInOtherProject
$h={
$h=    private static void Setup() =>
$h=        DI.Setup()
$h=            .Bind().As(Lifetime.Singleton).To<MyDependency>()
$h=            .Bind().To<MyService>()
$h=            .Root<IMyService>("MyService", kind: RootKinds.Exported);
$h=}
$h=```
$f=>[!IMPORTANT]
$f=>At this point, a composition from another assembly or another project can be used for this purpose. Compositions from the current project cannot be used in this way due to limitations of the source code generators.
$f=
$f=Reach for it when several roots of the composition share one imported graph and that graph differs per call: the base composition instance arrives as a root argument, so each call can bring its own.
$f=The composition itself stays parameterless, so a `Resolve`-based consumer or a factory delegate can still create it once and call roots later.
$f=Root arguments make the root a method; if a consumer only needs the shared graph, prefer the composition argument form so the call site stays a property.
$r=Shouldly
*/

// ReSharper disable ClassNeverInstantiated.Local
// ReSharper disable CheckNamespace
// ReSharper disable UnusedParameter.Local
// ReSharper disable RedundantAssignment
// ReSharper disable ArrangeTypeModifiers

// ReSharper disable PartialTypeWithSinglePart
#pragma warning disable CS9113 // Parameter is unread.
namespace Pure.DI.UsageTests.Advanced.ExportedRootsViaRootArgScenario;

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
        // Disable Resolve methods to keep the public API minimal
        // Resolve = Off
// {
        DI.Setup(nameof(Composition))
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