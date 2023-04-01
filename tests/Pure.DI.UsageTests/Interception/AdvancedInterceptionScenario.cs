/*
$v=true
$p=2
$d=Advanced interception
*/

// ReSharper disable ClassNeverInstantiated.Local
// ReSharper disable CheckNamespace
namespace Pure.DI.UsageTests.Interception.AdvancedInterceptionScenario;

using System.Collections.Immutable;
using System.Linq.Expressions;
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
    private static readonly IProxyBuilder ProxyBuilder = new DefaultProxyBuilder();
    private readonly IInterceptor[] _interceptors;

    public Composition(List<string> log)
        : this()
    {
        _log = log;
        _interceptors = new IInterceptor[]{ this };
    }

    private partial T OnDependencyInjection<T>(in T value, object? tag, object? lifetime) => 
        typeof(T).IsValueType 
            ? value :
            ProxyFactory<T>.GetFactory(ProxyBuilder)(value, _interceptors);

    public void Intercept(IInvocation invocation)
    {
        _log.Add(invocation.Method.Name);
        invocation.Proceed();
    }
    
    private static class ProxyFactory<T>
    {
        private static Func<T, IInterceptor[], T>? _factory;
        
        public static Func<T, IInterceptor[], T> GetFactory(IProxyBuilder proxyBuilder) => 
            _factory ?? CreateFactory(proxyBuilder);

        private static Func<T, IInterceptor[], T> CreateFactory(IProxyBuilder proxyBuilder)
        {
            // Compiles a delegate to create a proxy for the performance boost
            var proxyType = proxyBuilder.CreateInterfaceProxyTypeWithTargetInterface(typeof(T), Type.EmptyTypes, ProxyGenerationOptions.Default);
            var ctor = proxyType.GetConstructors().Single(i => i.GetParameters().Length == 2);
            var instance = Expression.Parameter(typeof(T));
            var interceptors = Expression.Parameter(typeof(IInterceptor[]));
            var newProxyExpression = Expression.New(ctor, interceptors, instance);
            return _factory = Expression.Lambda<Func<T, IInterceptor[], T>>(newProxyExpression, instance, interceptors).Compile();
        }
    }
}
// }

public class Scenario
{
    [Fact]
    public void Run()
    {
// {            
        // TrackInjections=true
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
    }
}