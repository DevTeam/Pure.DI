#### Task

By default, tasks are started automatically when they are injected. It is recommended to use an argument of type <c>CancellationToken</c> to the composition root to be able to cancel the execution of a task. In this case, the composition root property is automatically converted to a method with a parameter of type <c>CancellationToken</c>. To start a task, an instance of type <c>TaskFactory<T></c> is used, with default settings:

- CancellationToken.None
- TaskScheduler.Default
- TaskCreationOptions.None
- TaskContinuationOptions.None

But you can always override them, as in the example below for <c>TaskScheduler.Current</c>.


```c#
using Shouldly;
using Pure.DI;

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
```

<details>
<summary>Running this code sample locally</summary>

- Make sure you have the [.NET SDK 9.0](https://dotnet.microsoft.com/en-us/download/dotnet/9.0) or later is installed
```bash
dotnet --list-sdk
```
- Create a net9.0 (or later) console application
```bash
dotnet new console -n Sample
```
- Add references to NuGet packages
  - [Pure.DI](https://www.nuget.org/packages/Pure.DI)
  - [Shouldly](https://www.nuget.org/packages/Shouldly)
```bash
dotnet add package Pure.DI
dotnet add package Shouldly
```
- Copy the example code into the _Program.cs_ file

You are ready to run the example 🚀
```bash
dotnet run
```

</details>

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
  public IService GetRoot(CancellationToken cancellationToken)
  {
    Func<IDependency> perBlockFunc2 = new Func<IDependency>(
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    () =>
    {
      IDependency localValue118 = new Dependency();
      return localValue118;
    });
    TaskCreationOptions transientTaskCreationOptions4 = TaskCreationOptions.None;
    TaskContinuationOptions transientTaskContinuationOptions5 = TaskContinuationOptions.None;
    TaskScheduler transientTaskScheduler6 = TaskScheduler.Current;
    TaskFactory<IDependency> perBlockTaskFactory3;
    CancellationToken localCancellationToken119 = cancellationToken;
    TaskCreationOptions localTaskCreationOptions120 = transientTaskCreationOptions4;
    TaskContinuationOptions localTaskContinuationOptions121 = transientTaskContinuationOptions5;
    TaskScheduler localTaskScheduler122 = transientTaskScheduler6;
    perBlockTaskFactory3 = new TaskFactory<IDependency>(localCancellationToken119, localTaskCreationOptions120, localTaskContinuationOptions121, localTaskScheduler122);
    Task<IDependency> transientTask1; // Injects an instance factory
    Func<IDependency> localFactory123 = perBlockFunc2;
    // Injects a task factory creating and scheduling task objects
    TaskFactory<IDependency> localTaskFactory124 = perBlockTaskFactory3;
    // Creates and starts a task using the instance factory
    transientTask1 = localTaskFactory124.StartNew(localFactory123);
    return new Service(transientTask1);
  }
}
```

Class diagram:

```mermaid
---
 config:
  maxTextSize: 2147483647
  maxEdges: 2147483647
  class:
   hideEmptyMembersBox: true
---
classDiagram
	Dependency --|> IDependency
	Service --|> IService
	Composition ..> Service : IService GetRoot(System.Threading.CancellationToken cancellationToken)
	Service *--  TaskᐸIDependencyᐳ : TaskᐸIDependencyᐳ
	TaskᐸIDependencyᐳ o-- "PerBlock" FuncᐸIDependencyᐳ : FuncᐸIDependencyᐳ
	TaskᐸIDependencyᐳ o-- "PerBlock" TaskFactoryᐸIDependencyᐳ : TaskFactoryᐸIDependencyᐳ
	FuncᐸIDependencyᐳ *--  Dependency : IDependency
	TaskFactoryᐸIDependencyᐳ *--  TaskCreationOptions : TaskCreationOptions
	TaskFactoryᐸIDependencyᐳ *--  TaskContinuationOptions : TaskContinuationOptions
	TaskFactoryᐸIDependencyᐳ *--  TaskScheduler : TaskScheduler
	TaskFactoryᐸIDependencyᐳ o-- CancellationToken : Argument "cancellationToken"
	namespace Pure.DI.UsageTests.BCL.TaskScenario {
		class Composition {
		<<partial>>
		+IService GetRoot(System.Threading.CancellationToken cancellationToken)
		}
		class Dependency {
				<<class>>
			+Dependency()
		}
		class IDependency {
			<<interface>>
		}
		class IService {
			<<interface>>
		}
		class Service {
				<<class>>
			+Service(TaskᐸIDependencyᐳ dependencyTask)
		}
	}
	namespace System {
		class FuncᐸIDependencyᐳ {
				<<delegate>>
		}
	}
	namespace System.Threading {
		class CancellationToken {
				<<struct>>
		}
	}
	namespace System.Threading.Tasks {
		class TaskContinuationOptions {
				<<enum>>
		}
		class TaskCreationOptions {
				<<enum>>
		}
		class TaskFactoryᐸIDependencyᐳ {
				<<class>>
		}
		class TaskScheduler {
				<<abstract>>
		}
		class TaskᐸIDependencyᐳ {
				<<class>>
		}
	}
```

