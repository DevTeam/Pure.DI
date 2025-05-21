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
  public Task<IService<T2, bool>> GetOtherServiceAsync<T2>(CancellationToken cancellationToken)
    where T2: IDisposable
  {
    Func<IService<T2, bool>> perBlockFunc1 = new Func<IService<T2, bool>>(
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    () =>
    {
      OtherService<T2> transientOtherService6;
      IDependency<T2> localDependency122 = new Dependency<T2>();
      transientOtherService6 = new OtherService<T2>(localDependency122);
      IService<T2, bool> localValue121 = transientOtherService6;
      return localValue121;
    });
    TaskCreationOptions transientTaskCreationOptions3 = TaskCreationOptions.None;
    TaskContinuationOptions transientTaskContinuationOptions4 = TaskContinuationOptions.None;
    TaskScheduler transientTaskScheduler5 = TaskScheduler.Default;
    TaskFactory<IService<T2, bool>> perBlockTaskFactory2;
    CancellationToken localCancellationToken123 = cancellationToken;
    TaskCreationOptions localTaskCreationOptions124 = transientTaskCreationOptions3;
    TaskContinuationOptions localTaskContinuationOptions125 = transientTaskContinuationOptions4;
    TaskScheduler localTaskScheduler126 = transientTaskScheduler5;
    perBlockTaskFactory2 = new TaskFactory<IService<T2, bool>>(localCancellationToken123, localTaskCreationOptions124, localTaskContinuationOptions125, localTaskScheduler126);
    Task<IService<T2, bool>> transientTask0;
    // Injects an instance factory
    Func<IService<T2, bool>> localFactory127 = perBlockFunc1;
    // Injects a task factory creating and scheduling task objects
    TaskFactory<IService<T2, bool>> localTaskFactory128 = perBlockTaskFactory2;
    // Creates and starts a task using the instance factory
    transientTask0 = localTaskFactory128.StartNew(localFactory127);
    return transientTask0;
  }

  [MethodImpl(MethodImplOptions.AggressiveInlining)]
  public Task<IService<T2, T3>> GetMyRootAsync<T2, T3>(CancellationToken cancellationToken)
    where T2: IDisposable
    where T3: struct
  {
    Func<IService<T2, T3>> perBlockFunc1 = new Func<IService<T2, T3>>(
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    () =>
    {
      IService<T2, T3> localValue129 = new Service<T2, T3>(new Dependency<T2>());
      return localValue129;
    });
    TaskCreationOptions transientTaskCreationOptions3 = TaskCreationOptions.None;
    TaskContinuationOptions transientTaskContinuationOptions4 = TaskContinuationOptions.None;
    TaskScheduler transientTaskScheduler5 = TaskScheduler.Default;
    TaskFactory<IService<T2, T3>> perBlockTaskFactory2;
    CancellationToken localCancellationToken130 = cancellationToken;
    TaskCreationOptions localTaskCreationOptions131 = transientTaskCreationOptions3;
    TaskContinuationOptions localTaskContinuationOptions132 = transientTaskContinuationOptions4;
    TaskScheduler localTaskScheduler133 = transientTaskScheduler5;
    perBlockTaskFactory2 = new TaskFactory<IService<T2, T3>>(localCancellationToken130, localTaskCreationOptions131, localTaskContinuationOptions132, localTaskScheduler133);
    Task<IService<T2, T3>> transientTask0;
    // Injects an instance factory
    Func<IService<T2, T3>> localFactory134 = perBlockFunc1;
    // Injects a task factory creating and scheduling task objects
    TaskFactory<IService<T2, T3>> localTaskFactory135 = perBlockTaskFactory2;
    // Creates and starts a task using the instance factory
    transientTask0 = localTaskFactory135.StartNew(localFactory134);
    return transientTask0;
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
	OtherServiceᐸT2ᐳ --|> IServiceᐸT2ˏBooleanᐳ : "Other" 
	ServiceᐸT2ˏT3ᐳ --|> IServiceᐸT2ˏT3ᐳ
	DependencyᐸT2ᐳ --|> IDependencyᐸT2ᐳ
	Composition ..> TaskᐸIServiceᐸT2ˏT3ᐳᐳ : TaskᐸIServiceᐸT2ˏT3ᐳᐳ GetMyRootAsyncᐸT2ˏT3ᐳ(System.Threading.CancellationToken cancellationToken)
	TaskᐸIServiceᐸT2ˏBooleanᐳᐳ o-- "PerBlock" FuncᐸIServiceᐸT2ˏBooleanᐳᐳ : "Other"  FuncᐸIServiceᐸT2ˏBooleanᐳᐳ
	TaskᐸIServiceᐸT2ˏBooleanᐳᐳ o-- "PerBlock" TaskFactoryᐸIServiceᐸT2ˏBooleanᐳᐳ : TaskFactoryᐸIServiceᐸT2ˏBooleanᐳᐳ
	TaskᐸIServiceᐸT2ˏT3ᐳᐳ o-- "PerBlock" FuncᐸIServiceᐸT2ˏT3ᐳᐳ : FuncᐸIServiceᐸT2ˏT3ᐳᐳ
	TaskᐸIServiceᐸT2ˏT3ᐳᐳ o-- "PerBlock" TaskFactoryᐸIServiceᐸT2ˏT3ᐳᐳ : TaskFactoryᐸIServiceᐸT2ˏT3ᐳᐳ
	FuncᐸIServiceᐸT2ˏBooleanᐳᐳ *--  OtherServiceᐸT2ᐳ : "Other"  IServiceᐸT2ˏBooleanᐳ
	TaskFactoryᐸIServiceᐸT2ˏBooleanᐳᐳ *--  TaskScheduler : TaskScheduler
	TaskFactoryᐸIServiceᐸT2ˏBooleanᐳᐳ *--  TaskCreationOptions : TaskCreationOptions
	TaskFactoryᐸIServiceᐸT2ˏBooleanᐳᐳ *--  TaskContinuationOptions : TaskContinuationOptions
	TaskFactoryᐸIServiceᐸT2ˏBooleanᐳᐳ o-- CancellationToken : Argument "cancellationToken"
	FuncᐸIServiceᐸT2ˏT3ᐳᐳ *--  ServiceᐸT2ˏT3ᐳ : IServiceᐸT2ˏT3ᐳ
	TaskFactoryᐸIServiceᐸT2ˏT3ᐳᐳ *--  TaskScheduler : TaskScheduler
	TaskFactoryᐸIServiceᐸT2ˏT3ᐳᐳ *--  TaskCreationOptions : TaskCreationOptions
	TaskFactoryᐸIServiceᐸT2ˏT3ᐳᐳ *--  TaskContinuationOptions : TaskContinuationOptions
	TaskFactoryᐸIServiceᐸT2ˏT3ᐳᐳ o-- CancellationToken : Argument "cancellationToken"
	OtherServiceᐸT2ᐳ *--  DependencyᐸT2ᐳ : IDependencyᐸT2ᐳ
	ServiceᐸT2ˏT3ᐳ *--  DependencyᐸT2ᐳ : IDependencyᐸT2ᐳ
	namespace Pure.DI.UsageTests.Generics.GenericAsyncCompositionRootsWithConstraintsScenario {
		class Composition {
		<<partial>>
		+TaskᐸIServiceᐸT2ˏT3ᐳᐳ GetMyRootAsyncᐸT2ˏT3ᐳ(System.Threading.CancellationToken cancellationToken)
		+TaskᐸIServiceᐸT2ˏBooleanᐳᐳ GetOtherServiceAsyncᐸT2ᐳ(System.Threading.CancellationToken cancellationToken)
		}
		class DependencyᐸT2ᐳ {
				<<class>>
			+Dependency()
		}
		class IDependencyᐸT2ᐳ {
			<<interface>>
		}
		class IServiceᐸT2ˏBooleanᐳ {
			<<interface>>
		}
		class IServiceᐸT2ˏT3ᐳ {
			<<interface>>
		}
		class OtherServiceᐸT2ᐳ {
				<<class>>
		}
		class ServiceᐸT2ˏT3ᐳ {
				<<class>>
			+Service(IDependencyᐸT2ᐳ dependency)
		}
	}
	namespace System {
		class FuncᐸIServiceᐸT2ˏBooleanᐳᐳ {
				<<delegate>>
		}
		class FuncᐸIServiceᐸT2ˏT3ᐳᐳ {
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
		class TaskFactoryᐸIServiceᐸT2ˏBooleanᐳᐳ {
				<<class>>
		}
		class TaskFactoryᐸIServiceᐸT2ˏT3ᐳᐳ {
				<<class>>
		}
		class TaskScheduler {
				<<abstract>>
		}
		class TaskᐸIServiceᐸT2ˏBooleanᐳᐳ {
				<<class>>
		}
		class TaskᐸIServiceᐸT2ˏT3ᐳᐳ {
				<<class>>
		}
	}
```

