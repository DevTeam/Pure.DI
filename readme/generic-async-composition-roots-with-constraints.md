#### Generic async composition roots with constraints

[![CSharp](https://img.shields.io/badge/C%23-code-blue.svg)](../tests/Pure.DI.UsageTests/Generics/GenericAsyncCompositionRootsWithConstraintsScenario.cs)

> [!IMPORTANT]
> `Resolve' methods cannot be used to resolve generic composition roots.


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
> The method `Inject()`cannot be used outside of the binding setup.

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
  public Task<IService<T, bool>> GetOtherServiceAsync<T>(CancellationToken cancellationToken)
    where T: IDisposable
  {
    var perResolveFunc48 = default(Func<IService<T, bool>>);
    TaskScheduler transientTaskScheduler4 = TaskScheduler.Default;
    TaskContinuationOptions transientTaskContinuationOptions3 = TaskContinuationOptions.None;
    TaskCreationOptions transientTaskCreationOptions2 = TaskCreationOptions.None;
    TaskFactory<IService<T, bool>> perBlockTaskFactory1;
    {
        CancellationToken localCancellationToken41 = cancellationToken;
        TaskCreationOptions localTaskCreationOptions42 = transientTaskCreationOptions2;
        TaskContinuationOptions localTaskContinuationOptions43 = transientTaskContinuationOptions3;
        TaskScheduler localTaskScheduler44 = transientTaskScheduler4;
        perBlockTaskFactory1 = new TaskFactory<IService<T, bool>>(localCancellationToken41, localTaskCreationOptions42, localTaskContinuationOptions43, localTaskScheduler44);
    }

    if (perResolveFunc48 == null)
    {
        lock (_lock)
        {
            if (perResolveFunc48 == null)
            {
                perResolveFunc48 = new Func<IService<T, bool>>(
                [MethodImpl(MethodImplOptions.AggressiveInlining)]
                () =>
                {
                    OtherService<T> transientOtherService5;
                    {
                        IDependency<T> localDependency46 = new Dependency<T>();
                        transientOtherService5 = new OtherService<T>(localDependency46);
                    }

                    IService<T, bool> localValue45 = transientOtherService5;
                    return localValue45;
                });
            }
        }
    }

    Task<IService<T, bool>> transientTask0;
    {
        Func<IService<T, bool>> localFactory47 = perResolveFunc48!;
        TaskFactory<IService<T, bool>> localTaskFactory48 = perBlockTaskFactory1;
        transientTask0 = localTaskFactory48.StartNew(localFactory47);
    }

    return transientTask0;
  }

  [MethodImpl(MethodImplOptions.AggressiveInlining)]
  public Task<IService<T, T1>> GetMyRootAsync<T, T1>(CancellationToken cancellationToken)
    where T: IDisposable
    where T1: struct
  {
    var perResolveFunc50 = default(Func<IService<T, T1>>);
    TaskScheduler transientTaskScheduler4 = TaskScheduler.Default;
    TaskContinuationOptions transientTaskContinuationOptions3 = TaskContinuationOptions.None;
    TaskCreationOptions transientTaskCreationOptions2 = TaskCreationOptions.None;
    TaskFactory<IService<T, T1>> perBlockTaskFactory1;
    {
        CancellationToken localCancellationToken49 = cancellationToken;
        TaskCreationOptions localTaskCreationOptions50 = transientTaskCreationOptions2;
        TaskContinuationOptions localTaskContinuationOptions51 = transientTaskContinuationOptions3;
        TaskScheduler localTaskScheduler52 = transientTaskScheduler4;
        perBlockTaskFactory1 = new TaskFactory<IService<T, T1>>(localCancellationToken49, localTaskCreationOptions50, localTaskContinuationOptions51, localTaskScheduler52);
    }

    if (perResolveFunc50 == null)
    {
        lock (_lock)
        {
            if (perResolveFunc50 == null)
            {
                perResolveFunc50 = new Func<IService<T, T1>>(
                [MethodImpl(MethodImplOptions.AggressiveInlining)]
                () =>
                {
                    IService<T, T1> localValue53 = new Service<T, T1>(new Dependency<T>());
                    return localValue53;
                });
            }
        }
    }

    Task<IService<T, T1>> transientTask0;
    {
        Func<IService<T, T1>> localFactory54 = perResolveFunc50!;
        TaskFactory<IService<T, T1>> localTaskFactory55 = perBlockTaskFactory1;
        transientTask0 = localTaskFactory55.StartNew(localFactory54);
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
	class FuncᐸIServiceᐸTˏBooleanᐳᐳ
	class TaskFactoryᐸIServiceᐸTˏBooleanᐳᐳ
	class FuncᐸIServiceᐸTˏT1ᐳᐳ
	class TaskFactoryᐸIServiceᐸTˏT1ᐳᐳ
	OtherServiceᐸTᐳ --|> IServiceᐸTˏBooleanᐳ : "Other" 
	class OtherServiceᐸTᐳ
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
	TaskFactory o-- CancellationToken : Argument "cancellationToken"
	TaskFactory *--  TaskCreationOptions : TaskCreationOptions
	TaskFactory *--  TaskContinuationOptions : TaskContinuationOptions
	TaskFactory *--  TaskScheduler : TaskScheduler
	TaskᐸIServiceᐸTˏBooleanᐳᐳ o-- "PerResolve" FuncᐸIServiceᐸTˏBooleanᐳᐳ : "Other"  FuncᐸIServiceᐸTˏBooleanᐳᐳ
	TaskᐸIServiceᐸTˏBooleanᐳᐳ o-- "PerBlock" TaskFactoryᐸIServiceᐸTˏBooleanᐳᐳ : TaskFactoryᐸIServiceᐸTˏBooleanᐳᐳ
	TaskᐸIServiceᐸTˏT1ᐳᐳ o-- "PerResolve" FuncᐸIServiceᐸTˏT1ᐳᐳ : FuncᐸIServiceᐸTˏT1ᐳᐳ
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

