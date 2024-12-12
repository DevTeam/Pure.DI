#### OnNewInstance hint

[![CSharp](https://img.shields.io/badge/C%23-code-blue.svg)](../tests/Pure.DI.UsageTests/Hints/OnNewInstanceHintScenario.cs)

Hints are used to fine-tune code generation. The _OnNewInstance_ hint determines whether to generate partial _OnNewInstance_ method.
In addition, setup hints can be comments before the _Setup_ method in the form ```hint = value```, for example: `// OnNewInstance = On`.


```c#
using static Hint;

interface IDependency;

class Dependency : IDependency
{
    public override string ToString() => nameof(Dependency);
}

interface IService
{
    IDependency Dependency { get; }
}

class Service(IDependency dependency) : IService
{
    public IDependency Dependency { get; } = dependency;

    public override string ToString() => nameof(Service);
}

internal partial class Composition
{
    private readonly List<string> _log = [];

    public Composition(List<string> log) : this() =>
        _log = log;

    partial void OnNewInstance<T>(
        ref T value,
        object? tag,
        Lifetime lifetime) =>
        _log.Add($"{typeof(T).Name} created");
}

DI.Setup(nameof(Composition))
    .Hint(OnNewInstance, "On")
    .Bind().As(Lifetime.Singleton).To<Dependency>()
    .Bind().To<Service>()
    .Root<IService>("Root");

var log = new List<string>();
var composition = new Composition(log);
var service1 = composition.Root;
var service2 = composition.Root;

log.ShouldBe([
    "Dependency created",
    "Service created",
    "Service created"]);
```

The `OnNewInstanceLifetimeRegularExpression` hint helps you define a set of lifetimes that require instance creation control. You can use it to specify a regular expression to filter bindings by lifetime name.
For more hints, see [this](README.md#setup-hints) page.

The following partial class will be generated:

```c#
partial class Composition
{
  private readonly Composition _root;
  private readonly Lock _lock;

  private Dependency? _singletonDependency43;

  [OrdinalAttribute(256)]
  public Composition()
  {
    _root = this;
    _lock = new Lock();
  }

  internal Composition(Composition parentScope)
  {
    _root = (parentScope ?? throw new ArgumentNullException(nameof(parentScope)))._root;
    _lock = _root._lock;
  }

  public IService Root
  {
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    get
    {
      if (_root._singletonDependency43 is null)
      {
        using (_lock.EnterScope())
        {
          if (_root._singletonDependency43 is null)
          {
            Dependency _singletonDependency43Temp;
            _singletonDependency43Temp = new Dependency();
            OnNewInstance<Dependency>(ref _singletonDependency43Temp, null, Lifetime.Singleton);
            Thread.MemoryBarrier();
            _root._singletonDependency43 = _singletonDependency43Temp;
          }
        }
      }

      Service transientService0 = new Service(_root._singletonDependency43!);
      OnNewInstance<Service>(ref transientService0, null, Lifetime.Transient);
      return transientService0;
    }
  }


  partial void OnNewInstance<T>(ref T value, object? tag, Lifetime lifetime);
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
	Composition ..> Service : IService Root
	Service o-- "Singleton" Dependency : IDependency
	namespace Pure.DI.UsageTests.Hints.OnNewInstanceHintScenario {
		class Composition {
		<<partial>>
		+IService Root
		}
		class Dependency {
			+Dependency()
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
```

