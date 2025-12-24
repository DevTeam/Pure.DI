/*
$v=true
$p=2
$d=OnDependencyInjection wildcard hint
$h=Hints are used to fine-tune code generation. The _OnDependencyInjection_ hint determines whether to generate partial _OnDependencyInjection_ method to control of dependency injection.
$h=In addition, setup hints can be comments before the _Setup_ method in the form ```hint = value```, for example: `// OnDependencyInjection = On`.
$f=The `OnDependencyInjectionContractTypeNameWildcard` hint helps identify the set of types that require injection control. You can use it to specify a wildcard to filter the full name of a type.
$f=For more hints, see [this](README.md#setup-hints) page.
$r=Shouldly
*/

// ReSharper disable ClassNeverInstantiated.Local
// ReSharper disable CheckNamespace
// ReSharper disable UnusedParameterInPartialMethod
// ReSharper disable UnusedVariable
// ReSharper disable UnusedMemberInSuper.Global
// ReSharper disable ArrangeTypeModifiers
// ReSharper disable UnusedMember.Global

// ReSharper disable NotAccessedPositionalProperty.Global
namespace Pure.DI.UsageTests.Hints.OnDependencyInjectionWildcardHintScenario;

using Shouldly;
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
        // OnDependencyInjection = On
        DI.Setup(nameof(Composition))
            .Hint(OnDependencyInjectionContractTypeNameWildcard, "*IUserRepository")
            .Hint(OnDependencyInjectionContractTypeNameWildcard, "*IUserService")
            .RootArg<int>("id")
            .Bind().To<UserRepository>()
            .Bind().To<UserService>()
            .Root<IUserService>("GetUserService");

        var log = new List<string>();
        var composition = new Composition(log);
        var service = composition.GetUserService(33);

        log.ShouldBe([
            "UserRepository injected",
            "UserService injected"
        ]);
// }
        composition.SaveClassDiagram();
    }
}

// {
interface IUserRepository;

record UserRepository(int Id) : IUserRepository;

interface IUserService
{
    IUserRepository Repository { get; }
}

class UserService(IUserRepository repository) : IUserService
{
    public IUserRepository Repository { get; } = repository;
}

partial class Composition(List<string> log)
{
    private partial T OnDependencyInjection<T>(
        in T value,
        object? tag,
        Lifetime lifetime)
    {
        log.Add($"{value?.GetType().Name} injected");
        return value;
    }
}
// }