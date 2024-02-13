/*
$v=true
$p=6
$d=Composition root kinds
*/

// ReSharper disable ClassNeverInstantiated.Local
// ReSharper disable CheckNamespace
// ReSharper disable UnusedParameter.Local
// ReSharper disable ArrangeTypeModifiers
// ReSharper disable ClassNeverInstantiated.Global
// ReSharper disable UnusedVariable
// ReSharper disable UnusedMember.Local
namespace Pure.DI.UsageTests.Basics.CompositionRootKindsScenario;

using Shouldly;
using Xunit;

// {
interface IDependency;

class Dependency : IDependency;

interface IService;

class Service : IService
{
    public Service(IDependency dependency) { }
}

class OtherService : IService;

partial class Composition
{
    private static void Setup() =>
        DI.Setup(nameof(Composition))
            .Bind<IService>().To<Service>()
                // Creates a private partial root method named "GetRoot"
                .Root<IService>("GetRoot", kind: RootKinds.Private | RootKinds.Partial | RootKinds.Method)
            .Bind<IService>("Other").To<OtherService>()
                // Creates a public root method named "GetOtherService"
                .Root<IService>("GetOtherService", "Other", RootKinds.Public | RootKinds.Method)
            .Bind<IDependency>().To<Dependency>()
                // Creates a internal static root named "Dependency"
                .Root<IDependency>("Dependency", kind: RootKinds.Internal | RootKinds.Static);
    
    private partial IService GetRoot();

    public IService Root => GetRoot();
}
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