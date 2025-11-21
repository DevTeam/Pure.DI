/*
$v=true
$p=7
$d=Inheritance of compositions
*/

// ReSharper disable ClassNeverInstantiated.Local
// ReSharper disable CheckNamespace
// ReSharper disable UnusedParameter.Local
// ReSharper disable RedundantAssignment
// ReSharper disable ArrangeTypeModifiers
// ReSharper disable PartialTypeWithSinglePart
// ReSharper disable UnusedMember.Local
// ReSharper disable UnusedMember.Global
#pragma warning disable CS9113 // Parameter is unread.
namespace Pure.DI.UsageTests.Advanced.InheritanceOfCompositionsScenario;

using Pure.DI;
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
        // This hint indicates to not generate methods such as Resolve
        // Resolve = Off
// {
        var composition = new Composition();
        var app = composition.App;
// }
        app.ShouldBeOfType<App>();
    }
}

// {
// The base composition provides common infrastructure,
// such as database access, that can be shared across different parts of the application.
class Infrastructure
{
    // The 'Internal' kind indicates that this setup is intended
    // to be inherited and does not produce a public API on its own.
    private static void Setup() =>
        DI.Setup(kind: Internal)
            .Bind<IDatabase>().To<SqlDatabase>();
}

// The main composition inherits the infrastructure configuration
// and defines the application-specific dependencies.
partial class Composition : Infrastructure
{
    private void Setup() =>
        DI.Setup()
            .Bind<IUserManager>().To<UserManager>()
            .Root<App>(nameof(App));
}

interface IDatabase;

class SqlDatabase : IDatabase;

interface IUserManager;

class UserManager(IDatabase database) : IUserManager;

partial class App(IUserManager userManager)
{
    public IUserManager UserManager { get; } = userManager;
}
// }