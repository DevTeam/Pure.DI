﻿/*
$v=true
$p=7
$d=Async Enumerable
$h=Specifying `IAsyncEnumerable<T>` as the injection type allows instances of all bindings implementing type `T` to be injected in an asynchronous-lazy manner - the instances will be provided one at a time, in an order corresponding to the sequence of the bindings.
$r=Shouldly
*/

// ReSharper disable ClassNeverInstantiated.Local
// ReSharper disable CheckNamespace
// ReSharper disable ArrangeTypeModifiers

namespace Pure.DI.UsageTests.BCL.AsyncEnumerableScenario;

using Shouldly;
using Xunit;

// {
//# using Pure.DI;
// }

public class Scenario
{
    [Fact]
    public async Task Run()
    {
        // This hint indicates to not generate methods such as Resolve
        // Resolve = Off
// {
        DI.Setup(nameof(Composition))
            .Bind<IDependency>().To<AbcDependency>()
            .Bind<IDependency>(2).To<XyzDependency>()
            .Bind<IService>().To<Service>()

            // Composition root
            .Root<IService>("Root");

        var composition = new Composition();
        var service = composition.Root;
        var dependencies = await service.GetDependenciesAsync();
        dependencies[0].ShouldBeOfType<AbcDependency>();
        dependencies[1].ShouldBeOfType<XyzDependency>();
// }
        composition.SaveClassDiagram();
    }
}

// {
interface IDependency;

class AbcDependency : IDependency;

class XyzDependency : IDependency;

interface IService
{
    Task<IReadOnlyList<IDependency>> GetDependenciesAsync();
}

class Service(IAsyncEnumerable<IDependency> dependencies) : IService
{
    public async Task<IReadOnlyList<IDependency>> GetDependenciesAsync()
    {
        var deps = new List<IDependency>();
        await foreach (var dependency in dependencies)
        {
            deps.Add(dependency);
        }

        return deps;
    }
}
// }