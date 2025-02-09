#### Generic async composition roots with constraints

> [!IMPORTANT]
> `Resolve' methods cannot be used to resolve generic composition roots.


```c#
using Pure.DI;

DI.Setup(nameof(Composition))
    // This hint indicates to not generate methods such as Resolve
    .Hint(Hint.Resolve, "Off")
    .Bind().To<Dependency<TTDisposable>>()
    .Bind().To<Service<TTDisposable, TTS>>()
    // Creates OtherService manually,
    // just for the sake of example
    .Bind("Other").To(ctx =>
    {
        ctx.Inject(out IDependency<TTDisposable> dependency);
        return new OtherService<TTDisposable>(dependency);
    })

    // Specifies to use CancellationToken from the argument
    // when resolving a composition root
    .RootArg<CancellationToken>("cancellationToken")

    // Specifies to create a regular public method
    // to get a composition root of type Task<Service<T, TStruct>>
    // with the name "GetMyRootAsync"
    .Root<Task<IService<TTDisposable, TTS>>>("GetMyRootAsync")

    // Specifies to create a regular public method
    // to get a composition root of type Task<OtherService<T>>
    // with the name "GetOtherServiceAsync"
    // using the "Other" tag
    .Root<Task<IService<TTDisposable, bool>>>("GetOtherServiceAsync", "Other");

var composition = new Composition();

// Resolves composition roots asynchronously
var service = await composition.GetMyRootAsync<Stream, double>(CancellationToken.None);
var someOtherService = await composition.GetOtherServiceAsync<BinaryReader>(CancellationToken.None);

interface IDependency<T>
    where T : IDisposable;

class Dependency<T> : IDependency<T>
    where T : IDisposable;

interface IService<T, TStruct>
    where T : IDisposable
    where TStruct : struct;

class Service<T, TStruct>(IDependency<T> dependency) : IService<T, TStruct>
    where T : IDisposable
    where TStruct : struct;

