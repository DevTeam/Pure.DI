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
  public IService<T, T4> GetMyRoot<T, T4>()
    where T: IDisposable
    where T4: struct
  {
    return new Service<T, T4>(new Dependency<T>());
  }

  [MethodImpl(MethodImplOptions.AggressiveInlining)]
  public IService<T, bool> GetOtherService<T>()
    where T: IDisposable
  {
    OtherService<T> transient0_OtherService;
    {
        var dependency_1 = new Dependency<T>();
        transient0_OtherService = new OtherService<T>(dependency_1);
    }
    return transient0_OtherService;
  }
}
```

Class diagram:

```mermaid
classDiagram
	class Composition {
		<<partial>>
		+IServiceᐸTˏT4ᐳ GetMyRootᐸTˏT4ᐳ()
		+IServiceᐸTˏBooleanᐳ GetOtherServiceᐸTᐳ()
	}
	ServiceᐸTˏT4ᐳ --|> IServiceᐸTˏT4ᐳ
	class ServiceᐸTˏT4ᐳ {
		+Service(IDependencyᐸTᐳ dependency)
	}
	OtherServiceᐸTᐳ --|> IServiceᐸTˏBooleanᐳ : "Other" 
	class OtherServiceᐸTᐳ
	DependencyᐸTᐳ --|> IDependencyᐸTᐳ
	class DependencyᐸTᐳ {
		+Dependency()
	}
	class IServiceᐸTˏT4ᐳ {
		<<interface>>
	}
	class IServiceᐸTˏBooleanᐳ {
		<<interface>>
	}
	class IDependencyᐸTᐳ {
		<<interface>>
	}
	Composition ..> ServiceᐸTˏT4ᐳ : IServiceᐸTˏT4ᐳ GetMyRootᐸTˏT4ᐳ()
	Composition ..> OtherServiceᐸTᐳ : IServiceᐸTˏBooleanᐳ GetOtherServiceᐸTᐳ()
	ServiceᐸTˏT4ᐳ *--  DependencyᐸTᐳ : IDependencyᐸTᐳ
	OtherServiceᐸTᐳ *--  DependencyᐸTᐳ : IDependencyᐸTᐳ
```

