#### Generic composition roots with constraints

> [!IMPORTANT]
> `Resolve' methods cannot be used to resolve generic composition roots.


```c#
using Pure.DI;

DI.Setup(nameof(Composition))
    // This hint indicates to not generate methods such as Resolve
    .Hint(Hint.Resolve, "Off")
    .Bind().To<Dependency<TTDisposable>>()
    .Bind().To<Service<TTDisposable, TTS>>()
    // Creates OtherService manually,
    // just for the sake of example
    .Bind("Other").To(ctx =>
    {
        ctx.Inject(out IDependency<TTDisposable> dependency);
        return new OtherService<TTDisposable>(dependency);
    })

    // Specifies to create a regular public method
    // to get a composition root of type Service<T, TStruct>
    // with the name "GetMyRoot"
    .Root<IService<TTDisposable, TTS>>("GetMyRoot")

    // Specifies to create a regular public method
    // to get a composition root of type OtherService<T>
    // with the name "GetOtherService"
    // using the "Other" tag
    .Root<IService<TTDisposable, bool>>("GetOtherService", "Other");

var composition = new Composition();

// service = new Service<Stream, double>(new Dependency<Stream>());
var service = composition.GetMyRoot<Stream, double>();

// someOtherService = new OtherService<BinaryReader>(new Dependency<BinaryReader>());
var someOtherService = composition.GetOtherService<BinaryReader>();

interface IDependency<T>
    where T : IDisposable;

class Dependency<T> : IDependency<T>
    where T : IDisposable;

interface IService<T, TStruct>
    where T : IDisposable
    where TStruct : struct;

class Service<T, TStruct>(IDependency<T> dependency) : IService<T, TStruct>
    where T : IDisposable
    where TStruct : struct;

class OtherService<T>(IDependency<T> dependency) : IService<T, bool>
    where T : IDisposable;
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

> [!IMPORTANT]
> The method `Inject()`cannot be used outside of the binding setup.

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
  public IService<T5, bool> GetOtherService<T5>()
    where T5: IDisposable
  {
    OtherService<T5> transientOtherService0;
    IDependency<T5> localDependency95 = new Dependency<T5>();
    transientOtherService0 = new OtherService<T5>(localDependency95);
    return transientOtherService0;
  }

  [MethodImpl(MethodImplOptions.AggressiveInlining)]
  public IService<T5, T1> GetMyRoot<T5, T1>()
    where T5: IDisposable
    where T1: struct
  {
    return new Service<T5, T1>(new Dependency<T5>());
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
	OtherServiceᐸT5ᐳ --|> IServiceᐸT5ˏBooleanᐳ : "Other" 
	ServiceᐸT5ˏT1ᐳ --|> IServiceᐸT5ˏT1ᐳ
	DependencyᐸT5ᐳ --|> IDependencyᐸT5ᐳ
	Composition ..> OtherServiceᐸT5ᐳ : IServiceᐸT5ˏBooleanᐳ GetOtherServiceᐸT5ᐳ()
	Composition ..> ServiceᐸT5ˏT1ᐳ : IServiceᐸT5ˏT1ᐳ GetMyRootᐸT5ˏT1ᐳ()
	OtherServiceᐸT5ᐳ *--  DependencyᐸT5ᐳ : IDependencyᐸT5ᐳ
	ServiceᐸT5ˏT1ᐳ *--  DependencyᐸT5ᐳ : IDependencyᐸT5ᐳ
	namespace Pure.DI.UsageTests.Generics.GenericCompositionRootsWithConstraintsScenario {
		class Composition {
		<<partial>>
		+IServiceᐸT5ˏT1ᐳ GetMyRootᐸT5ˏT1ᐳ()
		+IServiceᐸT5ˏBooleanᐳ GetOtherServiceᐸT5ᐳ()
		}
		class DependencyᐸT5ᐳ {
			+Dependency()
		}
		class IDependencyᐸT5ᐳ {
			<<interface>>
		}
		class IServiceᐸT5ˏBooleanᐳ {
			<<interface>>
		}
		class IServiceᐸT5ˏT1ᐳ {
			<<interface>>
		}
		class OtherServiceᐸT5ᐳ {
		}
		class ServiceᐸT5ˏT1ᐳ {
			+Service(IDependencyᐸT5ᐳ dependency)
		}
	}
```

