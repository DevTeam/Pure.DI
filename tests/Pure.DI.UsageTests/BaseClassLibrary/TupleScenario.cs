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
namespace Pure.DI.UsageTests.BCL.TupleScenario;

using Xunit;

// {
interface IDependency { }

class Dependency : IDependency { }

readonly record struct Point(int X, int Y);

interface IService
{
    IDependency Dependency { get; }
}

class Service : IService
{
    public Service((Point Point, IDependency Dependency) tuple) => 
        Dependency = tuple.Dependency;

    public IDependency Dependency { get; }
}
// }

public class Scenario
{
    [Fact]
    public void Run()
    {
        // ToString = On
        // FormatCode = On
// {            
        DI.Setup("Composition")
            .Bind<IDependency>().To<Dependency>()
            .Bind<Point>().To(_ => new Point(7, 9))
            .Bind<IService>().To<Service>().Root<IService>("Root");

        var composition = new Composition();
        var root = composition.Root;
// }            
        composition.SaveClassDiagram();
    }
}