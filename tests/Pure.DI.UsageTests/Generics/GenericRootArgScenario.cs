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
// ReSharper disable SuggestVarOrType_Elsewhere
// ReSharper disable RedundantTypeArgumentsOfMethod
// ReSharper disable UnusedMemberInSuper.Global
namespace Pure.DI.UsageTests.Generics.GenericRootArgScenario;

using Xunit;

// {
//# using Pure.DI;
// }

public class Scenario
{
    [Fact]
    public void Run()
    {
        // This hint indicates to not generate methods such as Resolve
        // Resolve = Off
// {
        DI.Setup(nameof(Composition))
            .RootArg<TT>("someArg")
            .Bind<IService<TT>>().To<Service<TT>>()

            // Composition root
            .Root<IService<TT>>("GetMyService");

        var composition = new Composition();
        IService<int> service = composition.GetMyService<int>(someArg: 33);
// }
        composition.SaveClassDiagram();
    }
}

// {
interface IService<out T>
{
    T? Dependency { get; }
}

class Service<T> : IService<T>
{
    // The Dependency attribute specifies to perform an injection,
    // the integer value in the argument specifies
    // the ordinal of injection
    [Dependency]
    public void SetDependency(T dependency) =>
        Dependency = dependency;

    public T? Dependency { get; private set; }
}
// }