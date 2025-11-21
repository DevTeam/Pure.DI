/*
$v=true
$p=2
$d=Advanced interception
$h=This approach of interception maximizes performance by precompiling the proxy object factory.
$r=Shouldly;Castle.DynamicProxy
*/

// ReSharper disable ClassNeverInstantiated.Local
// ReSharper disable CheckNamespace
// ReSharper disable UnusedParameterInPartialMethod
// ReSharper disable ConvertIfStatementToReturnStatement
// ReSharper disable ArrangeTypeModifiers

// ReSharper disable UnusedMethodReturnValue.Global
namespace Pure.DI.UsageTests.Interception.AdvancedInterceptionScenario;

using System.Collections.Immutable;
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
        // This hint indicates to not generate methods such as Resolve
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

        // Взаимодействие с сервисами для проверки перехвата
        businessService.Process();
        businessService.DataService.Count();

        log.ShouldBe(
            ImmutableArray.Create(
                "Process returns Processed",
                "get_DataService returns Castle.Proxies.IDataServiceProxy",
                "Count returns 55"));
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
    private readonly List<string> _log = [];
    private static readonly IProxyBuilder ProxyBuilder = new DefaultProxyBuilder();
    private readonly IInterceptor[] _interceptors = [];

    public Composition(List<string> log)
        : this()
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