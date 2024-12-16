#### Generic async composition roots with constraints

[![CSharp](https://img.shields.io/badge/C%23-code-blue.svg)](../tests/Pure.DI.UsageTests/Generics/GenericAsyncCompositionRootsWithConstraintsScenario.cs)

> [!IMPORTANT]
> `Resolve' methods cannot be used to resolve generic composition roots.


```c#
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
```

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
    TaskFactory<IService<T2, bool>> perBlockTaskFactory2;
    CancellationToken localCancellationToken67 = cancellationToken;
    TaskCreationOptions transientTaskCreationOptions3 = TaskCreationOptions.None;
    TaskCreationOptions localTaskCreationOptions68 = transientTaskCreationOptions3;
    TaskContinuationOptions transientTaskContinuationOptions4 = TaskContinuationOptions.None;
    TaskContinuationOptions localTaskContinuationOptions69 = transientTaskContinuationOptions4;
    TaskScheduler transientTaskScheduler5 = TaskScheduler.Default;
    TaskScheduler localTaskScheduler70 = transientTaskScheduler5;
    perBlockTaskFactory2 = new TaskFactory<IService<T2, bool>>(localCancellationToken67, localTaskCreationOptions68, localTaskContinuationOptions69, localTaskScheduler70);
    Func<IService<T2, bool>> perBlockFunc1 = new Func<IService<T2, bool>>([MethodImpl(MethodImplOptions.AggressiveInlining)] () =>
    {
      OtherService<T2> transientOtherService6;
      IDependency<T2> localDependency72 = new Dependency<T2>();
      transientOtherService6 = new OtherService<T2>(localDependency72);
      IService<T2, bool> localValue71 = transientOtherService6;
      return localValue71;
    });
    Task<IService<T2, bool>> transientTask0;
    // Injects an instance factory
    Func<IService<T2, bool>> localFactory73 = perBlockFunc1;
    // Injects a task factory creating and scheduling task objects
    TaskFactory<IService<T2, bool>> localTaskFactory74 = perBlockTaskFactory2;
    // Creates and starts a task using the instance factory
    transientTask0 = localTaskFactory74.StartNew(localFactory73);
    return transientTask0;
  }

  [MethodImpl(MethodImplOptions.AggressiveInlining)]
  public Task<IService<T2, T>> GetMyRootAsync<T2, T>(CancellationToken cancellationToken)
    where T2: IDisposable
    where T: struct
  {
    TaskFactory<IService<T2, T>> perBlockTaskFactory2;
    CancellationToken localCancellationToken75 = cancellationToken;
    TaskCreationOptions transientTaskCreationOptions3 = TaskCreationOptions.None;
    TaskCreationOptions localTaskCreationOptions76 = transientTaskCreationOptions3;
    TaskContinuationOptions transientTaskContinuationOptions4 = TaskContinuationOptions.None;
    TaskContinuationOptions localTaskContinuationOptions77 = transientTaskContinuationOptions4;
    TaskScheduler transientTaskScheduler5 = TaskScheduler.Default;
    TaskScheduler localTaskScheduler78 = transientTaskScheduler5;
    perBlockTaskFactory2 = new TaskFactory<IService<T2, T>>(localCancellationToken75, localTaskCreationOptions76, localTaskContinuationOptions77, localTaskScheduler78);
    Func<IService<T2, T>> perBlockFunc1 = new Func<IService<T2, T>>([MethodImpl(MethodImplOptions.AggressiveInlining)] () =>
    {
      IService<T2, T> localValue79 = new Service<T2, T>(new Dependency<T2>());
      return localValue79;
    });
    Task<IService<T2, T>> transientTask0;
    // Injects an instance factory
    Func<IService<T2, T>> localFactory80 = perBlockFunc1;
    // Injects a task factory creating and scheduling task objects
    TaskFactory<IService<T2, T>> localTaskFactory81 = perBlockTaskFactory2;
    // Creates and starts a task using the instance factory
    transientTask0 = localTaskFactory81.StartNew(localFactory80);
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
	OtherServiceᐸT2ᐳ --|> IServiceᐸT2ˏBooleanᐳ : "Other" 
	ServiceᐸT2ˏTᐳ --|> IServiceᐸT2ˏTᐳ
	DependencyᐸT2ᐳ --|> IDependencyᐸT2ᐳ
	Composition ..> TaskᐸIServiceᐸT2ˏBooleanᐳᐳ : TaskᐸIServiceᐸT2ˏBooleanᐳᐳ GetOtherServiceAsyncᐸT2ᐳ(System.Threading.CancellationToken cancellationToken)
	Composition ..> TaskᐸIServiceᐸT2ˏTᐳᐳ : TaskᐸIServiceᐸT2ˏTᐳᐳ GetMyRootAsyncᐸT2ˏTᐳ(System.Threading.CancellationToken cancellationToken)
	TaskᐸIServiceᐸT2ˏBooleanᐳᐳ o-- "PerBlock" FuncᐸIServiceᐸT2ˏBooleanᐳᐳ : "Other"  FuncᐸIServiceᐸT2ˏBooleanᐳᐳ
	TaskᐸIServiceᐸT2ˏBooleanᐳᐳ o-- "PerBlock" TaskFactoryᐸIServiceᐸT2ˏBooleanᐳᐳ : TaskFactoryᐸIServiceᐸT2ˏBooleanᐳᐳ
	TaskᐸIServiceᐸT2ˏTᐳᐳ o-- "PerBlock" FuncᐸIServiceᐸT2ˏTᐳᐳ : FuncᐸIServiceᐸT2ˏTᐳᐳ
	TaskᐸIServiceᐸT2ˏTᐳᐳ o-- "PerBlock" TaskFactoryᐸIServiceᐸT2ˏTᐳᐳ : TaskFactoryᐸIServiceᐸT2ˏTᐳᐳ
	FuncᐸIServiceᐸT2ˏBooleanᐳᐳ *--  OtherServiceᐸT2ᐳ : "Other"  IServiceᐸT2ˏBooleanᐳ
	TaskFactoryᐸIServiceᐸT2ˏBooleanᐳᐳ o-- CancellationToken : Argument "cancellationToken"
	TaskFactoryᐸIServiceᐸT2ˏBooleanᐳᐳ *--  TaskCreationOptions : TaskCreationOptions
	TaskFactoryᐸIServiceᐸT2ˏBooleanᐳᐳ *--  TaskContinuationOptions : TaskContinuationOptions
	TaskFactoryᐸIServiceᐸT2ˏBooleanᐳᐳ *--  TaskScheduler : TaskScheduler
	FuncᐸIServiceᐸT2ˏTᐳᐳ *--  ServiceᐸT2ˏTᐳ : IServiceᐸT2ˏTᐳ
	TaskFactoryᐸIServiceᐸT2ˏTᐳᐳ o-- CancellationToken : Argument "cancellationToken"
	TaskFactoryᐸIServiceᐸT2ˏTᐳᐳ *--  TaskCreationOptions : TaskCreationOptions
	TaskFactoryᐸIServiceᐸT2ˏTᐳᐳ *--  TaskContinuationOptions : TaskContinuationOptions
	TaskFactoryᐸIServiceᐸT2ˏTᐳᐳ *--  TaskScheduler : TaskScheduler
	OtherServiceᐸT2ᐳ *--  DependencyᐸT2ᐳ : IDependencyᐸT2ᐳ
	ServiceᐸT2ˏTᐳ *--  DependencyᐸT2ᐳ : IDependencyᐸT2ᐳ
	namespace Pure.DI.UsageTests.Generics.GenericAsyncCompositionRootsWithConstraintsScenario {
		class Composition {
		<<partial>>
		+TaskᐸIServiceᐸT2ˏTᐳᐳ GetMyRootAsyncᐸT2ˏTᐳ(System.Threading.CancellationToken cancellationToken)
		+TaskᐸIServiceᐸT2ˏBooleanᐳᐳ GetOtherServiceAsyncᐸT2ᐳ(System.Threading.CancellationToken cancellationToken)
		}
		class DependencyᐸT2ᐳ {
			+Dependency()
		}
		class IDependencyᐸT2ᐳ {
			<<interface>>
		}
		class IServiceᐸT2ˏBooleanᐳ {
			<<interface>>
		}
		class IServiceᐸT2ˏTᐳ {
			<<interface>>
		}
		class OtherServiceᐸT2ᐳ {
		}
		class ServiceᐸT2ˏTᐳ {
			+Service(IDependencyᐸT2ᐳ dependency)
		}
	}
	namespace System {
		class FuncᐸIServiceᐸT2ˏBooleanᐳᐳ {
				<<delegate>>
		}
		class FuncᐸIServiceᐸT2ˏTᐳᐳ {
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
		}
		class TaskFactoryᐸIServiceᐸT2ˏTᐳᐳ {
		}
		class TaskScheduler {
				<<abstract>>
		}
		class TaskᐸIServiceᐸT2ˏBooleanᐳᐳ {
		}
		class TaskᐸIServiceᐸT2ˏTᐳᐳ {
		}
	}
```

