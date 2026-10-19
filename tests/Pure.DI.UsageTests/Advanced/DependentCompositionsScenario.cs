/*
$v=true
$p=6
$d=Dependent compositions
$sa=Composition roots
$sa=Global compositions
$sa=Inheritance of compositions
$h=The `Setup` method has an additional argument `kind`, which defines the type of composition:
$h=- `CompositionKind.Public` - will create a normal composition class, this is the default setting and can be omitted, it can also use the `DependsOn` method to use it as a dependency in other compositions
$h=- `CompositionKind.Internal` - the composition class will not be created, but that composition can be used to create other compositions by calling the `DependsOn` method with its name
$h=- `CompositionKind.Global` - the composition class will also not be created, but that composition will automatically be used to create other compositions
$h=Use dependent compositions to split large object graphs into reusable setup layers.
$h=An `Internal` setup is the right choice for a reusable layer that must not expose a composition class of its own; `Global` is for settings that every composition in the project should inherit, such as hints.
$f=This is how a multi-project solution keeps one setup per assembly: each library declares an `Internal` setup, the executable composes them with `.DependsOn("Namespace.Composition")` and exposes named roots.
$f=Setups are matched by name, so use `nameof(Composition)` rather than a string literal to keep `DependsOn` refactor-safe.
$f=A dependent setup contributes its bindings and roots, not its generated members: the composition that declares `DependsOn` owns the generated class and its roots.
$f=Limitations: too many setup layers can make graph ownership unclear; keep boundaries explicit and naming consistent.
$f=Prefer plain class inheritance ([Inheritance of compositions](inheritance-of-compositions.md)) when the layers are in one project and share hand-written members; use `DependsOn` across assemblies and projects.
*/

// ReSharper disable ClassNeverInstantiated.Local
// ReSharper disable CheckNamespace
// ReSharper disable UnusedParameter.Local
// ReSharper disable RedundantAssignment
// ReSharper disable ArrangeTypeModifiers

// ReSharper disable PartialTypeWithSinglePart
#pragma warning disable CS9113 // Parameter is unread.
namespace Pure.DI.UsageTests.Advanced.DependentCompositionsScenario;

using Pure.DI;
using UsageTests;
using Shouldly;
using Xunit;
using static CompositionKind;

// {
//# using Pure.DI;
//# using static Pure.DI.CompositionKind;
// }

public class Scenario
{
    [Fact]
    public void Run()
    {
        // Disable Resolve methods to keep the public API minimal
        // Resolve = Off
        // {
        // This setup does not generate code, but can be used as a dependency
        // and requires the use of the "DependsOn" call to add it as a dependency
        DI.Setup("Infrastructure", Internal)
            .Bind<IDatabase>().To<SqlDatabase>();

        // This setup generates code and can also be used as a dependency
        DI.Setup(nameof(Composition))
            // Uses "Infrastructure" setup
            .DependsOn("Infrastructure")
            .Bind<IUserService>().To<UserService>()
            .Root<IUserService>("UserService");

        // As in the previous case, this setup generates code and can also be used as a dependency
        DI.Setup(nameof(OtherComposition))
            // Uses "Composition" setup
            .DependsOn(nameof(Composition))
            .Root<Ui>("Ui");

        var composition = new Composition();
        var userService = composition.UserService;

        var otherComposition = new OtherComposition();
        userService = otherComposition.Ui.UserService;
        // }
        userService.ShouldBeOfType<UserService>();
        otherComposition.SaveClassDiagram();
    }
}

// {
interface IDatabase;

class SqlDatabase : IDatabase;

interface IUserService;

class UserService(IDatabase database) : IUserService;

partial class Ui(IUserService userService)
{
    public IUserService UserService { get; } = userService;
}
// }
