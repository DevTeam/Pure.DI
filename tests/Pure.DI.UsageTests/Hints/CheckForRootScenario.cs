/*
$v=true
$p=7
$d=Check for a root
$h=Sometimes you need to check if you can get the root of a composition using the _Resolve_ method before calling it, this example will show you how to do it:
$f=For more hints, see [this](README.md#setup-hints) page.
$r=Shouldly
*/

// ReSharper disable CheckNamespace
// ReSharper disable UnusedMember.Local
// ReSharper disable UnusedTypeParameter
// ReSharper disable UnusedParameterInPartialMethod
// ReSharper disable ArrangeTypeModifiers
// ReSharper disable UnusedMemberInSuper.Global
// ReSharper disable ArrangeTypeMemberModifiers

namespace Pure.DI.UsageTests.Hints.CheckForRootScenario;

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
        // Check if the main user service is registered
        Composition.HasRoot(typeof(IUserService)).ShouldBeTrue();

        // Check if the root dependency for the repository with the "Primary" tag exists
        Composition.HasRoot(typeof(IUserRepository), "Primary").ShouldBeTrue();

        // Verify that the abstract repository without a tag is NOT registered as a root
        Composition.HasRoot(typeof(IUserRepository)).ShouldBeFalse();
        Composition.HasRoot(typeof(IComparable)).ShouldBeFalse();

// }
        new Composition().SaveClassDiagram();
    }
}

// {
// Repository interface for user data access
interface IUserRepository;

// Concrete repository implementation (e.g., SQL Database)
class SqlUserRepository : IUserRepository;

// Business service interface
interface IUserService
{
    IUserRepository Repository { get; }
}

// Service requiring a specific repository implementation
class UserService : IUserService
{
    // Use the "Primary" tag to specify which database to use
    [Tag("Primary")]
    public required IUserRepository Repository { get; init; }
}

partial class Composition
{
    private static readonly HashSet<(Type type, object? tag)> Roots = [];

    // The method checks if the type can be resolved without actually creating the object.
    // Useful for diagnostics.
    internal static bool HasRoot(Type type, object? key = null) =>
        Roots.Contains((type, key));

    static void Setup() =>
        DI.Setup()
            // Specifies to use the partial OnNewRoot method to register roots
            .Hint(Hint.OnNewRoot, "On")

            // Registers the repository implementation with the "Primary" tag
            .Bind("Primary").To<SqlUserRepository>()
            .Bind().To<UserService>()

            // Defines composition roots
            .Root<IUserRepository>(tag: "Primary")
            .Root<IUserService>("Root");

    // Adds a new root to the HashSet during code generation
    private static partial void OnNewRoot<TContract, T>(
        IResolver<Composition, TContract> resolver,
        string name,
        object? tag,
        Lifetime lifetime) =>
        Roots.Add((typeof(TContract), tag));
}
// }