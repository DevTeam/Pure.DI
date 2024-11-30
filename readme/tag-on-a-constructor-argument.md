#### Tag on a constructor argument

[![CSharp](https://img.shields.io/badge/C%23-code-blue.svg)](../tests/Pure.DI.UsageTests/Advanced/TagOnConstructorArgScenario.cs)

The wildcards ‘*’ and ‘?’ are supported.


```c#
namespace Pure.DI.UsageTests.Advanced.TagOnConstructorArgScenario;


interface IDependency;

class AbcDependency : IDependency;

class XyzDependency : IDependency;

class Consumer<T>(IDependency myDep)
{
    public IDependency Dependency { get; } = myDep;
}

interface IService
{
    IDependency Dependency1 { get; }

    IDependency Dependency2 { get; }
}

class Service(
    IDependency dependency1,
    Consumer<string> consumer)
    : IService
{
    public IDependency Dependency1 { get; } = dependency1;

    public IDependency Dependency2 => consumer.Dependency;
}

DI.Setup(nameof(Composition))
    .Bind(Tag.OnConstructorArg<Service>("dependency1"))
    .To<AbcDependency>()
    .Bind(Tag.OnConstructorArg<Consumer<TT>>("myDep"))
    .To<XyzDependency>()
    .Bind<IService>().To<Service>()

    // Specifies to create the composition root named "Root"
    .Root<IService>("Root");

var composition = new Composition();
var service = composition.Root;
service.Dependency1.ShouldBeOfType<AbcDependency>();
service.Dependency2.ShouldBeOfType<XyzDependency>();
```

> [!WARNING]
> Each potentially injectable argument, property, or field contains an additional tag. This tag can be used to specify what can be injected there. This will only work if the binding type and the tag match. So while this approach can be useful for specifying what to enter, it can be more expensive to maintain and less reliable, so it is recommended to use attributes like `[Tag(...)]` instead.

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

  public IService Root
  {
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    get
    {
      return new Service(new AbcDependency(), new Consumer<string>(new XyzDependency()));
    }
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
	Service --|> IService
	AbcDependency --|> IDependency
	XyzDependency --|> IDependency
	Composition ..> Service : IService Root
	Service *--  AbcDependency : IDependency
	Service *--  ConsumerᐸStringᐳ : ConsumerᐸStringᐳ
	ConsumerᐸStringᐳ *--  XyzDependency : IDependency
	namespace Pure.DI.UsageTests.Advanced.TagOnConstructorArgScenario {
		class AbcDependency {
			+AbcDependency()
		}
		class Composition {
		<<partial>>
		+IService Root
		}
		class ConsumerᐸStringᐳ {
			+Consumer(IDependency myDep)
		}
		class IDependency {
			<<interface>>
		}
		class IService {
			<<interface>>
		}
		class Service {
			+Service(IDependency dependency1, ConsumerᐸStringᐳ consumer)
		}
		class XyzDependency {
			+XyzDependency()
		}
	}
```

