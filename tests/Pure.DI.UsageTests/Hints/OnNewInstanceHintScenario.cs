/*
$v=true
$p=4
$d=OnNewInstance hint
$h=Hints are used to fine-tune code generation. The _OnNewInstance_ hint determines whether to generate partial _OnNewInstance_ method.
$h=In addition, setup hints can be comments before the _Setup_ method in the form ```hint = value```, for example: `// OnNewInstance = On`.
$f=The `OnNewInstanceLifetimeRegularExpression` hint helps you define a set of lifetimes that require instance creation control. You can use it to specify a regular expression to filter bindings by lifetime name.
$f=For more hints, see [this](README.md#setup-hints) page.
*/

// ReSharper disable ClassNeverInstantiated.Local
// ReSharper disable CheckNamespace
// ReSharper disable UnusedParameterInPartialMethod
// ReSharper disable UnusedVariable
// ReSharper disable UnusedMemberInSuper.Global
// ReSharper disable ArrangeTypeModifiers
// ReSharper disable UnusedMember.Global

namespace Pure.DI.UsageTests.Hints.OnNewInstanceHintScenario;

using System.Collections.Immutable;
using Shouldly;
using Xunit;

// {
using static Hint;

interface IDependency;

class Dependency : IDependency
{
    public override string ToString() => nameof(Dependency);
}

interface IService
{
    IDependency Dependency { get; }
}

class Service(IDependency dependency) : IService
{
    public IDependency Dependency { get; } = dependency;

    public override string ToString() => nameof(Service);
}

internal partial class Composition
{
    private readonly List<string> _log = [];

    public Composition(List<string> log) : this() =>
        _log = log;

    partial void OnNewInstance<T>(
        ref T value,
        object? tag,
        Lifetime lifetime) =>
        _log.Add(typeof(T).Name);
}
// }

public class Scenario
{
    [Fact]
    public void Run()
    {
        // Resolve = Off
// {
        DI.Setup(nameof(Composition))
            .Hint(OnNewInstance, "On")
            .Bind().As(Lifetime.Singleton).To<Dependency>()
            .Bind().To<Service>()
            .Root<IService>("Root");

        var log = new List<string>();
        var composition = new Composition(log);
        var service1 = composition.Root;
        var service2 = composition.Root;

        log.ShouldBe([nameof(Dependency), nameof(Service), nameof(Service)]);
// }
        composition.SaveClassDiagram();
    }
}