#### Generic composition roots with constraints

[![CSharp](https://img.shields.io/badge/C%23-code-blue.svg)](../tests/Pure.DI.UsageTests/Generics/GenericCompositionRootsWithConstraintsScenario.cs)


```c#
interface IDependency<T>
    where T: IDisposable;

class Dependency<T> : IDependency<T>
    where T: IDisposable;

interface IService<T, TStruct>
    where T: IDisposable
    where TStruct: struct;

class Service<T, TStruct>(IDependency<T> dependency) : IService<T, TStruct>
    where T: IDisposable
    where TStruct: struct;

class OtherService<T>(IDependency<T> dependency) : IService<T, bool>
    where T: IDisposable;

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
> `Resolve' methods cannot be used to resolve generic composition roots.

The following partial class will be generated:

```c#
partial class Composition
{
  private readonly Composition _root;

  public Composition()
  {
    _root = this;
  }

  internal Composition(Composition parentScope)
  {
    _root = (parentScope ?? throw new ArgumentNullException(nameof(parentScope)))._root;
  }

  [MethodImpl(MethodImplOptions.AggressiveInlining)]
  public IService<T, T1> GetMyRoot<T, T1>()
    where T: IDisposable
    where T1: struct
  {
    return new Service<T, T1>(new Dependency<T>());
  }

  [MethodImpl(MethodImplOptions.AggressiveInlining)]
  public IService<T, bool> GetOtherService<T>()
    where T: IDisposable
  {
    OtherService<T> transientOtherService0;
    {
        var localDependency49 = new Dependency<T>();
        transientOtherService0 = new OtherService<T>(localDependency49);
    }

    return transientOtherService0;
  }
}
```

Class diagram:

```mermaid
classDiagram
	class Composition {
		<<partial>>
		+IServiceᐸTˏT1ᐳ GetMyRootᐸTˏT1ᐳ()
		+IServiceᐸTˏBooleanᐳ GetOtherServiceᐸTᐳ()
	}
	ServiceᐸTˏT1ᐳ --|> IServiceᐸTˏT1ᐳ
	class ServiceᐸTˏT1ᐳ {
		+Service(IDependencyᐸTᐳ dependency)
	}
	OtherServiceᐸTᐳ --|> IServiceᐸTˏBooleanᐳ : "Other" 
	class OtherServiceᐸTᐳ
	DependencyᐸTᐳ --|> IDependencyᐸTᐳ
	class DependencyᐸTᐳ {
		+Dependency()
	}
	class IServiceᐸTˏT1ᐳ {
		<<interface>>
	}
	class IServiceᐸTˏBooleanᐳ {
		<<interface>>
	}
	class IDependencyᐸTᐳ {
		<<interface>>
	}
	Composition ..> ServiceᐸTˏT1ᐳ : IServiceᐸTˏT1ᐳ GetMyRootᐸTˏT1ᐳ()
	Composition ..> OtherServiceᐸTᐳ : IServiceᐸTˏBooleanᐳ GetOtherServiceᐸTᐳ()
	ServiceᐸTˏT1ᐳ *--  DependencyᐸTᐳ : IDependencyᐸTᐳ
	OtherServiceᐸTᐳ *--  DependencyᐸTᐳ : IDependencyᐸTᐳ
```

