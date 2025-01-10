#### Generic composition roots with constraints

[![CSharp](https://img.shields.io/badge/C%23-code-blue.svg)](../tests/Pure.DI.UsageTests/Generics/GenericCompositionRootsWithConstraintsScenario.cs)

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
  public IService<T3, bool> GetOtherService<T3>()
    where T3: IDisposable
  {
    OtherService<T3> transientOtherService0;
    IDependency<T3> localDependency86 = new Dependency<T3>();
    transientOtherService0 = new OtherService<T3>(localDependency86);
    return transientOtherService0;
  }

  [MethodImpl(MethodImplOptions.AggressiveInlining)]
  public IService<T3, T1> GetMyRoot<T3, T1>()
    where T3: IDisposable
    where T1: struct
  {
    return new Service<T3, T1>(new Dependency<T3>());
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
	OtherServiceᐸT3ᐳ --|> IServiceᐸT3ˏBooleanᐳ : "Other" 
	ServiceᐸT3ˏT1ᐳ --|> IServiceᐸT3ˏT1ᐳ
	DependencyᐸT3ᐳ --|> IDependencyᐸT3ᐳ
	Composition ..> OtherServiceᐸT3ᐳ : IServiceᐸT3ˏBooleanᐳ GetOtherServiceᐸT3ᐳ()
	Composition ..> ServiceᐸT3ˏT1ᐳ : IServiceᐸT3ˏT1ᐳ GetMyRootᐸT3ˏT1ᐳ()
	OtherServiceᐸT3ᐳ *--  DependencyᐸT3ᐳ : IDependencyᐸT3ᐳ
	ServiceᐸT3ˏT1ᐳ *--  DependencyᐸT3ᐳ : IDependencyᐸT3ᐳ
	namespace Pure.DI.UsageTests.Generics.GenericCompositionRootsWithConstraintsScenario {
		class Composition {
		<<partial>>
		+IServiceᐸT3ˏT1ᐳ GetMyRootᐸT3ˏT1ᐳ()
		+IServiceᐸT3ˏBooleanᐳ GetOtherServiceᐸT3ᐳ()
		}
		class DependencyᐸT3ᐳ {
			+Dependency()
		}
		class IDependencyᐸT3ᐳ {
			<<interface>>
		}
		class IServiceᐸT3ˏBooleanᐳ {
			<<interface>>
		}
		class IServiceᐸT3ˏT1ᐳ {
			<<interface>>
		}
		class OtherServiceᐸT3ᐳ {
		}
		class ServiceᐸT3ˏT1ᐳ {
			+Service(IDependencyᐸT3ᐳ dependency)
		}
	}
```

