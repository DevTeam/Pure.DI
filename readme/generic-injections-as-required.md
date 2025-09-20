#### Generic injections as required


```c#
using Shouldly;
using Pure.DI;
using System.Collections.Generic;

DI.Setup(nameof(Composition))
    .Bind().To<Dependency<TT>>()
    .Bind().To<Service<TT>>()

    // Composition root
    .Root<IService<int>>("Root");

var composition = new Composition();
var service = composition.Root;
service.Dependencies.Count.ShouldBe(2);

interface IDependency<T>;

class Dependency<T> : IDependency<T>;

interface IService<T>
{
    IReadOnlyList<IDependency<T>> Dependencies { get; }
}

class Service<T>(Func<IDependency<T>> dependencyFactory): IService<T>
{
    public IReadOnlyList<IDependency<T>> Dependencies { get; } =
    [
        dependencyFactory(),
        dependencyFactory()
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
  [OrdinalAttribute(256)]
  public Composition()
  {
  }

  internal Composition(Composition parentScope)
  {
  }

  public IService<int> Root
  {
    get
    {
      Func<IDependency<int>> perBlockFunc1 = new Func<IDependency<int>>(() =>
      {
        IDependency<int> localValue27 = new Dependency<int>();
        return localValue27;
      });
      return new Service<int>(perBlockFunc1);
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
	ServiceᐸInt32ᐳ --|> IServiceᐸInt32ᐳ
	DependencyᐸInt32ᐳ --|> IDependencyᐸInt32ᐳ
	Composition ..> ServiceᐸInt32ᐳ : IServiceᐸInt32ᐳ Root
	ServiceᐸInt32ᐳ o-- "PerBlock" FuncᐸIDependencyᐸInt32ᐳᐳ : FuncᐸIDependencyᐸInt32ᐳᐳ
	FuncᐸIDependencyᐸInt32ᐳᐳ *--  DependencyᐸInt32ᐳ : IDependencyᐸInt32ᐳ
	namespace Pure.DI.UsageTests.Generics.GenericInjectionsAsRequiredScenario {
		class Composition {
		<<partial>>
		+IServiceᐸInt32ᐳ Root
		}
		class DependencyᐸInt32ᐳ {
				<<class>>
			+Dependency()
		}
		class IDependencyᐸInt32ᐳ {
			<<interface>>
		}
		class IServiceᐸInt32ᐳ {
			<<interface>>
		}
		class ServiceᐸInt32ᐳ {
				<<class>>
			+Service(FuncᐸIDependencyᐸInt32ᐳᐳ dependencyFactory)
		}
	}
	namespace System {
		class FuncᐸIDependencyᐸInt32ᐳᐳ {
				<<delegate>>
		}
	}
```

