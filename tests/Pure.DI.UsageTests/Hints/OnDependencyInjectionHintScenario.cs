/*
$v=true
$p=2
$d=OnDependencyInjection hint
$h=Hints are used to fine-tune code generation. The _OnDependencyInjection_ hint determines whether to generate partial _OnDependencyInjection_ method to control of dependency injection.
$h=In addition, setup hints can be comments before the _Setup_ method in the form ```hint = value```, for example: `// OnDependencyInjection = On`.
$f=The `OnDependencyInjectionContractTypeNameRegularExpression` hint helps identify the set of types that require injection control. You can use it to specify a regular expression to filter the full name of a type.
$f=For more hints, see [this](https://github.com/DevTeam/Pure.DI/blob/master/README.md#setup-hints) page.
*/

// ReSharper disable ClassNeverInstantiated.Local
// ReSharper disable CheckNamespace
// ReSharper disable UnusedParameterInPartialMethod
// ReSharper disable UnusedVariable
// ReSharper disable UnusedMemberInSuper.Global
namespace Pure.DI.UsageTests.Hints.OnDependencyInjectionHintScenario;

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
}

internal partial class Composition
{
    private readonly List<string> _log;

    public Composition(List<string> log)
        : this()
    {
        _log = log;
    }

    private partial T OnDependencyInjection<T>(in T value, object? tag, Lifetime lifetime)
    {
        _log.Add($"{value?.GetType().Name} injected");
        return value;
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
        // OnDependencyInjection = On
        DI.Setup("Composition")
            .Hint(OnDependencyInjectionContractTypeNameRegularExpression, nameof(IDependency))
            .Bind<IDependency>().To<Dependency>()
            .Bind<IService>().Tags().To<Service>().Root<IService>("Root");

        var log = new List<string>();
        var composition = new Composition(log);
        var service = composition.Root;
        
        log.ShouldBe(ImmutableArray.Create("Dependency injected"));
// }
        TestTools.SaveClassDiagram(composition, nameof(OnDependencyInjectionHintScenario));
    }
}