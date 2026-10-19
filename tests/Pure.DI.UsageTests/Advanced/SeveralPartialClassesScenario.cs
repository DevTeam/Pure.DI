/*
$v=true
$p=4
$d=A few partial classes
$sa=Partial class
$h=The setting code for one Composition can be located in several methods and/or in several partial classes.
$h=Each `DI.Setup()` call that does not name a composition adds to the composition of the enclosing partial class, so the parts merge regardless of which file or method they live in.
$f=This is how a large composition stays readable: one part per layer - infrastructure, domain, public API - each in its own file, often owned by a different team.
$f=>[!NOTE]
$f=>Splitting composition setup across multiple partial classes can improve organization for large compositions but may reduce readability if overused.
$f=
$f=All parts share one set of roots and one set of bindings, so a later part can override a binding of an earlier one and `DIW000` reports the duplicate.
$f=A part may leave `Resolve` enabled or disabled by hint, but a later part overrides the earlier hint, so keep hints in one place.
$f=Prefer `DependsOn` with named `Internal` setups ([dependent compositions](dependent-compositions.md)) when the layers must be reusable by more than one composition.
$r=Shouldly
*/

// ReSharper disable CheckNamespace
// ReSharper disable UnusedParameter.Local
// ReSharper disable UnusedMember.Local
// ReSharper disable ClassNeverInstantiated.Global
// ReSharper disable ArrangeTypeModifiers
// ReSharper disable ArrangeTypeMemberModifiers

#pragma warning disable CS9113 // Parameter is unread.
namespace Pure.DI.UsageTests.Advanced.SeveralPartialClassesScenario;

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
// {
        var composition = new Composition();
        var commenter = composition.Commenter;
// }
        commenter.ShouldBeOfType<ClassCommenter>();
        composition.SaveClassDiagram();
    }
}

// {
// Infrastructure interface for retrieving comments (e.g., from a database)
interface IComments;

class Comments : IComments;

// Domain service for handling class comments
interface IClassCommenter;

class ClassCommenter(IComments comments) : IClassCommenter;

partial class Composition
{
// }
    // Disable Resolve methods to keep the public API minimal
    // Resolve = Off
// {
    // Infrastructure layer setup.
    // This method isolates the configuration of databases or external services.
    static void SetupInfrastructure() =>
        DI.Setup()
            .Bind<IComments>().To<Comments>();
}

partial class Composition
{
    // Domain logic layer setup.
    // Here we bind domain services.
    static void SetupDomain() =>
        DI.Setup()
            .Bind<IClassCommenter>().To<ClassCommenter>();
}

partial class Composition
{
    // Public API setup (Composition Roots).
    // Determines which objects can be retrieved directly from the composition.
    private static void SetupApi() =>
        DI.Setup()
            .Root<IClassCommenter>("Commenter");
}
// }