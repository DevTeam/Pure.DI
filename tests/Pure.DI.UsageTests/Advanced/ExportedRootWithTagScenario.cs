/*
$v=true
$p=201
$d=Exported roots with tags
$sa=Exported roots
$h=Composition roots from other assemblies or projects can be used as a source of bindings. When you add a binding to a composition from another assembly or project, the roots of the composition with the `RootKind.Exported` type will be used in the bindings automatically. For example, in some assembly a composition is defined as:
$h=```c#
$h=public partial class CompositionWithTagsInOtherProject
$h={
$h=    private static void Setup() =>
$h=        DI.Setup()
$h=            .Bind().As(Lifetime.Singleton).To<MyDependency>()
$h=            .Bind("Some tag").To<MyService>()
$h=            .Root<IMyService>("MyService", "Some tag", RootKinds.Exported);
$h=}
$h=```
$h=Use this when a library exposes ready-made composition roots that must be reused in another composition.
$f=Limitations: Exported roots create an integration contract between assemblies; tag names and root contracts should be versioned carefully.
$f=See also: [Tags](tags.md), [Exported roots](exported-roots.md).
*/

// ReSharper disable ClassNeverInstantiated.Local
// ReSharper disable CheckNamespace
// ReSharper disable UnusedParameter.Local
// ReSharper disable RedundantAssignment
// ReSharper disable ArrangeTypeModifiers

// ReSharper disable PartialTypeWithSinglePart
#pragma warning disable CS9113 // Parameter is unread.
namespace Pure.DI.UsageTests.Advanced.ExportedRootWithTagScenario;

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
            // Binds to exposed composition roots from other project
            .Bind().As(Singleton).To<CompositionWithTagsInOtherProject>()
            .Root<Program>("Program");

        var composition = new Composition();
        var program = composition.Program;
        program.DoSomething();
// }
    }
}

// {
partial class Program([Tag("Some tag")] IMyService myService)
{
    public void DoSomething() => myService.DoSomething();
}
// }
