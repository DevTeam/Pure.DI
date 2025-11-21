/*
$v=true
$p=5
$d=ToString hint
$h=Hints are used to fine-tune code generation. The _ToString_ hint determines if the _ToString()_ method should be generated. This method provides a text-based class diagram in the format [mermaid](https://mermaid.js.org/). To see this diagram, just call the ToString method and copy the text to [this site](https://mermaid.live/). An example class diagram can be seen below.
$h=In addition, setup hints can be comments before the _Setup_ method in the form ```hint = value```, for example: `// ToString = On`.
$f=Developers who start using DI technology often complain that they stop seeing the structure of the application because it is difficult to understand how it is built. To make life easier, you can add the _ToString_ hint by telling the generator to create a `ToString()` method.
$f=For more hints, see [this](README.md#setup-hints) page.
*/

// ReSharper disable ClassNeverInstantiated.Local
// ReSharper disable CheckNamespace
// ReSharper disable UnusedParameterInPartialMethod
// ReSharper disable UnusedVariable
// ReSharper disable UnusedParameter.Local
// ReSharper disable SuggestVarOrType_BuiltInTypes
// ReSharper disable ArrangeTypeModifiers

#pragma warning disable CS9113 // Parameter is unread.
namespace Pure.DI.UsageTests.Hints.ToStringHintScenario;

using Xunit;

// {
//# using Pure.DI;
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
            .Hint(Hint.ToString, "On")
            .Bind().To<Database>()
            .Bind().To<UserRepository>()
            .Root<IUserRepository>("GetUserRepository");

        var composition = new Composition();
        // The ToString() method generates a class diagram in mermaid format
        string classDiagram = composition.ToString();
// }
        composition.SaveClassDiagram();
    }
}

// {
interface IDatabase;

class Database : IDatabase;

interface IUserRepository;

class UserRepository(IDatabase database) : IUserRepository;
// }