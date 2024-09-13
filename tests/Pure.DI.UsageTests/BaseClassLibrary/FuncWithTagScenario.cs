/*
$v=true
$p=99
$d=Func with tag
*/

// ReSharper disable ClassNeverInstantiated.Local
// ReSharper disable CheckNamespace
// ReSharper disable ArrangeTypeModifiers

namespace Pure.DI.UsageTests.BCL.FuncWithTagScenario;

using System.Collections.Immutable;
using Shouldly;
using Xunit;

// {
interface IDependency;

class Dependency : IDependency;

interface IService
{
    ImmutableArray<IDependency> Dependencies { get; }
}

class Service([Tag("my tag")] Func<IDependency> dependencyFactory)
    : IService
{
    public ImmutableArray<IDependency> Dependencies { get; } =
    [
        ..Enumerable
            .Range(0, 10)
            .Select(_ => dependencyFactory())
    ];
}
// }

public class Scenario
{
    [Fact]
    public void Run()
    {
// {            
        DI.Setup(nameof(Composition))
            .Bind<IDependency>("my tag").To<Dependency>()
            .Bind<IService>().To<Service>()

            // Composition root
            .Root<IService>("Root");

        var composition = new Composition();
        var service = composition.Root;
        service.Dependencies.Length.ShouldBe(10);
// }            
        composition.SaveClassDiagram();
    }
}