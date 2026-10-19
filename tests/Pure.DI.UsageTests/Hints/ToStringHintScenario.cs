/*
$v=true
$p=8
$d=ToString hint
$h=Hints are used to fine-tune code generation. The `ToString` hint determines if the `ToString()` method should be generated. This method provides a text-based class diagram in the format [_mermaid_](https://mermaid.js.org/). To see this diagram, just call the `ToString` method and copy the text to [this site](https://mermaid.live/). An example class diagram can be seen below.
$h=It answers the question a new team member asks first - what does this composition actually build - without reading the setup or the generated code.
$h=In addition, setup hints can be comments before the `Setup` method in the form `hint = value`, for example: `// ToString = On`.
$f=Developers who start using DI technology often complain that they stop seeing the structure of the application because it is difficult to understand how it is built. To make life easier, you can add the `ToString` hint by telling the generator to create a `ToString()` method.
$f=The diagram is produced from the same graph that generates the code, so it shows the bindings, tags and lifetimes that are really in effect rather than the ones you think you configured.
$f=The generated method adds code to the composition, so switch the hint on in compositions you study and off in ones you ship, unless the diagram is a deliberate diagnostic feature.
$f=Prefer this over reverse-engineering generated sources when reviewing a graph; for a written explanation of lifetimes and disposal, read the generated members themselves.
$f=For more hints, see [this](../README.md#setup-hints) page.
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
        // Disable Resolve methods to keep the public API minimal
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