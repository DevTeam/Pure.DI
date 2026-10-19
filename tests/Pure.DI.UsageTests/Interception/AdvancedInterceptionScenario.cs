/*
$v=true
$t=Interception
$t=HighPerformance
$p=2
$p=HighPerformance:14
$d=Advanced interception
$sa=Interception
$h=Advanced interception is useful when cross-cutting behavior is required on a hot path and the proxy factory itself must not become the bottleneck. Instead of asking Castle DynamicProxy to discover the construction path repeatedly, this scenario compiles and caches a strongly typed proxy factory per service type.
$h=The example wraps business services with a logging interceptor and caches the proxy creation delegate in a generic nested `ProxyFactory<T>`. The generated Pure.DI graph still creates the target services directly, while the interception hook applies the cached proxy layer after each dependency is built.
$f=>[!NOTE]
$f=>Advanced interception provides high-performance proxy generation for scenarios where runtime interception overhead must be minimized.
$f=
$f=Use this only when decorators are not expressive enough or when an existing interception ecosystem is required. For simple cross-cutting behavior, decorators are easier to read and cheaper to reason about.
$f=
$f=The realistic case is an application that must log every call on a latency-sensitive path, or that already depends on an interceptor library and cannot express the behavior as a decorator. Compiling the proxy constructor once per service type removes the repeated reflection that dominates the naive hook.
$f=>[!WARNING]
$f=>The proxy still sits between caller and target on every call, so a method that runs in nanoseconds stays dominated by the interception. Measure before adopting this, and exclude value-type dependencies - the hook already returns them untouched because they cannot be proxied.
$f=
$f=The `OnDependencyInjection` hook runs for every injected dependency, so proxy the contracts you care about with `OnDependencyInjectionContractTypeNameWildcard` instead of wrapping the whole graph. Prefer [Decorator](decorator.md) when you can wrap at compile time, and [Interception](interception.md) when the plain proxy generator is fast enough.
$r=Shouldly;Castle.DynamicProxy
*/

// ReSharper disable ClassNeverInstantiated.Local
// ReSharper disable CheckNamespace
// ReSharper disable UnusedParameterInPartialMethod
// ReSharper disable ConvertIfStatementToReturnStatement
// ReSharper disable ArrangeTypeModifiers
// ReSharper disable UnusedMethodReturnValue.Global
namespace Pure.DI.UsageTests.Interception.AdvancedInterceptionScenario;

using System.Linq.Expressions;
using System.Runtime.CompilerServices;
using Castle.DynamicProxy;
using Shouldly;
using Xunit;

// {
//# using System.Collections.Immutable;
//# using System.Linq.Expressions;
//# using System.Runtime.CompilerServices;
//# using Pure.DI;
// }

public class Scenario
{
    [Fact]
    public void Run()
    {
        // Disable Resolve methods to keep the public API minimal
        // Resolve = Off
// {
        // OnDependencyInjection = On
        DI.Setup(nameof(Composition))
            .Bind().To<DataService>()
            .Bind().To<BusinessService>()
            .Root<IBusinessService>("BusinessService");

        var log = new List<string>();
        var composition = new Composition(log);
        var businessService = composition.BusinessService;

        // Use the services to verify interception.
        businessService.Process();
        businessService.DataService.Count();

        log.ShouldBe(
            [
                "Process returns Processed",
                "get_DataService returns Castle.Proxies.IDataServiceProxy",
                "Count returns 55"
            ]);
// }
        composition.SaveClassDiagram();
    }
}

// {
public interface IDataService
{
    int Count();
}

class DataService : IDataService
{
    public int Count() => 55;
}

public interface IBusinessService
{
    IDataService DataService { get; }

    string Process();
}

class BusinessService(IDataService dataService) : IBusinessService
{
    public IDataService DataService { get; } = dataService;

    public string Process() => "Processed";
}

internal partial class Composition : IInterceptor
{
    private readonly List<string> _log;
    private static readonly IProxyBuilder ProxyBuilder = new DefaultProxyBuilder();
    private readonly IInterceptor[] _interceptors;

    public Composition(List<string> log)
    {
        _log = log;
        _interceptors = [this];
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
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
        invocation.Proceed();
        _log.Add($"{invocation.Method.Name} returns {invocation.ReturnValue}");
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
