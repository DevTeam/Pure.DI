﻿/*
$v=true
$p=1
$d=Composition root kinds
*/

// ReSharper disable ClassNeverInstantiated.Local
// ReSharper disable CheckNamespace
// ReSharper disable UnusedParameter.Local
// ReSharper disable ArrangeTypeModifiers
// ReSharper disable ClassNeverInstantiated.Global
// ReSharper disable UnusedVariable
// ReSharper disable UnusedMember.Local
// ReSharper disable ArrangeTypeMemberModifiers

namespace Pure.DI.UsageTests.Advanced.CompositionRootKindsScenario;

using Shouldly;
using Xunit;
using static RootKinds;

// {
//# using Pure.DI;
//# using static Pure.DI.RootKinds;
// }

public class Scenario
{
    [Fact]
    public void Run()
    {
// {
        var composition = new Composition();
        var service = composition.Root;
        var otherService = composition.GetOtherService();
        var dependency = Composition.Dependency;
// }
        service.ShouldBeOfType<Service>();
        composition.SaveClassDiagram();
    }
}

// {
interface IDependency;

class Dependency : IDependency;

interface IService;

class Service : IService
{
    public Service(IDependency dependency)
    {
    }
}

class OtherService : IService;

partial class Composition
{
    void Setup() =>
        DI.Setup(nameof(Composition))
            .Bind<IService>().To<Service>()
            .Bind<IService>("Other").To<OtherService>()
            .Bind<IDependency>().To<Dependency>()

            // Creates a public root method named "GetOtherService"
            .Root<IService>("GetOtherService", "Other", Public | Method)

            // Creates a private partial root method named "GetRoot"
            .Root<IService>("GetRoot", kind: Private | Partial | Method)

            // Creates a internal static root named "Dependency"
            .Root<IDependency>("Dependency", kind: Internal | Static);

    private partial IService GetRoot();

    public IService Root => GetRoot();
}
// }