class OtherService<T>(IDependency<T> dependency) : IService<T, bool>
    where T : IDisposable;
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
- Add reference to NuGet package
  - [Pure.DI](https://www.nuget.org/packages/Pure.DI)
```bash
dotnet add package Pure.DI
```
- Copy the example code into the _Program.cs_ file

You are ready to run the example 🚀
```bash
dotnet run
```

</details>

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
  public Task<IService<T5, bool>> GetOtherServiceAsync<T5>(CancellationToken cancellationToken)
    where T5: IDisposable
  {
    TaskFactory<IService<T5, bool>> perBlockTaskFactory2;
    CancellationToken localCancellationToken80 = cancellationToken;
    TaskCreationOptions transientTaskCreationOptions3 = TaskCreationOptions.None;
    TaskCreationOptions localTaskCreationOptions81 = transientTaskCreationOptions3;
    TaskContinuationOptions transientTaskContinuationOptions4 = TaskContinuationOptions.None;
    TaskContinuationOptions localTaskContinuationOptions82 = transientTaskContinuationOptions4;
    TaskScheduler transientTaskScheduler5 = TaskScheduler.Default;
    TaskScheduler localTaskScheduler83 = transientTaskScheduler5;
    perBlockTaskFactory2 = new TaskFactory<IService<T5, bool>>(localCancellationToken80, localTaskCreationOptions81, localTaskContinuationOptions82, localTaskScheduler83);
    Func<IService<T5, bool>> perBlockFunc1 = new Func<IService<T5, bool>>(
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    () =>
    {
      OtherService<T5> transientOtherService6;
      IDependency<T5> localDependency85 = new Dependency<T5>();
      transientOtherService6 = new OtherService<T5>(localDependency85);
      IService<T5, bool> localValue84 = transientOtherService6;
      return localValue84;
    });
    Task<IService<T5, bool>> transientTask0;
    // Injects an instance factory
    Func<IService<T5, bool>> localFactory86 = perBlockFunc1;
    // Injects a task factory creating and scheduling task objects
    TaskFactory<IService<T5, bool>> localTaskFactory87 = perBlockTaskFactory2;
    // Creates and starts a task using the instance factory
    transientTask0 = localTaskFactory87.StartNew(localFactory86);
    return transientTask0;
  }

  [MethodImpl(MethodImplOptions.AggressiveInlining)]
  public Task<IService<T5, T1>> GetMyRootAsync<T5, T1>(CancellationToken cancellationToken)
    where T5: IDisposable
    where T1: struct
  {
    TaskFactory<IService<T5, T1>> perBlockTaskFactory2;
    CancellationToken localCancellationToken88 = cancellationToken;
    TaskCreationOptions transientTaskCreationOptions3 = TaskCreationOptions.None;
    TaskCreationOptions localTaskCreationOptions89 = transientTaskCreationOptions3;
    TaskContinuationOptions transientTaskContinuationOptions4 = TaskContinuationOptions.None;
    TaskContinuationOptions localTaskContinuationOptions90 = transientTaskContinuationOptions4;
    TaskScheduler transientTaskScheduler5 = TaskScheduler.Default;
    TaskScheduler localTaskScheduler91 = transientTaskScheduler5;
    perBlockTaskFactory2 = new TaskFactory<IService<T5, T1>>(localCancellationToken88, localTaskCreationOptions89, localTaskContinuationOptions90, localTaskScheduler91);
    Func<IService<T5, T1>> perBlockFunc1 = new Func<IService<T5, T1>>(
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    () =>
    {
      IService<T5, T1> localValue92 = new Service<T5, T1>(new Dependency<T5>());
      return localValue92;
    });
    Task<IService<T5, T1>> transientTask0;
    // Injects an instance factory
    Func<IService<T5, T1>> localFactory93 = perBlockFunc1;
    // Injects a task factory creating and scheduling task objects
    TaskFactory<IService<T5, T1>> localTaskFactory94 = perBlockTaskFactory2;
    // Creates and starts a task using the instance factory
    transientTask0 = localTaskFactory94.StartNew(localFactory93);
    return transientTask0;
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
	OtherServiceᐸT5ᐳ --|> IServiceᐸT5ˏBooleanᐳ : "Other" 
	ServiceᐸT5ˏT1ᐳ --|> IServiceᐸT5ˏT1ᐳ
	DependencyᐸT5ᐳ --|> IDependencyᐸT5ᐳ
	Composition ..> TaskᐸIServiceᐸT5ˏBooleanᐳᐳ : TaskᐸIServiceᐸT5ˏBooleanᐳᐳ GetOtherServiceAsyncᐸT5ᐳ(System.Threading.CancellationToken cancellationToken)
	Composition ..> TaskᐸIServiceᐸT5ˏT1ᐳᐳ : TaskᐸIServiceᐸT5ˏT1ᐳᐳ GetMyRootAsyncᐸT5ˏT1ᐳ(System.Threading.CancellationToken cancellationToken)
	TaskᐸIServiceᐸT5ˏBooleanᐳᐳ o-- "PerBlock" FuncᐸIServiceᐸT5ˏBooleanᐳᐳ : "Other"  FuncᐸIServiceᐸT5ˏBooleanᐳᐳ
	TaskᐸIServiceᐸT5ˏBooleanᐳᐳ o-- "PerBlock" TaskFactoryᐸIServiceᐸT5ˏBooleanᐳᐳ : TaskFactoryᐸIServiceᐸT5ˏBooleanᐳᐳ
	TaskᐸIServiceᐸT5ˏT1ᐳᐳ o-- "PerBlock" FuncᐸIServiceᐸT5ˏT1ᐳᐳ : FuncᐸIServiceᐸT5ˏT1ᐳᐳ
	TaskᐸIServiceᐸT5ˏT1ᐳᐳ o-- "PerBlock" TaskFactoryᐸIServiceᐸT5ˏT1ᐳᐳ : TaskFactoryᐸIServiceᐸT5ˏT1ᐳᐳ
	FuncᐸIServiceᐸT5ˏBooleanᐳᐳ *--  OtherServiceᐸT5ᐳ : "Other"  IServiceᐸT5ˏBooleanᐳ
	TaskFactoryᐸIServiceᐸT5ˏBooleanᐳᐳ o-- CancellationToken : Argument "cancellationToken"
	TaskFactoryᐸIServiceᐸT5ˏBooleanᐳᐳ *--  TaskCreationOptions : TaskCreationOptions
	TaskFactoryᐸIServiceᐸT5ˏBooleanᐳᐳ *--  TaskContinuationOptions : TaskContinuationOptions
	TaskFactoryᐸIServiceᐸT5ˏBooleanᐳᐳ *--  TaskScheduler : TaskScheduler
	FuncᐸIServiceᐸT5ˏT1ᐳᐳ *--  ServiceᐸT5ˏT1ᐳ : IServiceᐸT5ˏT1ᐳ
	TaskFactoryᐸIServiceᐸT5ˏT1ᐳᐳ o-- CancellationToken : Argument "cancellationToken"
	TaskFactoryᐸIServiceᐸT5ˏT1ᐳᐳ *--  TaskCreationOptions : TaskCreationOptions
	TaskFactoryᐸIServiceᐸT5ˏT1ᐳᐳ *--  TaskContinuationOptions : TaskContinuationOptions
	TaskFactoryᐸIServiceᐸT5ˏT1ᐳᐳ *--  TaskScheduler : TaskScheduler
	OtherServiceᐸT5ᐳ *--  DependencyᐸT5ᐳ : IDependencyᐸT5ᐳ
	ServiceᐸT5ˏT1ᐳ *--  DependencyᐸT5ᐳ : IDependencyᐸT5ᐳ
	namespace Pure.DI.UsageTests.Generics.GenericAsyncCompositionRootsWithConstraintsScenario {
		class Composition {
		<<partial>>
		+TaskᐸIServiceᐸT5ˏT1ᐳᐳ GetMyRootAsyncᐸT5ˏT1ᐳ(System.Threading.CancellationToken cancellationToken)
		+TaskᐸIServiceᐸT5ˏBooleanᐳᐳ GetOtherServiceAsyncᐸT5ᐳ(System.Threading.CancellationToken cancellationToken)
		}
		class DependencyᐸT5ᐳ {
			+Dependency()
		}
		class IDependencyᐸT5ᐳ {
			<<interface>>
		}
		class IServiceᐸT5ˏBooleanᐳ {
			<<interface>>
		}
		class IServiceᐸT5ˏT1ᐳ {
			<<interface>>
		}
		class OtherServiceᐸT5ᐳ {
		}
		class ServiceᐸT5ˏT1ᐳ {
			+Service(IDependencyᐸT5ᐳ dependency)
		}
	}
	namespace System {
		class FuncᐸIServiceᐸT5ˏBooleanᐳᐳ {
				<<delegate>>
		}
		class FuncᐸIServiceᐸT5ˏT1ᐳᐳ {
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
		class TaskFactoryᐸIServiceᐸT5ˏBooleanᐳᐳ {
		}
		class TaskFactoryᐸIServiceᐸT5ˏT1ᐳᐳ {
		}
		class TaskScheduler {
				<<abstract>>
		}
		class TaskᐸIServiceᐸT5ˏBooleanᐳᐳ {
		}
		class TaskᐸIServiceᐸT5ˏT1ᐳᐳ {
		}
	}
```

