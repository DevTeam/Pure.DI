#### Advanced interception

Advanced interception is useful when cross-cutting behavior is required on a hot path and the proxy factory itself must not become the bottleneck. Instead of asking Castle DynamicProxy to discover the construction path repeatedly, this scenario compiles and caches a strongly typed proxy factory per service type.
The example wraps business services with a logging interceptor and caches the proxy creation delegate in a generic nested `ProxyFactory<T>`. The generated Pure.DI graph still creates the target services directly, while the interception hook applies the cached proxy layer after each dependency is built.


```c#
using Shouldly;
using Castle.DynamicProxy;
using System.Collections.Immutable;
using System.Linq.Expressions;
using System.Runtime.CompilerServices;
using Pure.DI;

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
```

<details>
<summary>Running this code sample locally</summary>

- Make sure you have the [.NET SDK 10.0](https://dotnet.microsoft.com/en-us/download/dotnet/10.0) or later installed
```bash
dotnet --list-sdk
```
- Create a net10.0 (or later) console application
```bash
dotnet new console -n Sample
```
- Add references to the NuGet packages
  - [Pure.DI](https://www.nuget.org/packages/Pure.DI)
  - [Shouldly](https://www.nuget.org/packages/Shouldly)
  - [Castle.DynamicProxy](https://www.nuget.org/packages/Castle.DynamicProxy)
```bash
dotnet add package Pure.DI
dotnet add package Shouldly
dotnet add package Castle.DynamicProxy
```
- Copy the example code into the _Program.cs_ file

You are ready to run the example 🚀
```bash
dotnet run
```

</details>

>[!NOTE]
>Advanced interception provides high-performance proxy generation for scenarios where runtime interception overhead must be minimized.
Use this only when decorators are not expressive enough or when an existing interception ecosystem is required. For simple cross-cutting behavior, decorators are easier to read and cheaper to reason about.

<details>
<summary>The following partial class will be generated</summary>

```c#
partial class Composition
{
  public IBusinessService BusinessService
  {
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    get
    {
      return OnDependencyInjection<IBusinessService>(new BusinessService(OnDependencyInjection<IDataService>(new DataService(), null, Lifetime.Transient)), null, Lifetime.Transient);
    }
  }


  private partial T OnDependencyInjection<T>(in T value, object? tag, Lifetime lifetime);
}
```

</details>

Class diagram:

```mermaid
---
 config:
  class:
   hideEmptyMembersBox: true
---
classDiagram
	DataService --|> IDataService
	BusinessService --|> IBusinessService
	Composition ..> BusinessService : IBusinessService BusinessService
	BusinessService *-- DataService : IDataService
	namespace Pure.DI.UsageTests.Interception.AdvancedInterceptionScenario {
		class BusinessService {
				<<class>>
			+BusinessService(IDataService dataService)
		}
		class Composition {
		<<partial>>
		+IBusinessService BusinessService
		}
		class DataService {
				<<class>>
			+DataService()
		}
		class IBusinessService {
			<<interface>>
		}
		class IDataService {
			<<interface>>
		}
	}
```

See also:

- [Interception](interception.md)

