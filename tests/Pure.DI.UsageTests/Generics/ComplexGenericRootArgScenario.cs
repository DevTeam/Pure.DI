/*
$v=true
$p=9
$d=Complex generic root arguments
*/

// ReSharper disable ClassNeverInstantiated.Local
// ReSharper disable CheckNamespace
// ReSharper disable UnusedParameter.Local
// ReSharper disable ArrangeTypeModifiers

// ReSharper disable UnusedVariable
// ReSharper disable SuggestVarOrType_Elsewhere
// ReSharper disable RedundantTypeArgumentsOfMethod
// ReSharper disable UnusedMemberInSuper.Global
// ReSharper disable NotAccessedPositionalProperty.Global
namespace Pure.DI.UsageTests.Generics.ComplexGenericRootArgScenario;

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
            .RootArg<MyData<TT>>("complexArg")
            .Bind<IService<TT2>>().To<Service<TT2>>()

            // Composition root
            .Root<IService<TT3>>("GetMyService");

        var composition = new Composition();
        IService<int> service = composition.GetMyService<int>(
            new MyData<int>(33, "Just contains an integer value 33"));
// }
        composition.SaveClassDiagram();
    }
}

// {
record MyData<T>(T Value, string Description); 
    
interface IService<out T>
{
    T? Val { get; }
}

class Service<T> : IService<T>
{
    // The Dependency attribute specifies to perform an injection,
    // the integer value in the argument specifies
    // the ordinal of injection
    [Dependency]
    public void SetDependency(MyData<T> data) =>
        Val = data.Value;

    public T? Val { get; private set; }
}
// }