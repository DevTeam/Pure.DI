/*
$v=true
$p=5
$d=Tuple
$h=The tuples feature provides concise syntax to group multiple data elements in a lightweight data structure. The following example shows how a type can ask to inject a tuple argument into it:
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
        // This hint indicates to not generate methods such as Resolve
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