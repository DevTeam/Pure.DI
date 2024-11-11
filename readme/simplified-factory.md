#### Simplified factory

[![CSharp](https://img.shields.io/badge/C%23-code-blue.svg)](../tests/Pure.DI.UsageTests/Basics/SimplifiedFactoryScenario.cs)

This example shows how to create and initialize an instance manually in a simplified form. When you use a lambda function to specify custom instance initialization logic, each parameter of that function represents an injection of a dependency. Starting with C# 10, you can also put the `Tag(...)` attribute in front of the parameter to specify the tag of the injected dependency.


```c#
interface IDependency
{
    DateTimeOffset Time { get; }

    bool IsInitialized { get; }
}

class Dependency : IDependency
{
    public DateTimeOffset Time { get; private set; }

    public bool IsInitialized { get; private set; }

    public void Initialize(DateTimeOffset time)
    {
        Time = time;
        IsInitialized = true;
    }
}

interface IService
{
    IDependency Dependency { get; }
}

class Service(IDependency dependency) : IService
{
    public IDependency Dependency { get; } = dependency;
}

DI.Setup(nameof(Composition))
    .Bind("now datetime").To(_ => DateTimeOffset.Now)
    // Injects Dependency and DateTimeOffset instances
    // and performs further initialization logic
    // defined in the lambda function
    .Bind<IDependency>().To((
        Dependency dependency,
        [Tag("now datetime")] DateTimeOffset time) =>
    {
        dependency.Initialize(time);
        return dependency;
    })
    .Bind().To<Service>()

    // Composition root
    .Root<IService>("MyService");

var composition = new Composition();
var service = composition.MyService;
service.Dependency.IsInitialized.ShouldBeTrue();
```

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

  public IService MyService
  {
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    get
    {
      DateTimeOffset transientDateTimeOffset3 = DateTimeOffset.Now;
      Dependency transientDependency1;
      Dependency localDependency29 = new Dependency();
      DateTimeOffset localTime30 = transientDateTimeOffset3;
      localDependency29.Initialize(localTime30);
      transientDependency1 = localDependency29;
      return new Service(transientDependency1);
    }
  }
}
```

Class diagram:

```mermaid
classDiagram
	class Composition {
		<<partial>>
		+IService MyService
	}
	class Dependency {
		+Dependency()
	}
	class DateTimeOffset
	Service --|> IService
	class Service {
		+Service(IDependency dependency)
	}
	class IService {
		<<interface>>
	}
	Composition ..> Service : IService MyService
	Dependency *--  DateTimeOffset : "now datetime"  DateTimeOffset
	Service *--  Dependency : IDependency
```

