/*
$v=true
$p=15
$d=Bind attribute for a generic type
*/

// ReSharper disable ClassNeverInstantiated.Local
// ReSharper disable CheckNamespace
// ReSharper disable UnusedType.Global
// ReSharper disable ArrangeTypeModifiers
// ReSharper disable MemberCanBeMadeStatic.Global
// ReSharper disable UnusedTypeParameter

#pragma warning disable CA1822
namespace Pure.DI.UsageTests.Basics.BindAttributeForGenericTypeScenario;

using Xunit;

// {
interface IDependency<T>
{
    public void DoSomething();
}

class Dependency<T> : IDependency<T>
{
    public void DoSomething()
    {
    }
}

class Facade
{
    [Bind(typeof(IDependency<TT>))]
    public IDependency<T> GetDependency<T>() => new Dependency<T>();
}

interface IService
{
    public void DoSomething();
}

class Service(IDependency<int> dep) : IService
{
    public void DoSomething() => dep.DoSomething();
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
#pragma warning restore CA1822