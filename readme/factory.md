#### Factory

[![CSharp](https://img.shields.io/badge/C%23-code-blue.svg)](../tests/Pure.DI.UsageTests/Basics/FactoryScenario.cs)

This example demonstrates how to create and initialize an instance manually.
At the compilation stage, the set of dependencies that an object needs in order to be created is determined. In most cases, this happens automatically according to the set of constructors and their arguments and does not require any additional customization efforts. But sometimes it is necessary to manually create an object, as in lines of code:


```c#
interface IDependency
{
    DateTimeOffset Time { get; }

    bool IsInitialized { get; }
}

class Dependency(DateTimeOffset time) : IDependency
{
    public DateTimeOffset Time { get; } = time;

    public bool IsInitialized { get; private set; }

    public void Initialize() => IsInitialized = true;
}

class FakeDependency : IDependency
{
    public DateTimeOffset Time => DateTimeOffset.MinValue;

    public bool IsInitialized => true;
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
    .Bind().To(_ => DateTimeOffset.Now)
    .RootArg<bool>("isFake", "FakeArgTag")
    .Bind<IDependency>().To<IDependency>(ctx =>
    {
        // When building a composition of objects,
        // all of this code will be outside the lambda function.

        // Some custom logic for creating an instance.
        // For example, here's how you can inject and initialize
        // an instance of a particular type:

        ctx.Inject<bool>("FakeArgTag", out var isFake);
        if (isFake)
        {
            return new FakeDependency();
        }

        ctx.Inject(out Dependency dependency);
        dependency.Initialize();
        return dependency;

    })
    .Bind<IService>().To<Service>()

    // Composition root
    .Root<IService>("GetMyService");

var composition = new Composition();

var service = composition.GetMyService(isFake: false);
service.Dependency.ShouldBeOfType<Dependency>();
service.Dependency.IsInitialized.ShouldBeTrue();
        
var serviceWithFakeDependency = composition.GetMyService(isFake: true);
serviceWithFakeDependency.Dependency.ShouldBeOfType<FakeDependency>();
```

This approach is more expensive to maintain, but allows you to create objects more flexibly by passing them some state and introducing dependencies. As in the case of automatic dependency injecting, objects give up control on embedding, and the whole process takes place when the object graph is created.
> [!IMPORTANT]
> The method `Inject()`cannot be used outside of the binding setup.

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
  public IService GetMyService(bool isFake)
  {
    DateTimeOffset transientDateTimeOffset3 = DateTimeOffset.Now;
    IDependency transientIDependency1;
    // When building a composition of objects,
    // all of this code will be outside the lambda function.
    // Some custom logic for creating an instance.
    // For example, here's how you can inject and initialize
    // an instance of a particular type:
      bool localIsFake40 = isFake;
    if (localIsFake40)
    {
      {transientIDependency1 = new FakeDependency();
    goto transientIDependency1Finish; }
    }
      Dependency localDependency41 = new Dependency(transientDateTimeOffset3);
    localDependency41.Initialize();
    transientIDependency1 = localDependency41;
    transientIDependency1Finish:;
    return new Service(transientIDependency1);
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
	Composition ..> Service : IService GetMyService(bool isFake)
	Service *--  IDependency : IDependency
	IDependency o-- Boolean : "FakeArgTag"  Argument "isFake"
	IDependency *--  Dependency : Dependency
	Dependency *--  DateTimeOffset : DateTimeOffset
	namespace Pure.DI.UsageTests.Basics.FactoryScenario {
		class Composition {
		<<partial>>
		+IService GetMyService(bool isFake)
		}
		class Dependency {
			+Dependency(DateTimeOffset time)
		}
		class IDependency {
				<<interface>>
		}
		class IService {
			<<interface>>
		}
		class Service {
			+Service(IDependency dependency)
		}
	}
	namespace System {
		class Boolean {
				<<struct>>
		}
		class DateTimeOffset {
				<<struct>>
		}
	}
```

