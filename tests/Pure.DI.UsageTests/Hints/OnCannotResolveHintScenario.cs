/*
$v=true
$p=3
$d=OnCannotResolve Hint
$h=The _OnCannotResolve_ hint determines whether to generate a partial `OnCannotResolve<T>(...)` method to handle a scenario where an instance which cannot be resolved.
*/

// ReSharper disable ClassNeverInstantiated.Local
// ReSharper disable CheckNamespace
// ReSharper disable UnusedParameterInPartialMethod
// ReSharper disable UnusedVariable
namespace Pure.DI.UsageTests.Hints.OnCannotResolveHintScenario;

using Shouldly;
using Xunit;

// {
public interface IDependency
{
}

public class Dependency : IDependency
{
    private readonly string _name;

    public Dependency(string name)
    {
        _name = name;
    }

    public override string ToString() => _name;
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
    private partial T OnCannotResolve<T>(object? tag, object lifetime)
    {
        if (typeof(T) == typeof(string))
        {
            return (T)(object)"Dependency with name";
        }

        throw new InvalidOperationException("Cannot resolve.");
    }
}
// }

public class Scenario
{
    [Fact]
    public void Run()
    {
        // ToString = On
// {            
        // OnCannotResolve = On
        // OnCannotResolveContractTypeNameRegularExpression = string
        DI.Setup("Composition")
            .Bind<IDependency>().To<Dependency>()
            .Bind<IService>().Tags().To<Service>()
            .Root<IService>("Root");

        var composition = new Composition();
        var service = composition.Root;
        service.Dependency.ToString().ShouldBe("Dependency with name");
        
// }
        TestTools.SaveClassDiagram(composition, nameof(OnCannotResolveHintScenario));
    }
}