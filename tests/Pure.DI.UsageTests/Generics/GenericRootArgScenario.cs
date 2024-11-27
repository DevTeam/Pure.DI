/*
$v=true
$p=8
$d=Generic root arguments
*/

// ReSharper disable ClassNeverInstantiated.Local
// ReSharper disable CheckNamespace
// ReSharper disable UnusedParameter.Local
// ReSharper disable ArrangeTypeModifiers

// ReSharper disable UnusedVariable
namespace Pure.DI.UsageTests.Basics.GenericRootArgScenario;

using Xunit;

// {
interface IService<out T>
{
    T? Dependency { get; }
}

class Service<T> : IService<T>
{
    // The Ordinal attribute specifies to perform an injection,
    // the integer value in the argument specifies
    // the ordinal of injection
    [Ordinal(0)]
    public void SetDependency(T dependency) =>
        Dependency = dependency;

    public T? Dependency { get; private set; }
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
            .RootArg<TT>("someArg")
            .Bind<IService<TT>>().To<Service<TT>>()

            // Composition root
            .Root<IService<TT>>("GetMyService");

        var composition = new Composition();
        var service = composition.GetMyService<int>(someArg: 33);
// }
        composition.SaveClassDiagram();
    }
}