/*
$v=true
$p=13
$d=Tuple
$h=The tuples feature provides concise syntax to group multiple data elements in a lightweight data structure. The following example shows how a type can ask to inject a tuple argument into it: each element is resolved as its own contract, so `Coordinates` and `IEngine` are injected into one constructor parameter.
$f=>[!NOTE]
$f=>Tuples are useful for returning multiple values from a method or grouping related dependencies without creating explicit types.
$f=
$f=The elements are resolved independently by type, which means a tuple is not a contract: you cannot bind a tuple as a whole or tag it per element. Use it to keep a constructor short when several values belong together semantically, such as a start position and a motor.
$f=Because a tuple carries no name, two dependencies of the same type inside one tuple would both resolve to the same binding. Prefer an explicit type or tags once the grouping needs identity.
$f=>[!TIP]
$f=>Prefer a small record when the group is used in more than one place or needs validation; a tuple is best for a single constructor parameter.
$r=Shouldly
*/

// ReSharper disable ClassNeverInstantiated.Local
// ReSharper disable CheckNamespace
// ReSharper disable UnusedVariable
// ReSharper disable NotAccessedPositionalProperty.Global
// ReSharper disable UnusedMemberInSuper.Global
// ReSharper disable ArrangeTypeModifiers
// ReSharper disable UnusedMember.Global

namespace Pure.DI.UsageTests.BCL.TupleScenario;

using Xunit;

// {
//# using Pure.DI;
// }

public class Scenario
{
    [Fact]
    public void Run()
    {
        // Disable Resolve methods to keep the public API minimal
        // Resolve = Off
        // {
        DI.Setup(nameof(Composition))
            .Bind<IEngine>().To<ElectricEngine>()
            .Bind<Coordinates>().To(() => new Coordinates(10, 20))
            .Bind<IVehicle>().To<Car>()

            // Composition root
            .Root<IVehicle>("Vehicle");

        var composition = new Composition();
        var vehicle = composition.Vehicle;
        // }
        composition.SaveClassDiagram();
    }
}

// {
interface IEngine;

class ElectricEngine : IEngine;

readonly record struct Coordinates(int X, int Y);

interface IVehicle
{
    IEngine Engine { get; }
}

class Car((Coordinates StartPosition, IEngine Engine) specs) : IVehicle
{
    // The tuple 'specs' groups the initialization data (like coordinates)
    // and dependencies (like engine) into a single lightweight argument.
    public IEngine Engine { get; } = specs.Engine;
}
// }