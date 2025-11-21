/*
$v=true
$p=9
$d=Field injection
$h=To use dependency injection for a field, make sure the field is writable and simply add the _Ordinal_ attribute to that field, specifying an ordinal that will be used to determine the injection order:
$f=The key points are:
$f=- The field must be writable
$f=- The `Dependency` (or `Ordinal`) attribute is used to mark the field for injection
$f=- The container automatically injects the dependency when resolving the object graph
$r=Shouldly
*/

// ReSharper disable ClassNeverInstantiated.Local
// ReSharper disable CheckNamespace
// ReSharper disable UnusedParameter.Local
// ReSharper disable ArrangeTypeModifiers

// ReSharper disable ArrangeAccessorOwnerBody
namespace Pure.DI.UsageTests.Basics.FieldInjectionScenario;

using Shouldly;
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
            .Bind<ICoffeeMachine>().To<CoffeeMachine>()
            .Bind<ISmartKitchen>().To<SmartKitchen>()

            // Composition root
            .Root<ISmartKitchen>("Kitchen");

        var composition = new Composition();
        var kitchen = composition.Kitchen;
        kitchen.CoffeeMachine.ShouldBeOfType<CoffeeMachine>();
        // }
        composition.SaveClassDiagram();
    }
}

// {
interface ICoffeeMachine;

class CoffeeMachine : ICoffeeMachine;

interface ISmartKitchen
{
    ICoffeeMachine? CoffeeMachine { get; }
}

class SmartKitchen : ISmartKitchen
{
    // The Dependency attribute specifies to perform an injection.
    // The container will automatically assign a value to this field
    // when creating the SmartKitchen instance.
    [Dependency]
    public ICoffeeMachine? CoffeeMachineImpl;

    // Expose the injected dependency through a public property
    public ICoffeeMachine? CoffeeMachine => CoffeeMachineImpl;
}
// }