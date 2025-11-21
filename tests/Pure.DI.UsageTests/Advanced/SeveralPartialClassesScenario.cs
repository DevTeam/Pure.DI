/*
$v=true
$p=11
$d=A few partial classes
$h=The setting code for one Composition can be located in several methods and/or in several partial classes.
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
    // This hint indicates to not generate methods such as Resolve
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
    // Determines which objects can be retrieved directly from the container.
    private static void SetupApi() =>
        DI.Setup()
            .Root<IClassCommenter>("Commenter");
}
// }