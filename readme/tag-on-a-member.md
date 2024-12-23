#### Tag on a member

[![CSharp](https://img.shields.io/badge/C%23-code-blue.svg)](../tests/Pure.DI.UsageTests/Advanced/TagOnMemberScenario.cs)

The wildcards ‘*’ and ‘?’ are supported.


```c#
using Pure.DI;
using Shouldly;

DI.Setup(nameof(Composition))
    .Bind().To<AbcDependency>()
    .Bind(Tag.OnMember<Service>(nameof(Service.Dependency)))
        .To<XyzDependency>()
    .Bind<IService>().To<Service>()

    // Specifies to create the composition root named "Root"
    .Root<IService>("Root");

var composition = new Composition();
var service = composition.Root;
service.Dependency.ShouldBeOfType<XyzDependency>();

interface IDependency;

class AbcDependency : IDependency;

class XyzDependency : IDependency;

interface IService
{
    IDependency Dependency { get; }
}

class Service : IService
{
    public required IDependency Dependency { init; get; }
}
```

> [!WARNING]
> Each potentially injectable argument, property, or field contains an additional tag. This tag can be used to specify what can be injected there. This will only work if the binding type and the tag match. So while this approach can be useful for specifying what to enter, it can be more expensive to maintain and less reliable, so it is recommended to use attributes like `[Tag(...)]` instead.


Class diagram:

```mermaid
---
 config:
  class:
   hideEmptyMembersBox: true
---
classDiagram
	Service --|> IService
	XyzDependency --|> IDependency
	Composition ..> Service : IService Root
	Service *--  XyzDependency : IDependency
	namespace Pure.DI.UsageTests.Advanced.TagOnMemberScenario {
		class Composition {
		<<partial>>
		+IService Root
		}
		class IDependency {
			<<interface>>
		}
		class IService {
			<<interface>>
		}
		class Service {
			+Service()
			+IDependency Dependency
		}
		class XyzDependency {
			+XyzDependency()
		}
	}
```

