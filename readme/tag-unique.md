#### Tag Unique

`Tag.Unique` is useful to register a binding with a unique tag. It will not be available through the composition root or `Resolve` methods directly, but can be injected in compositions as some kind of enumeration.


```c#
using Shouldly;
using Pure.DI;
using System.Collections.Immutable;

DI.Setup(nameof(Composition))
    .Bind<IDependency<TT>>(Tag.Unique).To<AbcDependency<TT>>()
    .Bind<IDependency<TT>>(Tag.Unique).To<XyzDependency<TT>>()
    .Bind<IService<TT>>().To<Service<TT>>()

    // Composition root
    .Root<IService<string>>("Root");

var composition = new Composition();
var stringService = composition.Root;
stringService.Dependencies.Length.ShouldBe(2);

interface IDependency<T>;

class AbcDependency<T> : IDependency<T>;

class XyzDependency<T> : IDependency<T>;

interface IService<T>
{
    ImmutableArray<IDependency<T>> Dependencies { get; }
}

class Service<T>(IEnumerable<IDependency<T>> dependencies) : IService<T>
{
    public ImmutableArray<IDependency<T>> Dependencies { get; }
        = [..dependencies];
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

  public IService<string> Root
  {
    get
    {
      IEnumerable<IDependency<string>> EnumerationOf_perBlockIEnumerable1()
      {
        yield return new AbcDependency<string>();
        yield return new XyzDependency<string>();
      }

      var perBlockIEnumerable1 = EnumerationOf_perBlockIEnumerable1();
      return new Service<string>(perBlockIEnumerable1);
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
	ServiceᐸStringᐳ --|> IServiceᐸStringᐳ
	AbcDependencyᐸStringᐳ --|> IDependencyᐸStringᐳ : "Unique tag #0" 
	XyzDependencyᐸStringᐳ --|> IDependencyᐸStringᐳ : "Unique tag #1" 
	Composition ..> ServiceᐸStringᐳ : IServiceᐸStringᐳ Root
	ServiceᐸStringᐳ o-- "PerBlock" IEnumerableᐸIDependencyᐸStringᐳᐳ : IEnumerableᐸIDependencyᐸStringᐳᐳ
	IEnumerableᐸIDependencyᐸStringᐳᐳ *--  AbcDependencyᐸStringᐳ : "Unique tag #0"  IDependencyᐸStringᐳ
	IEnumerableᐸIDependencyᐸStringᐳᐳ *--  XyzDependencyᐸStringᐳ : "Unique tag #1"  IDependencyᐸStringᐳ
	namespace Pure.DI.UsageTests.Advanced.TagUniqueScenario {
		class AbcDependencyᐸStringᐳ {
				<<class>>
			+AbcDependency()
		}
		class Composition {
		<<partial>>
		+IServiceᐸStringᐳ Root
		}
		class IDependencyᐸStringᐳ {
			<<interface>>
		}
		class IServiceᐸStringᐳ {
			<<interface>>
		}
		class ServiceᐸStringᐳ {
				<<class>>
			+Service(IEnumerableᐸIDependencyᐸStringᐳᐳ dependencies)
		}
		class XyzDependencyᐸStringᐳ {
				<<class>>
			+XyzDependency()
		}
	}
	namespace System.Collections.Generic {
		class IEnumerableᐸIDependencyᐸStringᐳᐳ {
				<<interface>>
		}
	}
```

