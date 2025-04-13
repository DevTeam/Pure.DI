#### Injections as required with arguments


```c#
using Shouldly;
using Pure.DI;
using System.Collections.Generic;

DI.Setup(nameof(Composition))
    .Bind().To<Dependency>()
    .Bind().To<Service>()

    // Composition root
    .Root<IService>("Root");

var composition = new Composition();
var service = composition.Root;
var dependencies = service.Dependencies;
dependencies.Count.ShouldBe(2);
dependencies[0].Id.ShouldBe(33);
dependencies[1].Id.ShouldBe(99);

interface IDependency
{
    int Id { get; }
}

class Dependency(int id) : IDependency
{
    public int Id { get; } = id;
}

interface IService
{
    IReadOnlyList<IDependency> Dependencies { get; }
}

class Service(Func<int, IDependency> dependencyFactoryWithArgs): IService
{
    public IReadOnlyList<IDependency> Dependencies { get; } =
    [
        dependencyFactoryWithArgs(33),
        dependencyFactoryWithArgs(99)
    ];
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
  private readonly Lock _lock;

  [OrdinalAttribute(256)]
  public Composition()
  {
    _root = this;
    _lock = new Lock();
  }

  internal Composition(Composition parentScope)
  {
    _root = (parentScope ?? throw new ArgumentNullException(nameof(parentScope)))._root;
    _lock = _root._lock;
  }

  public IService Root
  {
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    get
    {
      int overrInt320;
      Func<int, IDependency> perBlockFunc1 = new Func<int, IDependency>(
      [MethodImpl(MethodImplOptions.AggressiveInlining)]
      (int localArg116) =>
      {
        overrInt320 = localArg116;
        IDependency localValue98 = new Dependency(overrInt320);
        return localValue98;
      });
      return new Service(perBlockFunc1);
    }
  }
}
```

Class diagram:

```mermaid
---
 config:
  class:
   hideEmptyMembersBox: true
---
classDiagram
	Dependency --|> IDependency
	Service --|> IService
	Composition ..> Service : IService Root
	Dependency o-- "PerResolve" Int32 : Int32
	Service o-- "PerBlock" FuncᐸInt32ˏIDependencyᐳ : FuncᐸInt32ˏIDependencyᐳ
	FuncᐸInt32ˏIDependencyᐳ *--  Dependency : IDependency
	namespace Pure.DI.UsageTests.Basics.InjectionsAsRequiredWithArgumentsScenario {
		class Composition {
		<<partial>>
		+IService Root
		}
		class Dependency {
			+Dependency(Int32 id)
		}
		class IDependency {
			<<interface>>
		}
		class IService {
			<<interface>>
		}
		class Service {
			+Service(FuncᐸInt32ˏIDependencyᐳ dependencyFactoryWithArgs)
		}
	}
	namespace System {
		class FuncᐸInt32ˏIDependencyᐳ {
				<<delegate>>
		}
		class Int32 {
			<<struct>>
		}
	}
```

