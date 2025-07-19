#### Default lifetime for a type

For example, if a certain lifetime is used more often than others, you can make it the default lifetime for a certain type:


```c#
using Shouldly;
using Pure.DI;
using static Pure.DI.Lifetime;

DI.Setup(nameof(Composition))
    // Default lifetime applied to a specific type
    .DefaultLifetime<IDependency>(Singleton)
    .Bind().To<Dependency>()
    .Bind().To<Service>()
    .Root<IService>("Root");

var composition = new Composition();
var service1 = composition.Root;
var service2 = composition.Root;
service1.ShouldNotBe(service2);
service1.Dependency1.ShouldBe(service1.Dependency2);
service1.Dependency1.ShouldBe(service2.Dependency1);

interface IDependency;

class Dependency : IDependency;

interface IService
{
    public IDependency Dependency1 { get; }

    public IDependency Dependency2 { get; }
}

class Service(
    IDependency dependency1,
    IDependency dependency2)
    : IService
{
    public IDependency Dependency1 { get; } = dependency1;

    public IDependency Dependency2 { get; } = dependency2;
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
#if NET9_0_OR_GREATER
  private readonly Lock _lock;
#else
  private readonly Object _lock;
#endif

  private Dependency? _singleDependency52;

  [OrdinalAttribute(256)]
  public Composition()
  {
    _root = this;
#if NET9_0_OR_GREATER
    _lock = new Lock();
#else
    _lock = new Object();
#endif
  }

  internal Composition(Composition parentScope)
  {
    _root = (parentScope ?? throw new ArgumentNullException(nameof(parentScope)))._root;
    _lock = parentScope._lock;
  }

  public IService Root
  {
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    get
    {
      EnsureDependencyExists0();
      return new Service(_root._singleDependency52, _root._singleDependency52);
      [MethodImpl(MethodImplOptions.AggressiveInlining)]
      void EnsureDependencyExists0()
      {
        if (_root._singleDependency52 is null)
        {
          lock (_lock)
          {
            _root._singleDependency52 = new Dependency();
          }
        }
      }
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
	Dependency --|> IDependency
	Service --|> IService
	Composition ..> Service : IService Root
	Service o-- "2 Singleton instances" Dependency : IDependency
	namespace Pure.DI.UsageTests.Lifetimes.DefaultLifetimeForTypeScenario {
		class Composition {
		<<partial>>
		+IService Root
		}
		class Dependency {
				<<class>>
			+Dependency()
		}
		class IDependency {
			<<interface>>
		}
		class IService {
			<<interface>>
		}
		class Service {
				<<class>>
			+Service(IDependency dependency1, IDependency dependency2)
		}
	}
```

