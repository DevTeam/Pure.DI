#### Root arguments

[![CSharp](https://img.shields.io/badge/C%23-code-blue.svg)](../tests/Pure.DI.UsageTests/Basics/RootArgumentsScenario.cs)

Sometimes it is necessary to pass some state to the composition to use it when resolving dependencies. To do this, just use the `RootArg<T>(string argName)` method, specify the type of argument and its name. You can also specify a tag for each argument. You can then use them as dependencies when building the object graph. If you have multiple arguments of the same type, just use tags to distinguish them. The root of a composition that uses at least one root argument is prepended as a method, not a property. It is important to remember that the method will only display arguments that are used in the object graph of that composition root. Arguments that are not involved will not be added to the method arguments. It is best to use unique argument names so that there are no collisions.
> [!NOTE]
> Actually, root arguments work like normal bindings. The difference is that they bind to the values of the arguments. These values will be injected wherever they are required.



```c#
interface IDependency
{
    int Id { get; }

    public string DependencyName { get; }
}

class Dependency(int id, string dependencyName) : IDependency
{
    public int Id { get; } = id;

    public string DependencyName { get; } = dependencyName;
}

interface IService
{
    string Name { get; }

    IDependency Dependency { get; }
}

class Service(
    [Tag("forService")] string name,
    IDependency dependency)
    : IService
{
    public string Name { get; } = name;

    public IDependency Dependency { get; } = dependency;
}

DI.Setup(nameof(Composition))
    // This hint indicates to not generate methods such as Resolve
    .Hint(Hint.Resolve, "Off")
    .Bind<IDependency>().To<Dependency>()
    .Bind<IService>().To<Service>()

    // Some argument
    .RootArg<int>("id")
    .RootArg<string>("dependencyName")

    // An argument can be tagged (e.g., tag "forService")
    // to be injectable by type and this tag
    .RootArg<string>("serviceName", "forService")

    // Composition root
    .Root<IService>("CreateServiceWithArgs");

var composition = new Composition();

// service = new Service("Abc", new Dependency(123, "dependency 123"));
var service = composition.CreateServiceWithArgs(serviceName: "Abc", id: 123, dependencyName: "dependency 123");

service.Name.ShouldBe("Abc");
service.Dependency.Id.ShouldBe(123);
service.Dependency.DependencyName.ShouldBe("dependency 123");
```

When using composition root arguments, compilation warnings are shown if `Resolve` methods are generated, since these methods will not be able to create these roots. You can disable the creation of `Resolve` methods using the `Hint(Hint.Resolve, "Off")` hint, or ignore them but remember the risks of using `Resolve` methods.

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
  public IService CreateServiceWithArgs(int id, string dependencyName, string serviceName)
  {
    return new Service(serviceName, new Dependency(id, dependencyName));
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
	Dependency --|> IDependency
	Composition ..> Service : IService CreateServiceWithArgs(int id, string dependencyName, string serviceName)
	Service o-- String : "forService"  Argument "serviceName"
	Service *--  Dependency : IDependency
	Dependency o-- Int32 : Argument "id"
	Dependency o-- String : Argument "dependencyName"
	namespace Pure.DI.UsageTests.Basics.RootArgumentsScenario {
		class Composition {
		<<partial>>
		+IService CreateServiceWithArgs(int id, string dependencyName, string serviceName)
		}
		class Dependency {
			+Dependency(Int32 id, String dependencyName)
		}
		class IDependency {
			<<interface>>
		}
		class IService {
			<<interface>>
		}
		class Service {
			+Service(String name, IDependency dependency)
		}
	}
	namespace System {
		class Int32 {
				<<struct>>
		}
		class String {
		}
	}
```

