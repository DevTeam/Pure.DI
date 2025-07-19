#### Auto scoped

You can use the following example to automatically create a session when creating instances of a particular type:


```c#
using Shouldly;
using Pure.DI;
using static Pure.DI.Lifetime;

var composition = new Composition();
var program = composition.ProgramRoot;

// Creates service in session #1
var service1 = program.CreateService();

// Creates service in session #2
var service2 = program.CreateService();

// Checks that the scoped instances are not identical in different sessions
service1.Dependency.ShouldNotBe(service2.Dependency);

interface IDependency;

class Dependency : IDependency;

interface IService
{
    IDependency Dependency { get; }
}

class Service(IDependency dependency) : IService
{
    public IDependency Dependency => dependency;
}

// Implements a session
partial class Program(Func<IService> serviceFactory)
{
    public IService CreateService() => serviceFactory();
}

partial class Composition
{
    static void Setup() =>

        DI.Setup()
            .Bind().As(Scoped).To<Dependency>()
            // Session composition root
            .Root<Service>("SessionRoot", kind: RootKinds.Private)
            // Auto scoped
            .Bind().To(IService (Composition parentScope) =>
            {
                // Creates a new scope from the parent scope
                var scope = new Composition(parentScope);
                // Provides the session root in a new scope
                return scope.SessionRoot;
            })

            // Composition root
            .Root<Program>("ProgramRoot");
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

> [!IMPORTANT]
> The method `Inject()`cannot be used outside of the binding setup.

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

  private Dependency? _scopedDependency52;

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

  public Program ProgramRoot
  {
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    get
    {
      Func<IService> blockFunc1 = new Func<IService>(
      [MethodImpl(MethodImplOptions.AggressiveInlining)]
      () =>
      {
        IService transIService2;
        var transComposition3 = this;
        Composition localParentScope156 = transComposition3;
        // Creates a new scope from the parent scope
        var localScope157 = new Composition(localParentScope156);
        // Provides the session root in a new scope
        transIService2 = localScope157.SessionRoot;
        IService localValue155 = transIService2;
        return localValue155;
      });
      return new Program(blockFunc1);
    }
  }

  private Service SessionRoot
  {
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    get
    {
      if (_scopedDependency52 is null)
      {
        lock (_lock)
        {
          _scopedDependency52 = new Dependency();
        }
      }

      return new Service(_scopedDependency52);
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
	Composition ..> Program : Program ProgramRoot
	Composition ..> Service : Service SessionRoot
	IService *--  Composition : Composition
	Program o-- "PerBlock" FuncᐸIServiceᐳ : FuncᐸIServiceᐳ
	Service o-- "Scoped" Dependency : IDependency
	FuncᐸIServiceᐳ *--  IService : IService
	namespace Pure.DI.UsageTests.Lifetimes.AutoScopedScenario {
		class Composition {
		<<partial>>
		+Program ProgramRoot
		-Service SessionRoot
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
		class Program {
				<<class>>
			+Program(FuncᐸIServiceᐳ serviceFactory)
		}
		class Service {
				<<class>>
			+Service(IDependency dependency)
		}
	}
	namespace System {
		class FuncᐸIServiceᐳ {
				<<delegate>>
		}
	}
```

