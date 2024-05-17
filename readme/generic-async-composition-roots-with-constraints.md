#### Generic async composition roots with constraints

[![CSharp](https://img.shields.io/badge/C%23-code-blue.svg)](../tests/Pure.DI.UsageTests/Generics/GenericAsyncCompositionRootsWithConstraintsScenario.cs)


```c#
interface IDependency<T>
    where T: IDisposable;

class Dependency<T> : IDependency<T>
    where T: IDisposable;

interface IService<T, TStruct>
    where T: IDisposable
    where TStruct: struct;

class Service<T, TStruct>(IDependency<T> dependency) : IService<T, TStruct>
    where T: IDisposable
    where TStruct: struct;

class OtherService<T>(IDependency<T> dependency) : IService<T, bool>
    where T: IDisposable;

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
> `Resolve' methods cannot be used to resolve generic composition roots.

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
  public Task<IService<T, T1>> GetMyRootAsync<T, T1>(CancellationToken cancellationToken)
    where T: IDisposable
    where T1: struct
  {
    var perResolveFunc43 = default(Func<IService<T, T1>>);
    TaskScheduler transientTaskScheduler4 = TaskScheduler.Default;
    TaskContinuationOptions transientTaskContinuationOptions3 = TaskContinuationOptions.None;
    TaskCreationOptions transientTaskCreationOptions2 = TaskCreationOptions.None;
    TaskFactory<IService<T, T1>> perBlockTaskFactory1;
    {
        var localCancellationToken34 = cancellationToken;
        var localTaskCreationOptions35 = transientTaskCreationOptions2;
        var localTaskContinuationOptions36 = transientTaskContinuationOptions3;
        var localTaskScheduler37 = transientTaskScheduler4;
        perBlockTaskFactory1 = new TaskFactory<IService<T, T1>>(localCancellationToken34, localTaskCreationOptions35, localTaskContinuationOptions36, localTaskScheduler37);
    }

    if (perResolveFunc43 == null)
    {
        lock (_lock)
        {
            if (perResolveFunc43 == null)
            {
                perResolveFunc43 = new Func<IService<T, T1>>(
                [MethodImpl(MethodImplOptions.AggressiveInlining)]
                () =>
                {
                    var localValue38 = new Service<T, T1>(new Dependency<T>());
                    return localValue38;
                });
            }
        }
    }

    Task<IService<T, T1>> transientTask0;
    {
        var localFactory39 = perResolveFunc43!;
        var localTaskFactory40 = perBlockTaskFactory1;
        transientTask0 = localTaskFactory40.StartNew(localFactory39);
    }

    return transientTask0;
  }

  [MethodImpl(MethodImplOptions.AggressiveInlining)]
  public Task<IService<T, bool>> GetOtherServiceAsync<T>(CancellationToken cancellationToken)
    where T: IDisposable
  {
    var perResolveFunc45 = default(Func<IService<T, bool>>);
    TaskScheduler transientTaskScheduler4 = TaskScheduler.Default;
    TaskContinuationOptions transientTaskContinuationOptions3 = TaskContinuationOptions.None;
    TaskCreationOptions transientTaskCreationOptions2 = TaskCreationOptions.None;
    TaskFactory<IService<T, bool>> perBlockTaskFactory1;
    {
        var localCancellationToken41 = cancellationToken;
        var localTaskCreationOptions42 = transientTaskCreationOptions2;
        var localTaskContinuationOptions43 = transientTaskContinuationOptions3;
        var localTaskScheduler44 = transientTaskScheduler4;
        perBlockTaskFactory1 = new TaskFactory<IService<T, bool>>(localCancellationToken41, localTaskCreationOptions42, localTaskContinuationOptions43, localTaskScheduler44);
    }

    if (perResolveFunc45 == null)
    {
        lock (_lock)
        {
            if (perResolveFunc45 == null)
            {
                perResolveFunc45 = new Func<IService<T, bool>>(
                [MethodImpl(MethodImplOptions.AggressiveInlining)]
                () =>
                {
                    OtherService<T> transientOtherService5;
                    {
                        var localDependency46 = new Dependency<T>();
                        transientOtherService5 = new OtherService<T>(localDependency46);
                    }

                    var localValue45 = transientOtherService5;
                    return localValue45;
                });
            }
        }
    }

    Task<IService<T, bool>> transientTask0;
    {
        var localFactory47 = perResolveFunc45!;
        var localTaskFactory48 = perBlockTaskFactory1;
        transientTask0 = localTaskFactory48.StartNew(localFactory47);
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
		+TaskᐸIServiceᐸTˏT1ᐳᐳ GetMyRootAsyncᐸTˏT1ᐳ(System.Threading.CancellationToken cancellationToken)
		+TaskᐸIServiceᐸTˏBooleanᐳᐳ GetOtherServiceAsyncᐸTᐳ(System.Threading.CancellationToken cancellationToken)
	}
	class TaskScheduler
	class TaskCreationOptions
	class TaskContinuationOptions
	class TaskFactory
	class CancellationToken
	class FuncᐸIServiceᐸTˏT1ᐳᐳ
	class TaskFactoryᐸIServiceᐸTˏT1ᐳᐳ
	class FuncᐸIServiceᐸTˏBooleanᐳᐳ
	class TaskFactoryᐸIServiceᐸTˏBooleanᐳᐳ
	ServiceᐸTˏT1ᐳ --|> IServiceᐸTˏT1ᐳ
	class ServiceᐸTˏT1ᐳ {
		+Service(IDependencyᐸTᐳ dependency)
	}
	OtherServiceᐸTᐳ --|> IServiceᐸTˏBooleanᐳ : "Other" 
	class OtherServiceᐸTᐳ
	DependencyᐸTᐳ --|> IDependencyᐸTᐳ
	class DependencyᐸTᐳ {
		+Dependency()
	}
	class IServiceᐸTˏT1ᐳ {
		<<interface>>
	}
	class IServiceᐸTˏBooleanᐳ {
		<<interface>>
	}
	class IDependencyᐸTᐳ {
		<<interface>>
	}
	TaskFactory o-- CancellationToken : Argument "cancellationToken"
	TaskFactory *--  TaskCreationOptions : TaskCreationOptions
	TaskFactory *--  TaskContinuationOptions : TaskContinuationOptions
	TaskFactory *--  TaskScheduler : TaskScheduler
	Composition ..> TaskᐸIServiceᐸTˏT1ᐳᐳ : TaskᐸIServiceᐸTˏT1ᐳᐳ GetMyRootAsyncᐸTˏT1ᐳ(System.Threading.CancellationToken cancellationToken)
	Composition ..> TaskᐸIServiceᐸTˏBooleanᐳᐳ : TaskᐸIServiceᐸTˏBooleanᐳᐳ GetOtherServiceAsyncᐸTᐳ(System.Threading.CancellationToken cancellationToken)
	TaskᐸIServiceᐸTˏT1ᐳᐳ o-- "PerResolve" FuncᐸIServiceᐸTˏT1ᐳᐳ : FuncᐸIServiceᐸTˏT1ᐳᐳ
	TaskᐸIServiceᐸTˏT1ᐳᐳ o-- "PerBlock" TaskFactoryᐸIServiceᐸTˏT1ᐳᐳ : TaskFactoryᐸIServiceᐸTˏT1ᐳᐳ
	TaskᐸIServiceᐸTˏBooleanᐳᐳ o-- "PerResolve" FuncᐸIServiceᐸTˏBooleanᐳᐳ : "Other"  FuncᐸIServiceᐸTˏBooleanᐳᐳ
	TaskᐸIServiceᐸTˏBooleanᐳᐳ o-- "PerBlock" TaskFactoryᐸIServiceᐸTˏBooleanᐳᐳ : TaskFactoryᐸIServiceᐸTˏBooleanᐳᐳ
	FuncᐸIServiceᐸTˏT1ᐳᐳ *--  ServiceᐸTˏT1ᐳ : IServiceᐸTˏT1ᐳ
	TaskFactoryᐸIServiceᐸTˏT1ᐳᐳ o-- CancellationToken : Argument "cancellationToken"
	TaskFactoryᐸIServiceᐸTˏT1ᐳᐳ *--  TaskCreationOptions : TaskCreationOptions
	TaskFactoryᐸIServiceᐸTˏT1ᐳᐳ *--  TaskContinuationOptions : TaskContinuationOptions
	TaskFactoryᐸIServiceᐸTˏT1ᐳᐳ *--  TaskScheduler : TaskScheduler
	FuncᐸIServiceᐸTˏBooleanᐳᐳ *--  OtherServiceᐸTᐳ : "Other"  IServiceᐸTˏBooleanᐳ
	TaskFactoryᐸIServiceᐸTˏBooleanᐳᐳ o-- CancellationToken : Argument "cancellationToken"
	TaskFactoryᐸIServiceᐸTˏBooleanᐳᐳ *--  TaskCreationOptions : TaskCreationOptions
	TaskFactoryᐸIServiceᐸTˏBooleanᐳᐳ *--  TaskContinuationOptions : TaskContinuationOptions
	TaskFactoryᐸIServiceᐸTˏBooleanᐳᐳ *--  TaskScheduler : TaskScheduler
	ServiceᐸTˏT1ᐳ *--  DependencyᐸTᐳ : IDependencyᐸTᐳ
	OtherServiceᐸTᐳ *--  DependencyᐸTᐳ : IDependencyᐸTᐳ
```

