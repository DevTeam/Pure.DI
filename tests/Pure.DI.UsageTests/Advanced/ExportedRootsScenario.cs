/*
$v=true
$p=200
$d=Exported roots
$sa=Exported roots with tags
$sa=Exported generic roots
$h=Composition roots from other assemblies or projects can be used as a source of bindings. When you add a binding to a composition from another assembly or project, the roots of the composition with the `RootKind.Exported` type will be used in the bindings automatically. For example, in some assembly a composition is defined as:
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
$f=>[!WARNING]
$f=>Only roots marked `RootKinds.Exported` are offered to other assemblies, and only the type of such a root becomes bindable there; make the exported member's type the contract you are willing to support.
$f=>[!IMPORTANT]
$f=>At this point, a composition from another assembly or another project can be used for this purpose. Compositions from the current project cannot be used in this way due to limitations of the source code generators.
$f=
$f=In a real project this is how a library ships a ready-made graph: the library composes its own internals, exports the few contracts that make sense outside, and the application binds the library composition type once instead of re-declaring its bindings.
$f=The application still owns the liftetime of the imported graph: here the library composition is bound as a `Singleton`, so one instance backs every root that uses it.
$f=The exported root keeps its own name and tag, so an import never collides with a local root of the same contract.
$f=Prefer `.DependsOn("Namespace.Composition")` ([dependent compositions](dependent-compositions.md)) inside one project, and exported roots only across an assembly boundary.
*/

// ReSharper disable ClassNeverInstantiated.Local
// ReSharper disable CheckNamespace
// ReSharper disable UnusedParameter.Local
// ReSharper disable RedundantAssignment
// ReSharper disable ArrangeTypeModifiers

// ReSharper disable PartialTypeWithSinglePart
#pragma warning disable CS9113 // Parameter is unread.
namespace Pure.DI.UsageTests.Advanced.ExportedRootsScenario;

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
            .Bind().As(Singleton).To<CompositionInOtherProject>()
            .Root<Program>("Program");

        var composition = new Composition();
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