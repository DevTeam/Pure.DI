/*
$v=true
$p=14
$d=Generic injections as required with arguments
$r=Shouldly
*/

// ReSharper disable ClassNeverInstantiated.Local
// ReSharper disable CheckNamespace
// ReSharper disable ArrangeTypeModifiers

namespace Pure.DI.UsageTests.Generics.GenericInjectionsAsRequiredWithArgumentsScenario;

using Shouldly;
using Xunit;

// {
//# using Pure.DI;
//# using System.Collections.Generic;
// }

public class Scenario
{
    [Fact]
    public void Run()
    {
        // Resolve=Off
// {
        DI.Setup(nameof(Composition))
            .Bind().To<Dependency<TT>>()
            .Bind().To<Service<TT>>()

            // Composition root
            .Root<IService<string>>("Root");

        var composition = new Composition();
        var service = composition.Root;
        var dependencies = service.Dependencies;
        dependencies.Count.ShouldBe(2);
        dependencies[0].Id.ShouldBe(33);
        dependencies[1].Id.ShouldBe(99);
// }
        composition.SaveClassDiagram();
    }
}

// {
interface IDependency<out T>
{
    int Id { get; }
}

class Dependency<T>(int id) : IDependency<T>
{
    public int Id { get; } = id;
}

interface IService<out T>
{
    IReadOnlyList<IDependency<T>> Dependencies { get; }
}

class Service<T>(Func<int, IDependency<T>> dependencyFactoryWithArgs): IService<T>
{
    public IReadOnlyList<IDependency<T>> Dependencies { get; } =
    [
        dependencyFactoryWithArgs(33),
        dependencyFactoryWithArgs(99)
    ];
}
// }