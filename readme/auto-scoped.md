#### Auto scoped

[![CSharp](https://img.shields.io/badge/C%23-code-blue.svg)](../tests/Pure.DI.UsageTests/Lifetimes/AutoScopedScenario.cs)

You can use the following example to automatically create a session when creating instances of a particular type:


```c#
interface IDependency;

class Dependency : IDependency;

interface IService
{
    IDependency Dependency { get; }
}

class Service(IDependency dependency) : IService
{
    public IDependency Dependency => dependency;
}

// Implements a session
class Program(Func<IService> serviceFactory)
{
    public IService CreateService() => serviceFactory();
}

partial class Composition
{
    void Setup() =>
        DI.Setup(nameof(Composition))
            // This hint indicates to not generate methods such as Resolve
            .Hint(Hint.Resolve, "Off")
            .Bind().As(Scoped).To<Dependency>()
            // Session composition root
            .Root<Service>("SessionRoot", kind: RootKinds.Private)
            // Auto scoped
            .Bind().To<IService>(ctx =>
            {
                // Injects a base composition
                ctx.Inject(out Composition baseComposition);

                // Creates a session
                var session = new Composition(baseComposition);

                return session.SessionRoot;
            })

            // Program composition root
            .Root<Program>("ProgramRoot");
}

var composition = new Composition();
var program = composition.ProgramRoot;
        
// Creates service in session #1
var service1 = program.CreateService();
        
// Creates service in session #2
var service2 = program.CreateService();
        
// Checks that the scoped instances are not identical in different sessions
service1.Dependency.ShouldNotBe(service2.Dependency);
```

The following partial class will be generated:

```c#
partial class Composition
{
  private readonly Composition _root;
  private readonly object _lock;

  private Dependency? _scopedDependency36;

  public Composition()
  {
    _root = this;
    _lock = new object();
  }

  internal Composition(Composition parentScope)
  {
    _root = (parentScope ?? throw new ArgumentNullException(nameof(parentScope)))._root;
    _lock = _root._lock;
  }

  private Service SessionRoot
  {
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    get
    {
      if (_scopedDependency36 == null)
      {
          lock (_lock)
          {
              if (_scopedDependency36 == null)
              {
                  _scopedDependency36 = new Dependency();
              }
          }
      }

      return new Service(_scopedDependency36!);
    }
  }

  public Program ProgramRoot
  {
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    get
    {
      var perResolveFunc43 = default(Func<IService>);
      if (perResolveFunc43 == null)
      {
          lock (_lock)
          {
              if (perResolveFunc43 == null)
              {
                  perResolveFunc43 = new Func<IService>(
                  [MethodImpl(MethodImplOptions.AggressiveInlining)]
                  () =>
                  {
                      Composition transientComposition2 = this;
                      IService transientIService1;
                      {
                          var localBaseComposition31 = transientComposition2;
                          // Creates a session
                          var localSession32 = new Composition(localBaseComposition31);
                          transientIService1 = localSession32.SessionRoot;
                      }

                      var localValue30 = transientIService1;
                      return localValue30;
                  });
              }
          }
      }

      return new Program(perResolveFunc43!);
    }
  }
}
```

Class diagram:

```mermaid
classDiagram
	class Composition {
		<<partial>>
		+Program ProgramRoot
		+Service SessionRoot
	}
	class Program {
		+Program(FuncᐸIServiceᐳ serviceFactory)
	}
	class Service {
		+Service(IDependency dependency)
	}
	class IService
	Dependency --|> IDependency
	class Dependency {
		+Dependency()
	}
	class FuncᐸIServiceᐳ
	class Composition
	class IDependency {
		<<interface>>
	}
	Program o-- "PerResolve" FuncᐸIServiceᐳ : FuncᐸIServiceᐳ
	Service o-- "Scoped" Dependency : IDependency
	IService *--  Composition : Composition
	Composition ..> Service : Service SessionRoot
	Composition ..> Program : Program ProgramRoot
	FuncᐸIServiceᐳ *--  IService : IService
```

