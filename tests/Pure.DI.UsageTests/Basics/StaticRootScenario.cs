/*
$v=true
$p=17
$d=Static root
$h=Passing `kind: RootKinds.Static` to `Root<T>(...)` makes the generated root a static member, so an instance can be obtained directly from the composition type — `Composition.GlobalConfiguration` — without creating a composition object.
$h=This comes in handy at application entry points or in code that has no composition instance to hand.
$f=>[!NOTE]
$f=>Static roots are useful when you want to access services without creating a composition instance.
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