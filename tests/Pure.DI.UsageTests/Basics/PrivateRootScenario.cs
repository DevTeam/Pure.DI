/*
$v=true
$p=12
$d=Private root
$h=Useful when hiding the roots of a composition.
*/

// ReSharper disable CheckNamespace
// ReSharper disable UnusedParameter.Local
// ReSharper disable UnusedMember.Local
// ReSharper disable ClassNeverInstantiated.Global
// ReSharper disable ArrangeTypeModifiers
namespace Pure.DI.UsageTests.Basics.PrivateRootScenario;

using System.Diagnostics;
using Xunit;

// {
internal interface IDependency { }

internal class Dependency : IDependency { }

internal interface IService
{
    void DoSomething();
}

internal class Service : IService
{
    public Service(IDependency dependency) { }

    public void DoSomething() { }
}

// The partial class is also useful for specifying access modifiers to the generated class
public partial class Api
{
    // This method will not be called in runtime
    [Conditional("DI")]
    private void Setup() =>
        DI.Setup(nameof(Api))
            .Bind<IDependency>().To<Dependency>()
            .Bind<IService>().To<Service>().Root<IService>("Service", default, RootKinds.Private);

    public void DoSomething() => Service.DoSomething();
}
// }

public class Scenario
{
    [Fact]
    public void Run()
    {
// {            
        var api = new Api();
        api.DoSomething();
// }            
        api.SaveClassDiagram();
    }
}