#### Async Root

[![CSharp](https://img.shields.io/badge/C%23-code-blue.svg)](../tests/Pure.DI.UsageTests/Basics/AsyncRootScenario.cs)


```c#
interface IDependency;

class Dependency : IDependency;

interface IService;

class Service(IDependency dependency) : IService;

DI.Setup(nameof(Composition))
    .Bind<IDependency>().To<Dependency>()
    .Bind<IService>().To<Service>()

    // Specifies to use CancellationToken from the argument
    // when resolving a composition root
    .RootArg<CancellationToken>("cancellationToken")

    // Composition root
    .Root<Task<IService>>("GetMyServiceAsync");

var composition = new Composition();
        
// Resolves composition roots asynchronously
var service = await composition.GetMyServiceAsync(CancellationToken.None);
```

The following partial class will be generated:

```c#
partial class Composition
{
  private readonly Composition _root;
  private readonly object _lock;

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

  [MethodImpl(MethodImplOptions.AggressiveInlining)]
  public Task<IService> GetMyServiceAsync(CancellationToken cancellationToken)
  {
    var perResolveFunc45 = default(Func<IService>);
    TaskScheduler transientTaskScheduler4 = TaskScheduler.Default;
    TaskContinuationOptions transientTaskContinuationOptions3 = TaskContinuationOptions.None;
    TaskCreationOptions transientTaskCreationOptions2 = TaskCreationOptions.None;
    TaskFactory<IService> perBlockTaskFactory1;
    {
        CancellationToken localCancellationToken10 = cancellationToken;
        TaskCreationOptions localTaskCreationOptions11 = transientTaskCreationOptions2;
        TaskContinuationOptions localTaskContinuationOptions12 = transientTaskContinuationOptions3;
        TaskScheduler localTaskScheduler13 = transientTaskScheduler4;
        perBlockTaskFactory1 = new TaskFactory<IService>(localCancellationToken10, localTaskCreationOptions11, localTaskContinuationOptions12, localTaskScheduler13);
    }

    if (perResolveFunc45 == null)
    {
        lock (_lock)
        {
            if (perResolveFunc45 == null)
            {
                perResolveFunc45 = new Func<IService>(
                [MethodImpl(MethodImplOptions.AggressiveInlining)]
                () =>
                {
                    IService localValue14 = new Service(new Dependency());
                    return localValue14;
                });
            }
        }
    }

    Task<IService> transientTask0;
    {
        Func<IService> localFactory15 = perResolveFunc45!;
        TaskFactory<IService> localTaskFactory16 = perBlockTaskFactory1;
        transientTask0 = localTaskFactory16.StartNew(localFactory15);
    }

    return transientTask0;
  }
}
```

Class diagram:

```mermaid
classDiagram
	class Composition {
		<<partial>>
		+TaskᐸIServiceᐳ GetMyServiceAsync(System.Threading.CancellationToken cancellationToken)
	}
	class TaskScheduler
	class TaskCreationOptions
	class TaskContinuationOptions
	class TaskFactory
	Dependency --|> IDependency
	class Dependency {
		+Dependency()
	}
	Service --|> IService
	class Service {
		+Service(IDependency dependency)
	}
	class CancellationToken
	class FuncᐸIServiceᐳ
	class TaskFactoryᐸIServiceᐳ
	class IDependency {
		<<interface>>
	}
	class IService {
		<<interface>>
	}
	Composition ..> TaskᐸIServiceᐳ : TaskᐸIServiceᐳ GetMyServiceAsync(System.Threading.CancellationToken cancellationToken)
	TaskFactory o-- CancellationToken : Argument "cancellationToken"
	TaskFactory *--  TaskCreationOptions : TaskCreationOptions
	TaskFactory *--  TaskContinuationOptions : TaskContinuationOptions
	TaskFactory *--  TaskScheduler : TaskScheduler
	Service *--  Dependency : IDependency
	TaskᐸIServiceᐳ o-- "PerResolve" FuncᐸIServiceᐳ : FuncᐸIServiceᐳ
	TaskᐸIServiceᐳ o-- "PerBlock" TaskFactoryᐸIServiceᐳ : TaskFactoryᐸIServiceᐳ
	FuncᐸIServiceᐳ *--  Service : IService
	TaskFactoryᐸIServiceᐳ o-- CancellationToken : Argument "cancellationToken"
	TaskFactoryᐸIServiceᐳ *--  TaskCreationOptions : TaskCreationOptions
	TaskFactoryᐸIServiceᐳ *--  TaskContinuationOptions : TaskContinuationOptions
	TaskFactoryᐸIServiceᐳ *--  TaskScheduler : TaskScheduler
```

