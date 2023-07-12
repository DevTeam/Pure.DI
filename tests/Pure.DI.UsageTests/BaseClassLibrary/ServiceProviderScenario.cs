/*
$v=true
$p=6
$d=Service provider
$h=The `// ObjectResolveMethodName = GetService` hint overrides the _object Resolve(Type type)_ method name in _GetService_, allowing the _IServiceProvider_ interface to be implemented in a partial class.
*/

// ReSharper disable ClassNeverInstantiated.Local
// ReSharper disable CheckNamespace
// ReSharper disable UnusedParameter.Local
// ReSharper disable ArrangeTypeModifiers
namespace Pure.DI.UsageTests.BCL.ServiceProviderScenario;

using Shouldly;
using Xunit;

// {
interface IDependency { }

class Dependency : IDependency { }

interface IService
{
    IDependency Dependency { get; }
}

class Service : IService
{
    public Service(IDependency dependency) => 
        Dependency = dependency;

    public IDependency Dependency { get; }
}

partial class ServiceProvider: IServiceProvider
{
    private void Setup() =>
// }        
        // ToString = On
        // FormatCode = On
// {
        // The following hint overrides the name of the
        // "object Resolve(Type type)" method in "GetService",
        // which implements the "IServiceProvider" interface:

        // ObjectResolveMethodName = GetService
        DI.Setup(nameof(ServiceProvider))
            .Bind<IDependency>().As(Lifetime.Singleton).To<Dependency>()
            .Bind<IService>().To<Service>()
            .Root<IDependency>()
            .Root<IService>();
}
// }

public class Scenario
{
    [Fact]
    public void Run()
    {
// {            
        var serviceProvider = new ServiceProvider();
        var service = (IService)serviceProvider.GetService(typeof(IService));
        var dependency = serviceProvider.GetService(typeof(IDependency));
        service.Dependency.ShouldBe(dependency);
// }            
        service.ShouldBeOfType<Service>();
        TestTools.SaveClassDiagram(serviceProvider, nameof(ServiceProviderScenario));
    }
}