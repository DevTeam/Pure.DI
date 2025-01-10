#### Generic async composition roots with constraints

[![CSharp](https://img.shields.io/badge/C%23-code-blue.svg)](../tests/Pure.DI.UsageTests/Generics/GenericAsyncCompositionRootsWithConstraintsScenario.cs)

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
  public Task<IService<T3, bool>> GetOtherServiceAsync<T3>(CancellationToken cancellationToken)
    where T3: IDisposable
  {
    TaskFactory<IService<T3, bool>> perBlockTaskFactory2;
    CancellationToken localCancellationToken71 = cancellationToken;
    TaskCreationOptions transientTaskCreationOptions3 = TaskCreationOptions.None;
    TaskCreationOptions localTaskCreationOptions72 = transientTaskCreationOptions3;
    TaskContinuationOptions transientTaskContinuationOptions4 = TaskContinuationOptions.None;
    TaskContinuationOptions localTaskContinuationOptions73 = transientTaskContinuationOptions4;
    TaskScheduler transientTaskScheduler5 = TaskScheduler.Default;
    TaskScheduler localTaskScheduler74 = transientTaskScheduler5;
    perBlockTaskFactory2 = new TaskFactory<IService<T3, bool>>(localCancellationToken71, localTaskCreationOptions72, localTaskContinuationOptions73, localTaskScheduler74);
    Func<IService<T3, bool>> perBlockFunc1 = new Func<IService<T3, bool>>([MethodImpl(MethodImplOptions.AggressiveInlining)] () =>
    {
      OtherService<T3> transientOtherService6;
      IDependency<T3> localDependency76 = new Dependency<T3>();
      transientOtherService6 = new OtherService<T3>(localDependency76);
      IService<T3, bool> localValue75 = transientOtherService6;
      return localValue75;
    });
    Task<IService<T3, bool>> transientTask0;
    // Injects an instance factory
    Func<IService<T3, bool>> localFactory77 = perBlockFunc1;
    // Injects a task factory creating and scheduling task objects
    TaskFactory<IService<T3, bool>> localTaskFactory78 = perBlockTaskFactory2;
    // Creates and starts a task using the instance factory
    transientTask0 = localTaskFactory78.StartNew(localFactory77);
    return transientTask0;
  }

  [MethodImpl(MethodImplOptions.AggressiveInlining)]
  public Task<IService<T3, T1>> GetMyRootAsync<T3, T1>(CancellationToken cancellationToken)
    where T3: IDisposable
    where T1: struct
  {
    TaskFactory<IService<T3, T1>> perBlockTaskFactory2;
    CancellationToken localCancellationToken79 = cancellationToken;
    TaskCreationOptions transientTaskCreationOptions3 = TaskCreationOptions.None;
    TaskCreationOptions localTaskCreationOptions80 = transientTaskCreationOptions3;
    TaskContinuationOptions transientTaskContinuationOptions4 = TaskContinuationOptions.None;
    TaskContinuationOptions localTaskContinuationOptions81 = transientTaskContinuationOptions4;
    TaskScheduler transientTaskScheduler5 = TaskScheduler.Default;
    TaskScheduler localTaskScheduler82 = transientTaskScheduler5;
    perBlockTaskFactory2 = new TaskFactory<IService<T3, T1>>(localCancellationToken79, localTaskCreationOptions80, localTaskContinuationOptions81, localTaskScheduler82);
    Func<IService<T3, T1>> perBlockFunc1 = new Func<IService<T3, T1>>([MethodImpl(MethodImplOptions.AggressiveInlining)] () =>
    {
      IService<T3, T1> localValue83 = new Service<T3, T1>(new Dependency<T3>());
      return localValue83;
    });
    Task<IService<T3, T1>> transientTask0;
    // Injects an instance factory
    Func<IService<T3, T1>> localFactory84 = perBlockFunc1;
    // Injects a task factory creating and scheduling task objects
    TaskFactory<IService<T3, T1>> localTaskFactory85 = perBlockTaskFactory2;
    // Creates and starts a task using the instance factory
    transientTask0 = localTaskFactory85.StartNew(localFactory84);
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
	OtherServiceᐸT3ᐳ --|> IServiceᐸT3ˏBooleanᐳ : "Other" 
	ServiceᐸT3ˏT1ᐳ --|> IServiceᐸT3ˏT1ᐳ
	DependencyᐸT3ᐳ --|> IDependencyᐸT3ᐳ
	Composition ..> TaskᐸIServiceᐸT3ˏBooleanᐳᐳ : TaskᐸIServiceᐸT3ˏBooleanᐳᐳ GetOtherServiceAsyncᐸT3ᐳ(System.Threading.CancellationToken cancellationToken)
	Composition ..> TaskᐸIServiceᐸT3ˏT1ᐳᐳ : TaskᐸIServiceᐸT3ˏT1ᐳᐳ GetMyRootAsyncᐸT3ˏT1ᐳ(System.Threading.CancellationToken cancellationToken)
	TaskᐸIServiceᐸT3ˏBooleanᐳᐳ o-- "PerBlock" FuncᐸIServiceᐸT3ˏBooleanᐳᐳ : "Other"  FuncᐸIServiceᐸT3ˏBooleanᐳᐳ
	TaskᐸIServiceᐸT3ˏBooleanᐳᐳ o-- "PerBlock" TaskFactoryᐸIServiceᐸT3ˏBooleanᐳᐳ : TaskFactoryᐸIServiceᐸT3ˏBooleanᐳᐳ
	TaskᐸIServiceᐸT3ˏT1ᐳᐳ o-- "PerBlock" FuncᐸIServiceᐸT3ˏT1ᐳᐳ : FuncᐸIServiceᐸT3ˏT1ᐳᐳ
	TaskᐸIServiceᐸT3ˏT1ᐳᐳ o-- "PerBlock" TaskFactoryᐸIServiceᐸT3ˏT1ᐳᐳ : TaskFactoryᐸIServiceᐸT3ˏT1ᐳᐳ
	FuncᐸIServiceᐸT3ˏBooleanᐳᐳ *--  OtherServiceᐸT3ᐳ : "Other"  IServiceᐸT3ˏBooleanᐳ
	TaskFactoryᐸIServiceᐸT3ˏBooleanᐳᐳ o-- CancellationToken : Argument "cancellationToken"
	TaskFactoryᐸIServiceᐸT3ˏBooleanᐳᐳ *--  TaskCreationOptions : TaskCreationOptions
	TaskFactoryᐸIServiceᐸT3ˏBooleanᐳᐳ *--  TaskContinuationOptions : TaskContinuationOptions
	TaskFactoryᐸIServiceᐸT3ˏBooleanᐳᐳ *--  TaskScheduler : TaskScheduler
	FuncᐸIServiceᐸT3ˏT1ᐳᐳ *--  ServiceᐸT3ˏT1ᐳ : IServiceᐸT3ˏT1ᐳ
	TaskFactoryᐸIServiceᐸT3ˏT1ᐳᐳ o-- CancellationToken : Argument "cancellationToken"
	TaskFactoryᐸIServiceᐸT3ˏT1ᐳᐳ *--  TaskCreationOptions : TaskCreationOptions
	TaskFactoryᐸIServiceᐸT3ˏT1ᐳᐳ *--  TaskContinuationOptions : TaskContinuationOptions
	TaskFactoryᐸIServiceᐸT3ˏT1ᐳᐳ *--  TaskScheduler : TaskScheduler
	OtherServiceᐸT3ᐳ *--  DependencyᐸT3ᐳ : IDependencyᐸT3ᐳ
	ServiceᐸT3ˏT1ᐳ *--  DependencyᐸT3ᐳ : IDependencyᐸT3ᐳ
	namespace Pure.DI.UsageTests.Generics.GenericAsyncCompositionRootsWithConstraintsScenario {
		class Composition {
		<<partial>>
		+TaskᐸIServiceᐸT3ˏT1ᐳᐳ GetMyRootAsyncᐸT3ˏT1ᐳ(System.Threading.CancellationToken cancellationToken)
		+TaskᐸIServiceᐸT3ˏBooleanᐳᐳ GetOtherServiceAsyncᐸT3ᐳ(System.Threading.CancellationToken cancellationToken)
		}
		class DependencyᐸT3ᐳ {
			+Dependency()
		}
		class IDependencyᐸT3ᐳ {
			<<interface>>
		}
		class IServiceᐸT3ˏBooleanᐳ {
			<<interface>>
		}
		class IServiceᐸT3ˏT1ᐳ {
			<<interface>>
		}
		class OtherServiceᐸT3ᐳ {
		}
		class ServiceᐸT3ˏT1ᐳ {
			+Service(IDependencyᐸT3ᐳ dependency)
		}
	}
	namespace System {
		class FuncᐸIServiceᐸT3ˏBooleanᐳᐳ {
				<<delegate>>
		}
		class FuncᐸIServiceᐸT3ˏT1ᐳᐳ {
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
		class TaskFactoryᐸIServiceᐸT3ˏBooleanᐳᐳ {
		}
		class TaskFactoryᐸIServiceᐸT3ˏT1ᐳᐳ {
		}
		class TaskScheduler {
				<<abstract>>
		}
		class TaskᐸIServiceᐸT3ˏBooleanᐳᐳ {
		}
		class TaskᐸIServiceᐸT3ˏT1ᐳᐳ {
		}
	}
```

