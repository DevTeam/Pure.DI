/*
$v=true
$p=1
$d=Generics
$h=Generic types are also supported, this is easy to do by binding generic types and specifying generic markers like `TT`, `TT1` etc. as generic type parameters:
$f=Actually, the property _Root_ looks like:
$f=```c#
$f=public IService Root
$f={
$f=  get
$f=  {
$f=    return new Service(new Dependency<int>(), new Dependency<string>());
$f=  }
$f=}
$f=``` 
*/

// ReSharper disable ClassNeverInstantiated.Local
// ReSharper disable CheckNamespace
// ReSharper disable UnusedTypeParameter
// ReSharper disable ArrangeTypeModifiers
namespace Pure.DI.UsageTests.Generics.GenericsScenario;

using Shouldly;
using Xunit;

// {
interface IDependency<T>;

class Dependency<T> : IDependency<T>;

interface IService
{
    IDependency<int> IntDependency { get; }
    
    IDependency<string> StringDependency { get; }
}

class Service(
    IDependency<int> intDependency,
    IDependency<string> stringDependency)
    : IService
{
    public IDependency<int> IntDependency { get; } = intDependency;

    public IDependency<string> StringDependency { get; } = stringDependency;
}
// }

public class Scenario
{
    [Fact]
    public void Run()
    {
// {            
        DI.Setup(nameof(Composition))
            // This hint indicates to not generate methods such as Resolve
            .Hint(Hint.Resolve, "Off")
            .Bind<IDependency<TT>>().To<Dependency<TT>>()
            .Bind<IService>().To<Service>()
            
            // Composition root
            .Root<IService>("Root");

        var composition = new Composition();
        var service = composition.Root;
        service.IntDependency.ShouldBeOfType<Dependency<int>>();
        service.StringDependency.ShouldBeOfType<Dependency<string>>();
// }
        composition.SaveClassDiagram();
    }
}