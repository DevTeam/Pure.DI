#### Bind attribute with lifetime and tag


```c#
using Pure.DI;

DI.Setup(nameof(Composition))
    .Bind().As(Lifetime.Singleton).To<Facade>()
    .Bind().To<Service>()

    // Composition root
    .Root<IService>("Root");

var composition = new Composition();
var service = composition.Root;
service.DoSomething();

interface IDependency
{
    public void DoSomething();
}

class Dependency : IDependency
{
    public void DoSomething()
    {
    }
}

class Facade
{
    [Bind(lifetime: Lifetime.Singleton, tags: ["my tag"])]
    public IDependency Dependency { get; } = new Dependency();
}

interface IService
{
    public void DoSomething();
}

class Service([Tag("my tag")] IDependency dep) : IService
{
    public void DoSomething() => dep.DoSomething();
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
- Add reference to NuGet package
  - [Pure.DI](https://www.nuget.org/packages/Pure.DI)
```bash
dotnet add package Pure.DI
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

  private IDependency? _singleIDependency;
  private Facade? _singleFacade51;

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
      if (_root._singleIDependency is null)
        lock (_lock)
          if (_root._singleIDependency is null)
          {
            if (_root._singleFacade51 is null)
            {
              _root._singleFacade51 = new Facade();
            }

            Facade localInstance_1182D1278 = _root._singleFacade51;
            _root._singleIDependency = localInstance_1182D1278.Dependency;
          }

      return new Service(_root._singleIDependency);
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
	Service --|> IService
	Composition ..> Service : IService Root
	IDependency o-- "Singleton" Facade : Facade
	Service o-- "Singleton" IDependency : "my tag"  IDependency
	namespace Pure.DI.UsageTests.Basics.BindAttributeWithLifetimeAndTagScenario {
		class Composition {
		<<partial>>
		+IService Root
		}
		class Facade {
				<<class>>
			+Facade()
		}
		class IDependency {
				<<interface>>
		}
		class IService {
			<<interface>>
		}
		class Service {
				<<class>>
			+Service(IDependency dep)
		}
	}
```

