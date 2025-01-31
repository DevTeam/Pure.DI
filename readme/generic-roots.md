#### Generic roots

[![CSharp](https://img.shields.io/badge/C%23-code-blue.svg)](../tests/Pure.DI.UsageTests/Generics/GenericsRootsScenario.cs)


```c#
using Pure.DI;

DI.Setup(nameof(Composition))
    // This hint indicates to not generate methods such as Resolve
    .Hint(Hint.Resolve, "Off")
    .Bind().To<Dependency<TT>>()
    .Bind().To<Service<TT>>()
    // Creates OtherService manually,
    // just for the sake of example
    .Bind<OtherService<TT>>().To(ctx =>
    {
        ctx.Inject(out IDependency<TT> dependency);
        return new OtherService<TT>(dependency);
    })

    // Specifies to define composition roots for all types inherited from IService<TT>
    // available at compile time at the point where the method is called
    .Roots<IService<TT>>("GetMy{type}");

var composition = new Composition();

// service = new Service<int>(new Dependency<int>());
var service = composition.GetMyService_T<int>();

// someOtherService = new OtherService<int>(new Dependency<int>());
var someOtherService = composition.GetMyOtherService_T<string>();

interface IDependency<T>;

class Dependency<T> : IDependency<T>;

interface IService<T>;

class Service<T>(IDependency<T> dependency) : IService<T>;

class OtherService<T>(IDependency<T> dependency) : IService<T>;
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

  [OrdinalAttribute(256)]
  public Composition()
  {
    _root = this;
  }

  internal Composition(Composition parentScope)
  {
    _root = (parentScope ?? throw new ArgumentNullException(nameof(parentScope)))._root;
  }

  [MethodImpl(MethodImplOptions.AggressiveInlining)]
  public OtherService<T3> GetMyOtherService_T<T3>()
  {
    OtherService<T3> transientOtherService0;
    IDependency<T3> localDependency97 = new Dependency<T3>();
    transientOtherService0 = new OtherService<T3>(localDependency97);
    return transientOtherService0;
  }

  [MethodImpl(MethodImplOptions.AggressiveInlining)]
  public Service<T3> GetMyService_T<T3>()
  {
    return new Service<T3>(new Dependency<T3>());
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
	DependencyᐸT3ᐳ --|> IDependencyᐸT3ᐳ
	Composition ..> OtherServiceᐸT3ᐳ : OtherServiceᐸT3ᐳ GetMyOtherService_TᐸT3ᐳ()
	Composition ..> ServiceᐸT3ᐳ : ServiceᐸT3ᐳ GetMyService_TᐸT3ᐳ()
	OtherServiceᐸT3ᐳ *--  DependencyᐸT3ᐳ : IDependencyᐸT3ᐳ
	ServiceᐸT3ᐳ *--  DependencyᐸT3ᐳ : IDependencyᐸT3ᐳ
	namespace Pure.DI.UsageTests.Generics.GenericsRootsScenario {
		class Composition {
		<<partial>>
		+OtherServiceᐸT3ᐳ GetMyOtherService_TᐸT3ᐳ()
		+ServiceᐸT3ᐳ GetMyService_TᐸT3ᐳ()
		}
		class DependencyᐸT3ᐳ {
			+Dependency()
		}
		class IDependencyᐸT3ᐳ {
			<<interface>>
		}
		class OtherServiceᐸT3ᐳ {
		}
		class ServiceᐸT3ᐳ {
		}
	}
```

