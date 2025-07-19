#### Interception

Interception allows you to enrich or change the behavior of a certain set of objects from the object graph being created without changing the code of the corresponding types.


```c#
using Shouldly;
using Castle.DynamicProxy;
using System.Runtime.CompilerServices;
using Pure.DI;

// OnDependencyInjection = On
// OnDependencyInjectionContractTypeNameWildcard = *IService
DI.Setup(nameof(Composition))
    .Bind().To<Service>()
    .Root<IService>("Root");

var composition = new Composition();
var service = composition.Root;
service.GetMessage().ShouldBe("Hello World !!!");

public interface IService
{
    string GetMessage();
}

class Service : IService
{
    public string GetMessage() => "Hello World";
}

partial class Composition : IInterceptor
{
    private static readonly ProxyGenerator ProxyGenerator = new();

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

        return (T)ProxyGenerator.CreateInterfaceProxyWithTargetInterface(
            typeof(T),
            value,
            this);
    }

    public void Intercept(IInvocation invocation)
    {
        invocation.Proceed();
        if (invocation.Method.Name == nameof(IService.GetMessage)
            && invocation.ReturnValue is string message)
        {
            invocation.ReturnValue = $"{message} !!!";
        }
    }
}
```

<details>
<summary>Running this code sample locally</summary>

- Make sure you have the [.NET SDK 9.0](https://dotnet.microsoft.com/en-us/download/dotnet/9.0) or later is installed
```bash
dotnet --list-sdk
```
- Create a net9.0 (or later) console application
```bash
dotnet new console -n Sample
```
- Add references to NuGet packages
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

Using an intercept gives you the ability to add end-to-end functionality such as:

- Logging

- Action logging

- Performance monitoring

- Security

- Caching

- Error handling

- Providing resistance to failures, etc.

The following partial class will be generated:

```c#
partial class Composition
{
  [OrdinalAttribute(256)]
  public Composition()
  {
  }

  internal Composition(Composition parentScope)
  {
  }

  public IService Root
  {
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    get
    {
      return OnDependencyInjection<IService>(new Service(), null, Lifetime.Transient);
    }
  }


  private partial T OnDependencyInjection<T>(in T value, object? tag, Lifetime lifetime);
}
```

Class diagram:

```mermaid
---
 config:
  maxTextSize: 2147483647
  maxEdges: 2147483647
  class:
   hideEmptyMembersBox: true
---
classDiagram
	Service --|> IService
	Composition ..> Service : IService Root
	namespace Pure.DI.UsageTests.Interception.InterceptionScenario {
		class Composition {
		<<partial>>
		+IService Root
		}
		class IService {
			<<interface>>
		}
		class Service {
				<<class>>
			+Service()
		}
	}
```

