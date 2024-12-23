#### Generic composition roots

[![CSharp](https://img.shields.io/badge/C%23-code-blue.svg)](../tests/Pure.DI.UsageTests/Generics/GenericsCompositionRootsScenario.cs)

Sometimes you want to be able to create composition roots with type parameters. In this case, the composition root can only be represented by a method.
> [!IMPORTANT]
> `Resolve()' methods cannot be used to resolve generic composition roots.


```c#
using Pure.DI;

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

interface IDependency<T>;

class Dependency<T> : IDependency<T>;

interface IService<T>;

class Service<T>(IDependency<T> dependency) : IService<T>;

class OtherService<T>(IDependency<T> dependency) : IService<T>;
```

> [!IMPORTANT]
> The method `Inject()`cannot be used outside of the binding setup.


Class diagram:

```mermaid
---
 config:
  class:
   hideEmptyMembersBox: true
---
classDiagram
	OtherServiceᐸT1ᐳ --|> IServiceᐸT1ᐳ : "Other" 
	ServiceᐸT1ᐳ --|> IServiceᐸT1ᐳ
	DependencyᐸT1ᐳ --|> IDependencyᐸT1ᐳ
	Composition ..> OtherServiceᐸT1ᐳ : IServiceᐸT1ᐳ GetOtherServiceᐸT1ᐳ()
	Composition ..> ServiceᐸT1ᐳ : IServiceᐸT1ᐳ GetMyRootᐸT1ᐳ()
	OtherServiceᐸT1ᐳ *--  DependencyᐸT1ᐳ : IDependencyᐸT1ᐳ
	ServiceᐸT1ᐳ *--  DependencyᐸT1ᐳ : IDependencyᐸT1ᐳ
	namespace Pure.DI.UsageTests.Generics.GenericsCompositionRootsScenario {
		class Composition {
		<<partial>>
		+IServiceᐸT1ᐳ GetMyRootᐸT1ᐳ()
		+IServiceᐸT1ᐳ GetOtherServiceᐸT1ᐳ()
		}
		class DependencyᐸT1ᐳ {
			+Dependency()
		}
		class IDependencyᐸT1ᐳ {
			<<interface>>
		}
		class IServiceᐸT1ᐳ {
			<<interface>>
		}
		class OtherServiceᐸT1ᐳ {
		}
		class ServiceᐸT1ᐳ {
			+Service(IDependencyᐸT1ᐳ dependency)
		}
	}
```

