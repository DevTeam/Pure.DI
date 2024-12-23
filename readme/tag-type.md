#### Tag Type

[![CSharp](https://img.shields.io/badge/C%23-code-blue.svg)](../tests/Pure.DI.UsageTests/Advanced/TagTypeScenario.cs)

`Tag.Type` in bindings replaces the expression `typeof(T)`, where `T` is the type of the implementation in a binding.


```c#
using Pure.DI;
using Shouldly;

DI.Setup(nameof(Composition))
    // Tag.Type here is the same as typeof(AbcDependency)
    // The `default` tag is used to resolve dependencies
    // when the tag was not specified by the consumer
    .Bind<IDependency>(Tag.Type, default).To<AbcDependency>()
    // Tag.Type here is the same as typeof(XyzDependency)
    .Bind<IDependency>(Tag.Type).As(Lifetime.Singleton).To<XyzDependency>()
    .Bind<IService>().To<Service>()

    // Composition root
    .Root<IService>("Root")

    // "XyzRoot" is root name, typeof(XyzDependency) is tag
    .Root<IDependency>("XyzRoot", typeof(XyzDependency));

var composition = new Composition();
var service = composition.Root;
service.Dependency1.ShouldBeOfType<AbcDependency>();
service.Dependency2.ShouldBeOfType<XyzDependency>();
service.Dependency2.ShouldBe(composition.XyzRoot);
service.Dependency3.ShouldBeOfType<AbcDependency>();

interface IDependency;

class AbcDependency : IDependency;

class XyzDependency : IDependency;

interface IService
{
    IDependency Dependency1 { get; }

    IDependency Dependency2 { get; }

    IDependency Dependency3 { get; }
}

class Service(
    [Tag(typeof(AbcDependency))] IDependency dependency1,
    [Tag(typeof(XyzDependency))] IDependency dependency2,
    IDependency dependency3)
    : IService
{
    public IDependency Dependency1 { get; } = dependency1;

    public IDependency Dependency2 { get; } = dependency2;

    public IDependency Dependency3 { get; } = dependency3;
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
	XyzDependency --|> IDependency : typeof(Pure.DI.UsageTests.Advanced.TagTypeScenario.XyzDependency) 
	Service --|> IService
	AbcDependency --|> IDependency : typeof(Pure.DI.UsageTests.Advanced.TagTypeScenario.AbcDependency) 
	AbcDependency --|> IDependency
	Composition ..> XyzDependency : IDependency XyzRoot
	Composition ..> Service : IService Root
	Service *--  AbcDependency : typeof(Pure.DI.UsageTests.Advanced.TagTypeScenario.AbcDependency)  IDependency
	Service o-- "Singleton" XyzDependency : typeof(Pure.DI.UsageTests.Advanced.TagTypeScenario.XyzDependency)  IDependency
	Service *--  AbcDependency : IDependency
	namespace Pure.DI.UsageTests.Advanced.TagTypeScenario {
		class AbcDependency {
			+AbcDependency()
		}
		class Composition {
		<<partial>>
		+IService Root
		+IDependency XyzRoot
		+ T ResolveᐸTᐳ()
		+ T ResolveᐸTᐳ(object? tag)
		+ object Resolve(Type type)
		+ object Resolve(Type type, object? tag)
		}
		class IDependency {
			<<interface>>
		}
		class IService {
			<<interface>>
		}
		class Service {
			+Service(IDependency dependency1, IDependency dependency2, IDependency dependency3)
		}
		class XyzDependency {
			+XyzDependency()
		}
	}
```

