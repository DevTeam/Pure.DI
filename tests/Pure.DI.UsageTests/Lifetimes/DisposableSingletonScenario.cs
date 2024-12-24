﻿/*
$v=true
$p=7
$d=Disposable singleton
$h=To dispose all created singleton instances, simply dispose the composition instance:
$f=A composition class becomes disposable if it creates at least one disposable singleton instance.
*/

// ReSharper disable ClassNeverInstantiated.Local
// ReSharper disable CheckNamespace
// ReSharper disable ArrangeTypeModifiers

namespace Pure.DI.UsageTests.Lifetimes.DisposableSingletonScenario;

using Xunit;
using static Lifetime;

// {
//# using Pure.DI;
//# using Shouldly;
//# using static Pure.DI.Lifetime;
// }

public class Scenario
{
    [Fact]
    public void Run()
    {
// {
        DI.Setup(nameof(Composition))
            // This hint indicates to not generate methods such as Resolve
            .Hint(Hint.Resolve, "Off")
            .Bind().As(Singleton).To<Dependency>()
            .Bind().To<Service>()
            .Root<IService>("Root");

        IDependency dependency;
        using (var composition = new Composition())
        {
            var service = composition.Root;
            dependency = service.Dependency;
        }

        dependency.IsDisposed.ShouldBeTrue();
// }
        new Composition().SaveClassDiagram();
    }
}

// {
interface IDependency
{
    bool IsDisposed { get; }
}

class Dependency : IDependency, IDisposable
{
    public bool IsDisposed { get; private set; }

    public void Dispose() => IsDisposed = true;
}

interface IService
{
    public IDependency Dependency { get; }
}

class Service(IDependency dependency) : IService
{
    public IDependency Dependency { get; } = dependency;
}
// }