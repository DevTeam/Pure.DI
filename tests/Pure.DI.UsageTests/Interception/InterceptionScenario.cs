/*
$v=true
$p=1
$d=Interception
*/

// ReSharper disable ClassNeverInstantiated.Local
// ReSharper disable CheckNamespace
// ReSharper disable UnusedParameterInPartialMethod
namespace Pure.DI.UsageTests.Interception.InterceptionScenario;

using System.Collections.Immutable;
using Castle.DynamicProxy;
using Shouldly;
using Xunit;

// {
public interface IDependency
{
    void DependencyCall();
}

public class Dependency : IDependency
{
    public void DependencyCall()
    {
    }
}

public interface IService
{
    IDependency Dependency { get; }

    void ServiceCall();
}

public class Service : IService
{
    public Service(IDependency dependency)
    {
        Dependency = dependency;
    }

    public IDependency Dependency { get; }

    public void ServiceCall()
    {
    }
}

internal partial class Composition: IInterceptor
{
    private readonly List<string> _log;
    private static readonly ProxyGenerator ProxyGenerator = new();

    public Composition(List<string> log)
        : this()
    {
        _log = log;
    }

    private partial T OnDependencyInjection<T>(in T value, object? tag, object? lifetime) => 
        typeof(T).IsValueType
            ? value
            : (T)ProxyGenerator.CreateInterfaceProxyWithTargetInterface(typeof(T), value, this);

    public void Intercept(IInvocation invocation)
    {
        _log.Add(invocation.Method.Name);
        invocation.Proceed();
    }
}
// }

public class Scenario
{
    [Fact]
    public void Run()
    {
        // ToString = On
// {            
        // OnDependencyInjection = On
        DI.Setup("Composition")
            .Bind<IDependency>().To<Dependency>()
            .Bind<IService>().Tags().To<Service>()
            .Root<IService>("Root");

        var log = new List<string>();
        var composition = new Composition(log);
        var service = composition.Root;
        service.ServiceCall();
        service.Dependency.DependencyCall();
        log.ShouldBe(ImmutableArray.Create("ServiceCall", "get_Dependency", "DependencyCall"));
// }
        TestTools.SaveClassDiagram(composition, nameof(InterceptionScenario));
    }
}