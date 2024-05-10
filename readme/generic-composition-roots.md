#### Generic composition roots

[![CSharp](https://img.shields.io/badge/C%23-code-blue.svg)](../tests/Pure.DI.UsageTests/Generics/GenericsCompositionRootsScenario.cs)

A generic composition root is represented by a method.

```c#
interface IDependency<T>;

class Dependency<T> : IDependency<T>;

interface IService<T>;

class Service<T>(IDependency<T> dependency) : IService<T>;

class OtherService<T>(IDependency<T> dependency) : IService<T>;

DI.Setup(nameof(Composition))
    // This hint indicates to not generate methods such as Resolve
    .Hint(Hint.Resolve, "Off")
    .Bind().To<Dependency<TT>>()
    .Bind().To<Service<TT>>()
    // Creates OtherService manually,
    // just for the sake of example
    .Bind("Other").To(ctx =>
    {
        ctx.Inject(out IDependency<TT> dependency);
        return new OtherService<TT>(dependency);
    })

    // Specifies to create a regular public method
    // to get a composition root of type Service<T>
    // with the name "GetMyRoot"
    .Root<IService<TT>>("GetMyRoot")

    // Specifies to create a regular public method
    // to get a composition root of type OtherService<T>
    // with the name "GetOtherService"
    // using the "Other" tag
    .Root<IService<TT>>("GetOtherService", "Other");

var composition = new Composition();
        
// service = new Service<int>(new Dependency<int>());
var service = composition.GetMyRoot<int>();
        
// someOtherService = new OtherService<int>(new Dependency<int>());
var someOtherService = composition.GetOtherService<string>();
```

When a generic composition root is used, `Resolve` methods cannot be used to resolve them.

<details open>
<summary>Class Diagram</summary>

```mermaid
classDiagram
	class Composition {
		<<partial>>
		+IServiceᐸT54ᐳ GetMyRootᐸT54ᐳ()
		+IServiceᐸT54ᐳ GetOtherServiceᐸT54ᐳ()
	}
	ServiceᐸT54ᐳ --|> IServiceᐸT54ᐳ : 
	class ServiceᐸT54ᐳ {
		+Service(IDependencyᐸT54ᐳ dependency)
	}
	OtherServiceᐸT54ᐳ --|> IServiceᐸT54ᐳ : "Other" 
	class OtherServiceᐸT54ᐳ
	DependencyᐸT54ᐳ --|> IDependencyᐸT54ᐳ : 
	class DependencyᐸT54ᐳ {
		+Dependency()
	}
	class IServiceᐸT54ᐳ {
		<<interface>>
	}
	class IDependencyᐸT54ᐳ {
		<<interface>>
	}
	Composition ..> ServiceᐸT54ᐳ : IServiceᐸT54ᐳ GetMyRootᐸT54ᐳ()
	Composition ..> OtherServiceᐸT54ᐳ : IServiceᐸT54ᐳ GetOtherServiceᐸT54ᐳ()
	ServiceᐸT54ᐳ *--  DependencyᐸT54ᐳ : IDependencyᐸT54ᐳ
	OtherServiceᐸT54ᐳ *--  DependencyᐸT54ᐳ : IDependencyᐸT54ᐳ
```

</details>

<details>
<summary>Pure.DI-generated partial class Composition</summary><blockquote>

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
  public IService<T54> GetMyRoot<T54>()
  {
    return new Service<T54>(new Dependency<T54>());
  }

  [MethodImpl(MethodImplOptions.AggressiveInlining)]
  public IService<T54> GetOtherService<T54>()
  {
    OtherService<T54> transient0_OtherService;
    {
        var dependency_1 = new Dependency<T54>();
        transient0_OtherService = new OtherService<T54>(dependency_1);
    }
    return transient0_OtherService;
  }
}
```

</blockquote></details>

