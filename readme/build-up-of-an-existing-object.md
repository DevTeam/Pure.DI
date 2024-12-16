#### Build up of an existing object

[![CSharp](https://img.shields.io/badge/C%23-code-blue.svg)](../tests/Pure.DI.UsageTests/Basics/BuildUpScenario.cs)

In other words, injecting the necessary dependencies via methods, properties, or fields into an existing object.


```c#
interface IDependency
{
    string Name { get; }

    Guid Id { get; }
}

class Dependency : IDependency
{
    // The Ordinal attribute specifies to perform an injection and its order
    [Ordinal(1)]
    public string Name { get; set; } = "";

    public Guid Id { get; private set; } = Guid.Empty;

    // The Ordinal attribute specifies to perform an injection and its order
    [Ordinal(0)]
    public void SetId(Guid id) => Id = id;
}

interface IService
{
    IDependency Dependency { get; }
}

record Service(IDependency Dependency) : IService;

DI.Setup(nameof(Composition))
    .RootArg<string>("name")
    .Bind().To(_ => Guid.NewGuid())
    .Bind().To(ctx =>
    {
        var dependency = new Dependency();
        ctx.BuildUp(dependency);
        return dependency;
    })
    .Bind().To<Service>()

    // Composition root
    .Root<IService>("GetMyService");

var composition = new Composition();
var service = composition.GetMyService("Some name");
service.Dependency.Name.ShouldBe("Some name");
service.Dependency.Id.ShouldNotBe(Guid.Empty);
```

The following partial class will be generated:

```c#
partial class Composition
{
  private readonly Composition _root;

  [OrdinalAttribute(128)]
  public Composition()
  {
    _root = this;
  }

  internal Composition(Composition parentScope)
  {
    _root = (parentScope ?? throw new ArgumentNullException(nameof(parentScope)))._root;
  }

  [MethodImpl(MethodImplOptions.AggressiveInlining)]
  public IService GetMyService(string name)
  {
    Guid transientGuid2 = Guid.NewGuid();
    Dependency transientDependency1;
    var localDependency45= new Dependency();
    localDependency45.SetId(transientGuid2);
    localDependency45.Name = name;
    transientDependency1 = localDependency45;
    return new Service(transientDependency1);
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
	Service --|> IEquatableᐸServiceᐳ
	Dependency --|> IDependency
	Composition ..> Service : IService GetMyService(string name)
	Service *--  Dependency : IDependency
	Dependency o-- String : Argument "name"
	Dependency *--  Guid : Guid
	namespace Pure.DI.UsageTests.Basics.BuildUpScenario {
		class Composition {
		<<partial>>
		+IService GetMyService(string name)
		}
		class Dependency {
			+String Name
			+SetId(Guid id) : Void
		}
		class IDependency {
			<<interface>>
		}
		class IService {
			<<interface>>
		}
		class Service {
				<<record>>
			+Service(IDependency Dependency)
		}
	}
	namespace System {
		class Guid {
				<<struct>>
		}
		class IEquatableᐸServiceᐳ {
			<<interface>>
		}
		class String {
		}
	}
```

