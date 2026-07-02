/*
$v=true
$p=202
$d=Exported roots via arg
$sa=Exported roots
$h=Composition roots from other assemblies or projects can be used as a source of bindings passed through composition arguments. When you add a binding to a composition from another assembly or project, the roots of the composition with the `RootKind.Exported` type will be used in the bindings automatically. For example, in some assembly a composition is defined as:
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
$r=Shouldly
*/

// ReSharper disable ClassNeverInstantiated.Local
// ReSharper disable CheckNamespace
// ReSharper disable UnusedParameter.Local
// ReSharper disable RedundantAssignment
// ReSharper disable ArrangeTypeModifiers

// ReSharper disable PartialTypeWithSinglePart
#pragma warning disable CS9113 // Parameter is unread.
namespace Pure.DI.UsageTests.Advanced.ExportedRootsViaArgScenario;

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
            .Arg<CompositionInOtherProject>("baseComposition")
            .Root<Program>("Program");

        var baseComposition = new CompositionInOtherProject();
        var composition = new Composition(baseComposition);
        var program = composition.Program;
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