/*
$v=true
$p=2
$d=Advanced interception
$h=This approach of interception maximizes performance by precompiling the proxy object factory.
*/

// ReSharper disable ClassNeverInstantiated.Local
// ReSharper disable CheckNamespace
// ReSharper disable UnusedParameterInPartialMethod
// ReSharper disable ConvertIfStatementToReturnStatement
// ReSharper disable ArrangeTypeModifiers
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

class Dependency : IDependency
{
    public void DependencyCall() { }
}

public interface IService
{
    IDependency Dependency { get; }

    void ServiceCall();
}

class Service(IDependency dependency) : IService
{
    public IDependency Dependency { get; } = dependency;

    public void ServiceCall() { }
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
        _interceptors = [this];
    }

    private partial T OnDependencyInjection<T>(
        in T value,
        object? tag,
        Lifetime lifetime)
    {
        if (typeof(T).IsValueType)
        {
            return value;
        }

        return ProxyFactory<T>.GetFactory(ProxyBuilder)(
            value,
            _interceptors);
    }

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
            var proxyType = proxyBuilder.CreateInterfaceProxyTypeWithTargetInterface(
                typeof(T),
                Type.EmptyTypes,
                ProxyGenerationOptions.Default);
            var ctor = proxyType.GetConstructors()
                .Single(i => i.GetParameters().Length == 2);
            var instance = Expression.Parameter(typeof(T));
            var interceptors = Expression.Parameter(typeof(IInterceptor[]));
            var newProxyExpression = Expression.New(ctor, interceptors, instance);
            return _factory = Expression.Lambda<Func<T, IInterceptor[], T>>(
                newProxyExpression,
                instance,
                interceptors)
                .Compile();
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
        // OnDependencyInjection = On
        DI.Setup(nameof(Composition))
            .Bind<IDependency>().To<Dependency>()
            .RootBind<IService>("Root").To<Service>();

        var log = new List<string>();
        var composition = new Composition(log);
        var service = composition.Root;
        service.ServiceCall();
        service.Dependency.DependencyCall();

        log.ShouldBe(
            ImmutableArray.Create(
                "ServiceCall",
                "get_Dependency",
                "DependencyCall"));
// }
        composition.SaveClassDiagram();
    }
}