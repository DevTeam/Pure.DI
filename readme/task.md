#### Task

[![CSharp](https://img.shields.io/badge/C%23-code-blue.svg)](../tests/Pure.DI.UsageTests/BaseClassLibrary/TaskScenario.cs)

By default, tasks are started automatically when they are injected. It is recommended to use an argument of type <c>CancellationToken</c> to the composition root to be able to cancel the execution of a task. In this case, the composition root property is automatically converted to a method with a parameter of type <c>CancellationToken</c>. To start a task, an instance of type <c>TaskFactory<T></c> is used, with default settings:

- CancellationToken.None
- TaskScheduler.Default
- TaskCreationOptions.None
- TaskContinuationOptions.None

But you can always override them, as in the example below for <c>TaskScheduler.Current</c>.


```c#
interface IDependency
{
    ValueTask DoSomething(CancellationToken cancellationToken);
}

class Dependency : IDependency
{
    public ValueTask DoSomething(CancellationToken cancellationToken) => ValueTask.CompletedTask;
}

interface IService
{
    Task RunAsync(CancellationToken cancellationToken);
}

class Service(Task<IDependency> dependencyTask) : IService
{
    public async Task RunAsync(CancellationToken cancellationToken)
    {
        var dependency = await dependencyTask;
        await dependency.DoSomething(cancellationToken);
    }
}

DI.Setup(nameof(Composition))
    .Hint(Hint.Resolve, "Off")
    // Overrides TaskScheduler.Default if necessary
    .Bind<TaskScheduler>().To(_ => TaskScheduler.Current)
    // Specifies to use CancellationToken from the composition root argument,
    // if not specified then CancellationToken.None will be used
    .RootArg<CancellationToken>("cancellationToken")
    .Bind<IDependency>().To<Dependency>()
    .Bind<IService>().To<Service>()

    // Composition root
    .Root<IService>("GetRoot");

var composition = new Composition();
using var cancellationTokenSource = new CancellationTokenSource();

// Creates a composition root with the CancellationToken passed to it
var service = composition.GetRoot(cancellationTokenSource.Token);
await service.RunAsync(cancellationTokenSource.Token);
```

The following partial class will be generated:

```c#
partial class Composition
{
  private readonly Composition _root;
  private readonly object _lock;

  [OrdinalAttribute(10)]
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
  public IService GetRoot(CancellationToken cancellationToken)
  {
    var perResolveFunc45 = default(Func<IDependency>);
    TaskScheduler transientTaskScheduler5 = TaskScheduler.Current;
    TaskContinuationOptions transientTaskContinuationOptions4 = TaskContinuationOptions.None;
    TaskCreationOptions transientTaskCreationOptions3 = TaskCreationOptions.None;
    TaskFactory<IDependency> perBlockTaskFactory2;
    CancellationToken localCancellationToken38 = cancellationToken;
    TaskCreationOptions localTaskCreationOptions39 = transientTaskCreationOptions3;
    TaskContinuationOptions localTaskContinuationOptions40 = transientTaskContinuationOptions4;
    TaskScheduler localTaskScheduler41 = transientTaskScheduler5;
    perBlockTaskFactory2 = new TaskFactory<IDependency>(localCancellationToken38, localTaskCreationOptions39, localTaskContinuationOptions40, localTaskScheduler41);
    if (perResolveFunc45 == null)
    {
      lock (_lock)
      {
        if (perResolveFunc45 == null)
        {
          perResolveFunc45 = new Func<IDependency>([MethodImpl(MethodImplOptions.AggressiveInlining)] () =>
          {
            IDependency localValue42 = new Dependency();
            return localValue42;
          });
        }
      }
    }

    Task<IDependency> transientTask1;
    Func<IDependency> localFactory43 = perResolveFunc45!;
    TaskFactory<IDependency> localTaskFactory44 = perBlockTaskFactory2;
    transientTask1 = localTaskFactory44.StartNew(localFactory43);
    return new Service(transientTask1);
  }
}
```

Class diagram:

```mermaid
classDiagram
	class Composition {
		<<partial>>
		+IService GetRoot(global::System.Threading.CancellationToken cancellationToken)
	}
	class TaskCreationOptions
	class TaskContinuationOptions
	class TaskFactory
	class TaskScheduler
	class CancellationToken
	Dependency --|> IDependency
	class Dependency {
		+Dependency()
	}
	Service --|> IService
	class Service {
		+Service(TaskᐸIDependencyᐳ dependencyTask)
	}
	class TaskᐸIDependencyᐳ
	class FuncᐸIDependencyᐳ
	class TaskFactoryᐸIDependencyᐳ
	class IDependency {
		<<interface>>
	}
	class IService {
		<<interface>>
	}
	Composition ..> Service : IService GetRoot(global::System.Threading.CancellationToken cancellationToken)
	TaskFactory o-- CancellationToken : Argument "cancellationToken"
	TaskFactory *--  TaskCreationOptions : TaskCreationOptions
	TaskFactory *--  TaskContinuationOptions : TaskContinuationOptions
	TaskFactory *--  TaskScheduler : TaskScheduler
	Service *--  TaskᐸIDependencyᐳ : TaskᐸIDependencyᐳ
	TaskᐸIDependencyᐳ o-- "PerResolve" FuncᐸIDependencyᐳ : FuncᐸIDependencyᐳ
	TaskᐸIDependencyᐳ o-- "PerBlock" TaskFactoryᐸIDependencyᐳ : TaskFactoryᐸIDependencyᐳ
	FuncᐸIDependencyᐳ *--  Dependency : IDependency
	TaskFactoryᐸIDependencyᐳ o-- CancellationToken : Argument "cancellationToken"
	TaskFactoryᐸIDependencyᐳ *--  TaskCreationOptions : TaskCreationOptions
	TaskFactoryᐸIDependencyᐳ *--  TaskContinuationOptions : TaskContinuationOptions
	TaskFactoryᐸIDependencyᐳ *--  TaskScheduler : TaskScheduler
```

