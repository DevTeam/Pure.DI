#### Async Enumerable

Specifying `IAsyncEnumerable<T>` as the injection type allows instances of all bindings implementing type `T` to be injected in an asynchronous-lazy manner - the instances will be provided one at a time, in an order corresponding to the sequence of the bindings.


```c#
using Shouldly;
using Pure.DI;

DI.Setup(nameof(Composition))
    .Bind<IDependency>().To<AbcDependency>()
    .Bind<IDependency>(2).To<XyzDependency>()
    .Bind<IService>().To<Service>()

    // Composition root
    .Root<IService>("Root");

var composition = new Composition();
var service = composition.Root;
var dependencies = await service.GetDependenciesAsync();
dependencies[0].ShouldBeOfType<AbcDependency>();
dependencies[1].ShouldBeOfType<XyzDependency>();

interface IDependency;

class AbcDependency : IDependency;

class XyzDependency : IDependency;

interface IService
{
    Task<IReadOnlyList<IDependency>> GetDependenciesAsync();
}

class Service(IAsyncEnumerable<IDependency> dependencies) : IService
{
    public async Task<IReadOnlyList<IDependency>> GetDependenciesAsync()
    {
        var deps = new List<IDependency>();
        await foreach (var dependency in dependencies)
        {
            deps.Add(dependency);
        }

        return deps;
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
```bash
dotnet add package Pure.DI
dotnet add package Shouldly
```
- Copy the example code into the _Program.cs_ file

You are ready to run the example 🚀
```bash
dotnet run
```

</details>

The following partial class will be generated:

```c#
partial class Composition
{
  private readonly Composition _root;

  [OrdinalAttribute(256)]
  public Composition()
  {
    _root = this;
  }

  internal Composition(Composition parentScope)
  {
    _root = (parentScope ?? throw new ArgumentNullException(nameof(parentScope)))._root;
  }

  public IService Root
  {
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    get
    {
      [MethodImpl(MethodImplOptions.AggressiveInlining)]
      async IAsyncEnumerable<IDependency> EnumerationOf_transientIAsyncEnumerable1()
      {
        yield return new AbcDependency();
        yield return new XyzDependency();
        await Task.CompletedTask;
      }

      var transientIAsyncEnumerable1 = EnumerationOf_transientIAsyncEnumerable1();
      return new Service(transientIAsyncEnumerable1);
    }
  }
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
	AbcDependency --|> IDependency
	XyzDependency --|> IDependency : 2 
	Service --|> IService
	Composition ..> Service : IService Root
	Service *--  IAsyncEnumerableᐸIDependencyᐳ : IAsyncEnumerableᐸIDependencyᐳ
	IAsyncEnumerableᐸIDependencyᐳ *--  AbcDependency : IDependency
	IAsyncEnumerableᐸIDependencyᐳ *--  XyzDependency : 2  IDependency
	namespace Pure.DI.UsageTests.BCL.AsyncEnumerableScenario {
		class AbcDependency {
				<<class>>
			+AbcDependency()
		}
		class Composition {
		<<partial>>
		+IService Root
		}
		class IDependency {
			<<interface>>
		}
		class IService {
			<<interface>>
		}
		class Service {
				<<class>>
			+Service(IAsyncEnumerableᐸIDependencyᐳ dependencies)
		}
		class XyzDependency {
				<<class>>
			+XyzDependency()
		}
	}
	namespace System.Collections.Generic {
		class IAsyncEnumerableᐸIDependencyᐳ {
				<<interface>>
		}
	}
```

