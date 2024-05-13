#### Required properties or fields

[![CSharp](https://img.shields.io/badge/C%23-code-blue.svg)](../tests/Pure.DI.UsageTests/Basics/RequiredPropertiesOrFieldsScenario.cs)

All properties or fields marked with the _required_ keyword automatically become injected dependencies.


```c#
interface IDependency;

class Dependency : IDependency;

interface IService
{
    string Name { get;}

    IDependency Dependency { get;}
}

class Service : IService
{
    public required string ServiceNameField;

    public string Name => ServiceNameField;

    // The required property will be injected automatically
    // without additional effort
    public required IDependency Dependency { get; init; }
}

DI.Setup(nameof(Composition))
    .Arg<string>("name")
    .Bind<IDependency>().To<Dependency>()
    .Bind<IService>().To<Service>()

    // Composition root
    .Root<IService>("Root");

var composition = new Composition(name: "My Service");
var service = composition.Root;
service.Dependency.ShouldBeOfType<Dependency>();
service.Name.ShouldBe("My Service");
```

The following partial class will be generated:

```c#
partial class Composition
{
  private readonly Composition _root;

  private readonly string _arg_name;

  public Composition(string name)
  {
    _arg_name = name ?? throw new ArgumentNullException(nameof(name));
    _root = this;
  }

  internal Composition(Composition parentScope)
  {
    _root = (parentScope ?? throw new ArgumentNullException(nameof(parentScope)))._root;
    _arg_name = _root._arg_name;
  }

  public IService Root
  {
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    get
    {
      return new Service()
      {
          ServiceNameField = _arg_name,
          Dependency = new Dependency()
      };
    }
  }
}
```

Class diagram:

```mermaid
classDiagram
	class Composition {
		<<partial>>
		+IService Root
	}
	class String
	Dependency --|> IDependency : 
	class Dependency {
		+Dependency()
	}
	Service --|> IService : 
	class Service {
		+Service()
		+String ServiceNameField
		+IDependency Dependency
	}
	class IDependency {
		<<interface>>
	}
	class IService {
		<<interface>>
	}
	Service o-- String : Argument "name"
	Service *--  Dependency : IDependency
	Composition ..> Service : IService Root
```

