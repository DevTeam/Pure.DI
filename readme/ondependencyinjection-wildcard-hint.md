#### OnDependencyInjection wildcard hint

Hints are used to fine-tune code generation. The _OnDependencyInjection_ hint determines whether to generate partial _OnDependencyInjection_ method to control of dependency injection.
In addition, setup hints can be comments before the _Setup_ method in the form ```hint = value```, for example: `// OnDependencyInjection = On`.


```c#
using Shouldly;
using Pure.DI;
using static Pure.DI.Hint;

// OnDependencyInjection = On
DI.Setup(nameof(Composition))
    .Hint(OnDependencyInjectionContractTypeNameWildcard, "*IDependency")
    .Hint(OnDependencyInjectionContractTypeNameWildcard, "*IService")
    .RootArg<int>("id")
    .Bind().To<Dependency>()
    .Bind().To<Service>()
    .Root<IService>("GetRoot");

var log = new List<string>();
var composition = new Composition(log);
var service = composition.GetRoot(33);

log.ShouldBe([
    "Dependency injected",
    "Service injected"]);

interface IDependency;

record Dependency(int Id) : IDependency;

interface IService
{
    IDependency Dependency { get; }
}

class Service(IDependency dependency) : IService
{
    public IDependency Dependency { get; } = dependency;
}

partial class Composition
{
    private readonly List<string> _log = [];

    public Composition(List<string> log) : this() =>
        _log = log;

    private partial T OnDependencyInjection<T>(
        in T value,
        object? tag,
        Lifetime lifetime)
    {
        _log.Add($"{value?.GetType().Name} injected");
        return value;
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

The `OnDependencyInjectionContractTypeNameWildcard` hint helps identify the set of types that require injection control. You can use it to specify a wildcard to filter the full name of a type.
For more hints, see [this](README.md#setup-hints) page.

The following partial class will be generated:

```c#
partial class Composition
{
#if NET9_0_OR_GREATER
  private readonly Lock _lock;
#else
  private readonly Object _lock;
#endif

  [OrdinalAttribute(256)]
  public Composition()
  {
#if NET9_0_OR_GREATER
    _lock = new Lock();
#else
    _lock = new Object();
#endif
  }

  internal Composition(Composition parentScope)
  {
  }

  [MethodImpl(MethodImplOptions.AggressiveInlining)]
  public IService GetRoot(int id)
  {
    return OnDependencyInjection<IService>(new Service(OnDependencyInjection<IDependency>(new Dependency(id), null, Lifetime.Transient)), null, Lifetime.Transient);
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
	Dependency --|> IDependency
	Dependency --|> IEquatableᐸDependencyᐳ
	Service --|> IService
	Composition ..> Service : IService GetRoot(int id)
	Dependency o-- Int32 : Argument "id"
	Service *--  Dependency : IDependency
	namespace Pure.DI.UsageTests.Hints.OnDependencyInjectionWildcardHintScenario {
		class Composition {
		<<partial>>
		+IService GetRoot(int id)
		}
		class Dependency {
				<<record>>
			+Dependency(Int32 Id)
		}
		class IDependency {
			<<interface>>
		}
		class IService {
			<<interface>>
		}
		class Service {
				<<class>>
			+Service(IDependency dependency)
		}
	}
	namespace System {
		class IEquatableᐸDependencyᐳ {
			<<interface>>
		}
		class Int32 {
				<<struct>>
		}
	}
```

