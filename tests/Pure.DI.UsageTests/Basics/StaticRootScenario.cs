/*
$v=true
$t=Basics
$t=HighPerformance
$p=28
$p=HighPerformance:11
$d=Static root
$sa=Composition root kinds
$h=Passing `kind: RootKinds.Static` to `Root<T>(...)` makes the generated root a static member, so an instance can be obtained directly from the composition type — `Composition.GlobalConfiguration` — without creating a composition object.
$h=This is useful for stateless entry-point services such as static configuration readers, validators, or one-shot command helpers where the composition itself does not carry state.
$f=>[!NOTE]
$f=>Static roots keep the call site compact and avoid allocating a composition instance for graphs that do not need composition-level state.
$f=Avoid static roots for graphs that depend on scoped state, per-composition caches, or externally supplied constructor arguments. In those cases, an instance composition keeps ownership and lifetime boundaries clearer.
$r=Shouldly
*/

// ReSharper disable ClassNeverInstantiated.Local
// ReSharper disable CheckNamespace
// ReSharper disable UnusedParameter.Local
// ReSharper disable ArrangeTypeModifiers

#pragma warning disable CS9113 // Parameter is unread.
namespace Pure.DI.UsageTests.Basics.StaticRootScenario;

using Shouldly;
using Xunit;

// {
//# using Pure.DI;
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
            .Bind().As(Lifetime.PerResolve).To<FileSystem>()
            .Bind().To<Configuration>()
            .Root<IConfiguration>("GlobalConfiguration", kind: RootKinds.Static);

        var configuration = Composition.GlobalConfiguration;
        configuration.ShouldBeOfType<Configuration>();
// }
        new Composition().SaveClassDiagram();
    }
}

// {
interface IFileSystem;

class FileSystem : IFileSystem;

interface IConfiguration;

class Configuration(IFileSystem fileSystem) : IConfiguration;
// }
