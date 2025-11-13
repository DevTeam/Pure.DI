#### Bind attribute

`BindAttribute` allows you to perform automatic binding to properties, fields or methods that belong to the type of the binding involved.


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

interface IOtherDependency
{
    public void DoSomething();
}

class OtherDependency : IOtherDependency
{
    public void DoSomething()
    {
    }
}

class Facade
{
    [Bind] public IDependency Dependency { get; } = new Dependency();

    [Bind] public IOtherDependency OtherDependency { get; } = new OtherDependency();
}

interface IService
{
    public void DoSomething();
}

class Service(IDependency dep, Func<IOtherDependency> otherDep) : IService
{
    public void DoSomething()
    {
        dep.DoSomething();
        otherDep().DoSomething();
    }
}
```

<details>
<summary>Running this code sample locally</summary>

- Make sure you have the [.NET SDK 10.0](https://dotnet.microsoft.com/en-us/download/dotnet/10.0) or later is installed
```bash
dotnet --list-sdk
```
- Create a net10.0 (or later) console application
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

This attribute `BindAttribute` applies to field properties and methods, to regular, static, and even returning generalized types.

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

  private Facade? _singletonFacade51;

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
      IDependency transientIDependency1;
      EnsureFacadeExists();
      Facade localInstance_1182D1277 = _root._singletonFacade51;
      transientIDependency1 = localInstance_1182D1277.Dependency;
      Func<IOtherDependency> transientFunc2 = new Func<IOtherDependency>(
      [MethodImpl(MethodImplOptions.AggressiveInlining)]
      () =>
      {
        IOtherDependency transientIOtherDependency4;
        EnsureFacadeExists();
        Facade localInstance_1182D1278 = _root._singletonFacade51;
        transientIOtherDependency4 = localInstance_1182D1278.OtherDependency;
        IOtherDependency localValue16 = transientIOtherDependency4;
        return localValue16;
      });
      return new Service(transientIDependency1, transientFunc2);
      [MethodImpl(MethodImplOptions.AggressiveInlining)]
      void EnsureFacadeExists()
      {
        if (_root._singletonFacade51 is null)
          lock (_lock)
            if (_root._singletonFacade51 is null)
            {
              _root._singletonFacade51 = new Facade();
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
	Service --|> IService
	Composition ..> Service : IService Root
	Service *--  IDependency : IDependency
	Service o-- "PerBlock" FuncᐸIOtherDependencyᐳ : FuncᐸIOtherDependencyᐳ
	IOtherDependency o-- "Singleton" Facade : Facade
	IDependency o-- "Singleton" Facade : Facade
	FuncᐸIOtherDependencyᐳ *--  IOtherDependency : IOtherDependency
	namespace Pure.DI.UsageTests.Basics.BindAttributeScenario {
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
		class IOtherDependency {
				<<interface>>
		}
		class IService {
			<<interface>>
		}
		class Service {
				<<class>>
			+Service(IDependency dep, FuncᐸIOtherDependencyᐳ otherDep)
		}
	}
	namespace System {
		class FuncᐸIOtherDependencyᐳ {
				<<delegate>>
		}
	}
```

