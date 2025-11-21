/*
$v=true
$p=0
$d=Resolve hint
$h=Hints are used to fine-tune code generation. The _Resolve_ hint determines whether to generate _Resolve_ methods. By default, a set of four _Resolve_ methods are generated. Set this hint to _Off_ to disable the generation of resolve methods. This will reduce class composition generation time, and no anonymous composition roots will be generated in this case. When the _Resolve_ hint is disabled, only the regular root properties are available, so be sure to define them explicitly with the `Root<T>(...)` method.
$h=In addition, setup hints can be comments before the _Setup_ method in the form ```hint = value```, for example: `// Resolve = Off`.
$f=For more hints, see [this](README.md#setup-hints) page.
*/

// ReSharper disable ClassNeverInstantiated.Local
// ReSharper disable CheckNamespace
// ReSharper disable UnusedParameterInPartialMethod
// ReSharper disable UnusedVariable
// ReSharper disable UnusedParameter.Local
// ReSharper disable ArrangeTypeModifiers

#pragma warning disable CS9113 // Parameter is unread.
namespace Pure.DI.UsageTests.Hints.ResolveHintScenario;

using Xunit;
using static Hint;

// {
//# using Pure.DI;
//# using static Pure.DI.Hint;
// }

public class Scenario
{
    [Fact]
    public void Run()
    {
        // This hint indicates to not generate methods such as Resolve
        // Resolve = Off
// {
        DI.Setup(nameof(Composition))
            .Hint(Resolve, "Off")
            .Bind().To<DatabaseService>()
            // When the "Resolve" hint is disabled, only the regular root properties
            // are available, so be sure to define them explicitly
            // with the "Root<T>(...)" method.
            .Root<IDatabaseService>("DatabaseService")
            .Bind().To<UserService>()
            .Root<IUserService>("UserService");

        var composition = new Composition();

        // The "Resolve" method is not generated,
        // so we can only access the roots through properties:
        var userService = composition.UserService;
        var databaseService = composition.DatabaseService;

        // composition.Resolve<IUserService>(); // Compile error
// }
        composition.SaveClassDiagram();
    }
}

// {
interface IDatabaseService;

class DatabaseService : IDatabaseService;

interface IUserService;

class UserService(IDatabaseService database) : IUserService;
// }