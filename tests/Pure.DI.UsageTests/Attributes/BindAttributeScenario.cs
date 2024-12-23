/*
$v=true
$p=13
$d=Bind attribute
$h=`BindAttribute` allows you to perform automatic binding to properties, fields or methods that belong to the type of the binding involved.
$f=This attribute `BindAttribute` applies to field properties and methods, to regular, static, and even returning generalized types.
*/

// ReSharper disable ClassNeverInstantiated.Local
// ReSharper disable CheckNamespace
// ReSharper disable UnusedType.Global
// ReSharper disable ArrangeTypeModifiers

namespace Pure.DI.UsageTests.Basics.BindAttributeScenario;

using Xunit;

// {
//# using Pure.DI;
// }

public class Scenario
{
    [Fact]
    public void Run()
    {
        // Resolve = Off
// {
        DI.Setup(nameof(Composition))
            .Bind().As(Lifetime.Singleton).To<Facade>()
            .Bind().To<Service>()

            // Composition root
            .Root<IService>("Root");

        var composition = new Composition();
        var service = composition.Root;
        service.DoSomething();
// }
        composition.SaveClassDiagram();
    }
}

// {
interface IDependency
{
    public void DoSomething();
}

class Dependency : IDependency
{
    public void DoSomething()
    {
    }
}

class Facade
{
    [Bind]
    public IDependency Dependency { get; } = new Dependency();
}

interface IService
{
    public void DoSomething();
}

class Service(IDependency dep) : IService
{
    public void DoSomething() => dep.DoSomething();
}
// }