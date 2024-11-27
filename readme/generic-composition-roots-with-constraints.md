#### Generic composition roots with constraints

[![CSharp](https://img.shields.io/badge/C%23-code-blue.svg)](../tests/Pure.DI.UsageTests/Generics/GenericCompositionRootsWithConstraintsScenario.cs)

> [!IMPORTANT]
> `Resolve' methods cannot be used to resolve generic composition roots.


```c#
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
```

> [!IMPORTANT]
> The method `Inject()`cannot be used outside of the binding setup.

The following partial class will be generated:

```c#
partial class Composition
{
  private readonly Composition _root;

  [OrdinalAttribute(20)]
  public Composition()
  {
    _root = this;
  }

  internal Composition(Composition parentScope)
  {
    _root = (parentScope ?? throw new ArgumentNullException(nameof(parentScope)))._root;
  }

  [MethodImpl(MethodImplOptions.AggressiveInlining)]
  public IService<T2, bool> GetOtherService<T2>()
    where T2: IDisposable
  {
    OtherService<T2> transientOtherService0;
    IDependency<T2> localDependency76 = new Dependency<T2>();
    transientOtherService0 = new OtherService<T2>(localDependency76);
    return transientOtherService0;
  }

  [MethodImpl(MethodImplOptions.AggressiveInlining)]
  public IService<T2, T> GetMyRoot<T2, T>()
    where T2: IDisposable
    where T: struct
  {
    return new Service<T2, T>(new Dependency<T2>());
  }
}
```

Class diagram:

```mermaid
classDiagram
	class Composition {
		<<partial>>
		+IServiceᐸT2ˏTᐳ GetMyRootᐸT2ˏTᐳ()
		+IServiceᐸT2ˏBooleanᐳ GetOtherServiceᐸT2ᐳ()
	}
	OtherServiceᐸT2ᐳ --|> IServiceᐸT2ˏBooleanᐳ : "Other" 
	class OtherServiceᐸT2ᐳ
	ServiceᐸT2ˏTᐳ --|> IServiceᐸT2ˏTᐳ
	class ServiceᐸT2ˏTᐳ {
		+Service(IDependencyᐸT2ᐳ dependency)
	}
	DependencyᐸT2ᐳ --|> IDependencyᐸT2ᐳ
	class DependencyᐸT2ᐳ {
		+Dependency()
	}
	class IServiceᐸT2ˏBooleanᐳ {
		<<interface>>
	}
	class IServiceᐸT2ˏTᐳ {
		<<interface>>
	}
	class IDependencyᐸT2ᐳ {
		<<interface>>
	}
	Composition ..> OtherServiceᐸT2ᐳ : IServiceᐸT2ˏBooleanᐳ GetOtherServiceᐸT2ᐳ()
	Composition ..> ServiceᐸT2ˏTᐳ : IServiceᐸT2ˏTᐳ GetMyRootᐸT2ˏTᐳ()
	OtherServiceᐸT2ᐳ *--  DependencyᐸT2ᐳ : IDependencyᐸT2ᐳ
	ServiceᐸT2ˏTᐳ *--  DependencyᐸT2ᐳ : IDependencyᐸT2ᐳ
```

