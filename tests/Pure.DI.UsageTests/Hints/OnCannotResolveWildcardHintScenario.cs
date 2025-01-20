/*
$v=true
$p=3
$d=OnCannotResolve wildcard hint
$h=Hints are used to fine-tune code generation. The _OnCannotResolve_ hint determines whether to generate a partial `OnCannotResolve<T>(...)` method to handle a scenario where an instance which cannot be resolved.
$h=In addition, setup hints can be comments before the _Setup_ method in the form ```hint = value```, for example: `// OnCannotResolveContractTypeNameWildcard = string`.
$f=The `OnCannotResolveContractTypeNameWildcard` hint helps define the set of types that require manual dependency resolution. You can use it to specify a wildcard to filter the full type name.
$f=For more hints, see [this](README.md#setup-hints) page.
$r=Shouldly
*/

// ReSharper disable ClassNeverInstantiated.Local
// ReSharper disable CheckNamespace
// ReSharper disable UnusedParameterInPartialMethod
// ReSharper disable UnusedVariable
// ReSharper disable ArrangeTypeModifiers

namespace Pure.DI.UsageTests.Hints.OnCannotResolveWildcardHintScenario;
#pragma warning disable CA1822

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
        // Resolve = Off
// {
        // OnCannotResolveContractTypeNameWildcard = string
        DI.Setup(nameof(Composition))
            .Hint(OnCannotResolve, "On")
            .Bind().To<Dependency>()
            .Bind().To<Service>()
            .Root<IService>("Root");

        var composition = new Composition();
        var service = composition.Root;
        service.Dependency.ToString().ShouldBe("My name");

// }
        composition.SaveClassDiagram();
    }
}

// {
interface IDependency;

class Dependency(string name) : IDependency
{
    public override string ToString() => name;
}

interface IService
{
    IDependency Dependency { get; }
}

class Service(IDependency dependency) : IService
{
    public IDependency Dependency { get; } = dependency;
}

partial class Composition
{
    private partial T OnCannotResolve<T>(
        object? tag,
        Lifetime lifetime)
    {
        if (typeof(T) == typeof(string))
        {
            return (T)(object)"My name";
        }

        throw new InvalidOperationException("Cannot resolve.");
    }
}
// }