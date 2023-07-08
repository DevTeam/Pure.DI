/*
$v=true
$p=4
$d=OnInstanceCreation hint
$h=Hints are used to fine-tune code generation. The _OnInstanceCreation_ hint determines whether to generate partial _OnInstanceCreation_ method.
$h=In addition, setup hints can be comments before the _Setup_ method in the form ```hint = value```, for example: `// OnInstanceCreation = On`.
$f=The `OnInstanceCreationLifetimeRegularExpression` hint helps you define a set of lifetimes that require instance creation control. You can use it to specify a regular expression to filter bindings by lifetime name.
$f=For more hints, see [this](https://github.com/DevTeam/Pure.DI/blob/master/README.md#setup-hints) page.
*/

// ReSharper disable ClassNeverInstantiated.Local
// ReSharper disable CheckNamespace
// ReSharper disable UnusedParameterInPartialMethod
// ReSharper disable UnusedVariable
// ReSharper disable UnusedMemberInSuper.Global
namespace Pure.DI.UsageTests.Hints.OnInstanceCreationHintScenario;

using System.Collections.Immutable;
using Shouldly;
using Xunit;

// {
using static Hint;

public interface IDependency
{
}

public class Dependency : IDependency
{
    public override string ToString() => "Dependency";
}

public interface IService
{
    IDependency Dependency { get; }
}

public class Service : IService
{
    public Service(IDependency dependency)
    {
        Dependency = dependency;
    }

    public IDependency Dependency { get; }
    
    public override string ToString() => "Service";
}

internal partial class Composition
{
    private readonly List<string> _log;

    public Composition(List<string> log)
        : this()
    {
        _log = log;
    }

    partial void OnInstanceCreation<T>(ref T value, object? tag, Lifetime lifetime)
    {
        _log.Add(typeof(T).Name);
    }
}
// }

public class Scenario
{
    [Fact]
    public void Run()
    {
        // ToString = On
        // FormatCode = On
// {
        DI.Setup("Composition")
            .Hint(OnInstanceCreation, "On")
            .Hint(OnInstanceCreationLifetimeRegularExpression, nameof(Lifetime.Transient))
            .Bind<IDependency>().To<Dependency>()
            .Bind<IService>().Tags().To<Service>().Root<IService>("Root");

        var log = new List<string>();
        var composition = new Composition(log);
        var service = composition.Root;
        
        log.ShouldBe(ImmutableArray.Create("Dependency", "Service"));
// }
        TestTools.SaveClassDiagram(composition, nameof(OnInstanceCreationHintScenario));
    }
}