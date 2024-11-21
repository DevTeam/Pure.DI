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

  [OrdinalAttribute(10)]
  public Composition()
  {
    _root = this;
  }

  internal Composition(Composition parentScope)
  {
    _root = (parentScope ?? throw new ArgumentNullException(nameof(parentScope)))._root;
  }

  [MethodImpl(MethodImplOptions.AggressiveInlining)]
  public Task<IService<T, bool>> GetOtherServiceAsync<T>(CancellationToken cancellationToken)
    where T: IDisposable
  {
    TaskFactory<IService<T, bool>> perBlockTaskFactory2;
    CancellationToken localCancellationToken60 = cancellationToken;
    TaskCreationOptions transientTaskCreationOptions3 = TaskCreationOptions.None;
    TaskCreationOptions localTaskCreationOptions61 = transientTaskCreationOptions3;
    TaskContinuationOptions transientTaskContinuationOptions4 = TaskContinuationOptions.None;
    TaskContinuationOptions localTaskContinuationOptions62 = transientTaskContinuationOptions4;
    TaskScheduler transientTaskScheduler5 = TaskScheduler.Default;
    TaskScheduler localTaskScheduler63 = transientTaskScheduler5;
    perBlockTaskFactory2 = new TaskFactory<IService<T, bool>>(localCancellationToken60, localTaskCreationOptions61, localTaskContinuationOptions62, localTaskScheduler63);
    Func<IService<T, bool>> perBlockFunc1 = new Func<IService<T, bool>>([MethodImpl(MethodImplOptions.AggressiveInlining)] () =>
    {
      OtherService<T> transientOtherService6;
      IDependency<T> localDependency65 = new Dependency<T>();
      transientOtherService6 = new OtherService<T>(localDependency65);
      IService<T, bool> localValue64 = transientOtherService6;
      return localValue64;
    });
    Task<IService<T, bool>> transientTask0;
    // Injects an instance factory
    Func<IService<T, bool>> localFactory66 = perBlockFunc1;
    // Injects a task factory creating and scheduling task objects
    TaskFactory<IService<T, bool>> localTaskFactory67 = perBlockTaskFactory2;
    // Creates and starts a task using the instance factory
    transientTask0 = localTaskFactory67.StartNew(localFactory66);
    return transientTask0;
  }

  [MethodImpl(MethodImplOptions.AggressiveInlining)]
  public Task<IService<T, T1>> GetMyRootAsync<T, T1>(CancellationToken cancellationToken)
    where T: IDisposable
    where T1: struct
  {
    TaskFactory<IService<T, T1>> perBlockTaskFactory2;
    CancellationToken localCancellationToken68 = cancellationToken;
    TaskCreationOptions transientTaskCreationOptions3 = TaskCreationOptions.None;
    TaskCreationOptions localTaskCreationOptions69 = transientTaskCreationOptions3;
    TaskContinuationOptions transientTaskContinuationOptions4 = TaskContinuationOptions.None;
    TaskContinuationOptions localTaskContinuationOptions70 = transientTaskContinuationOptions4;
    TaskScheduler transientTaskScheduler5 = TaskScheduler.Default;
    TaskScheduler localTaskScheduler71 = transientTaskScheduler5;
    perBlockTaskFactory2 = new TaskFactory<IService<T, T1>>(localCancellationToken68, localTaskCreationOptions69, localTaskContinuationOptions70, localTaskScheduler71);
    Func<IService<T, T1>> perBlockFunc1 = new Func<IService<T, T1>>([MethodImpl(MethodImplOptions.AggressiveInlining)] () =>
    {
      IService<T, T1> localValue72 = new Service<T, T1>(new Dependency<T>());
      return localValue72;
    });
    Task<IService<T, T1>> transientTask0;
    // Injects an instance factory
    Func<IService<T, T1>> localFactory73 = perBlockFunc1;
    // Injects a task factory creating and scheduling task objects
    TaskFactory<IService<T, T1>> localTaskFactory74 = perBlockTaskFactory2;
    // Creates and starts a task using the instance factory
    transientTask0 = localTaskFactory74.StartNew(localFactory73);
    return transientTask0;
  }
}
```

Class diagram:

```mermaid
classDiagram
	class Composition {
		<<partial>>
		+TaskᐸIServiceᐸTˏT1ᐳᐳ GetMyRootAsyncᐸTˏT1ᐳ(System.Threading.CancellationToken cancellationToken)
		+TaskᐸIServiceᐸTˏBooleanᐳᐳ GetOtherServiceAsyncᐸTᐳ(System.Threading.CancellationToken cancellationToken)
	}
	class FuncᐸIServiceᐸTˏBooleanᐳᐳ
	class TaskFactoryᐸIServiceᐸTˏBooleanᐳᐳ
	class FuncᐸIServiceᐸTˏT1ᐳᐳ
	class TaskFactoryᐸIServiceᐸTˏT1ᐳᐳ
	OtherServiceᐸTᐳ --|> IServiceᐸTˏBooleanᐳ : "Other" 
	class OtherServiceᐸTᐳ
	class CancellationToken
	class TaskCreationOptions
	class TaskContinuationOptions
	class TaskScheduler
	ServiceᐸTˏT1ᐳ --|> IServiceᐸTˏT1ᐳ
	class ServiceᐸTˏT1ᐳ {
		+Service(IDependencyᐸTᐳ dependency)
	}
	DependencyᐸTᐳ --|> IDependencyᐸTᐳ
	class DependencyᐸTᐳ {
		+Dependency()
	}
	class IServiceᐸTˏBooleanᐳ {
		<<interface>>
	}
	class IServiceᐸTˏT1ᐳ {
		<<interface>>
	}
	class IDependencyᐸTᐳ {
		<<interface>>
	}
	Composition ..> TaskᐸIServiceᐸTˏBooleanᐳᐳ : TaskᐸIServiceᐸTˏBooleanᐳᐳ GetOtherServiceAsyncᐸTᐳ(System.Threading.CancellationToken cancellationToken)
	Composition ..> TaskᐸIServiceᐸTˏT1ᐳᐳ : TaskᐸIServiceᐸTˏT1ᐳᐳ GetMyRootAsyncᐸTˏT1ᐳ(System.Threading.CancellationToken cancellationToken)
	TaskᐸIServiceᐸTˏBooleanᐳᐳ o-- "PerBlock" FuncᐸIServiceᐸTˏBooleanᐳᐳ : "Other"  FuncᐸIServiceᐸTˏBooleanᐳᐳ
	TaskᐸIServiceᐸTˏBooleanᐳᐳ o-- "PerBlock" TaskFactoryᐸIServiceᐸTˏBooleanᐳᐳ : TaskFactoryᐸIServiceᐸTˏBooleanᐳᐳ
	TaskᐸIServiceᐸTˏT1ᐳᐳ o-- "PerBlock" FuncᐸIServiceᐸTˏT1ᐳᐳ : FuncᐸIServiceᐸTˏT1ᐳᐳ
	TaskᐸIServiceᐸTˏT1ᐳᐳ o-- "PerBlock" TaskFactoryᐸIServiceᐸTˏT1ᐳᐳ : TaskFactoryᐸIServiceᐸTˏT1ᐳᐳ
	FuncᐸIServiceᐸTˏBooleanᐳᐳ *--  OtherServiceᐸTᐳ : "Other"  IServiceᐸTˏBooleanᐳ
	TaskFactoryᐸIServiceᐸTˏBooleanᐳᐳ o-- CancellationToken : Argument "cancellationToken"
	TaskFactoryᐸIServiceᐸTˏBooleanᐳᐳ *--  TaskCreationOptions : TaskCreationOptions
	TaskFactoryᐸIServiceᐸTˏBooleanᐳᐳ *--  TaskContinuationOptions : TaskContinuationOptions
	TaskFactoryᐸIServiceᐸTˏBooleanᐳᐳ *--  TaskScheduler : TaskScheduler
	FuncᐸIServiceᐸTˏT1ᐳᐳ *--  ServiceᐸTˏT1ᐳ : IServiceᐸTˏT1ᐳ
	TaskFactoryᐸIServiceᐸTˏT1ᐳᐳ o-- CancellationToken : Argument "cancellationToken"
	TaskFactoryᐸIServiceᐸTˏT1ᐳᐳ *--  TaskCreationOptions : TaskCreationOptions
	TaskFactoryᐸIServiceᐸTˏT1ᐳᐳ *--  TaskContinuationOptions : TaskContinuationOptions
	TaskFactoryᐸIServiceᐸTˏT1ᐳᐳ *--  TaskScheduler : TaskScheduler
	OtherServiceᐸTᐳ *--  DependencyᐸTᐳ : IDependencyᐸTᐳ
	ServiceᐸTˏT1ᐳ *--  DependencyᐸTᐳ : IDependencyᐸTᐳ
```

