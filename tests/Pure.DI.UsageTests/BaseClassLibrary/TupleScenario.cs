﻿/*
$v=true
$p=5
$d=Tuple 
*/

// ReSharper disable ClassNeverInstantiated.Local
// ReSharper disable CheckNamespace
// ReSharper disable UnusedVariable
namespace Pure.DI.UsageTests.BCL.TupleScenario;

using Xunit;

// {
internal interface IDependency { }

internal class Dependency : IDependency { }

internal readonly record struct Point(int X, int Y);

internal interface IService
{
    IDependency Dependency { get; }
}

internal class Service : IService
{
    public Service((Point Point, IDependency Dependency) tuple)
    {
        Dependency = tuple.Dependency;
    }

    public IDependency Dependency { get; }
}
// }

public class Scenario
{
    [Fact]
    public void Run()
    {
        // ToString = On
// {            
        DI.Setup("Composition")
            .Bind<IDependency>().To<Dependency>()
            .Bind<Point>().To(_ => new Point(7, 9))
            .Bind<IService>().To<Service>()
            .Root<IService>("Root");

        var composition = new Composition();
        var root = composition.Root;
// }            
        TestTools.SaveClassDiagram(new Composition(), nameof(TupleScenario));
    }
}