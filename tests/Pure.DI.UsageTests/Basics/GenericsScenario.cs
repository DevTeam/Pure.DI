/*
$v=true
$p=4
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
namespace Pure.DI.UsageTests.Basics.GenericsScenario;

using Shouldly;
using Xunit;

// {
internal interface IDependency<T> { }

internal class Dependency<T> : IDependency<T> { }

internal interface IService
{
    IDependency<int> IntDependency { get; }
    
    IDependency<string> StringDependency { get; }
}

internal class Service : IService
{
    public Service(IDependency<int> intDependency, IDependency<string> stringDependency)
    {
        IntDependency = intDependency;
        StringDependency = stringDependency;
    }
    
    public IDependency<int> IntDependency { get; }
    
    public IDependency<string> StringDependency { get; }
}
// }

public class Scenario
{
    [Fact]
    public void Run()
    {
        // ToString = On
        // FormatCode = Off
// {            
        DI.Setup("Composition")
            .Bind<IDependency<TT>>().To<Dependency<TT>>()
            .Bind<IService>().To<Service>().Root<IService>("Root");

        var composition = new Composition();
        var service = composition.Root;
        service.IntDependency.ShouldBeOfType<Dependency<int>>();
        service.StringDependency.ShouldBeOfType<Dependency<string>>();
// }
        TestTools.SaveClassDiagram(composition, nameof(GenericsScenario));
    }
}