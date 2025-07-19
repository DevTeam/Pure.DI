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
#if NET9_0_OR_GREATER
  private readonly Lock _lock;
#else
  private readonly Object _lock;
#endif

  [OrdinalAttribute(256)]
  public Composition()
  {
#if NET9_0_OR_GREATER
    _lock = new Lock();
#else
    _lock = new Object();
#endif
  }

  internal Composition(Composition parentScope)
  {
  }

  [MethodImpl(MethodImplOptions.AggressiveInlining)]
  public Task<IService<T2, bool>> GetOtherServiceAsync<T2>(CancellationToken cancellationToken)
    where T2: IDisposable
  {
    Task<IService<T2, bool>> transTask; // Injects an instance factory
    Func<IService<T2, bool>> blockFunc1 = new Func<IService<T2, bool>>(
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    () =>
    {
      OtherService<T2> transOtherService3;
      IDependency<T2> localDependency130 = new Dependency<T2>();
      transOtherService3 = new OtherService<T2>(localDependency130);
      IService<T2, bool> localValue129 = transOtherService3;
      return localValue129;
    });
    Func<IService<T2, bool>> localFactory127 = blockFunc1;
    // Injects a task factory creating and scheduling task objects
    TaskFactory<IService<T2, bool>> blockTaskFactory2;
    CancellationToken localCancellationToken131 = cancellationToken;
    TaskCreationOptions transTaskCreationOptions6 = TaskCreationOptions.None;
    TaskCreationOptions localTaskCreationOptions132 = transTaskCreationOptions6;
    TaskContinuationOptions transTaskContinuationOptions7 = TaskContinuationOptions.None;
    TaskContinuationOptions localTaskContinuationOptions133 = transTaskContinuationOptions7;
    TaskScheduler transTaskScheduler8 = TaskScheduler.Default;
    TaskScheduler localTaskScheduler134 = transTaskScheduler8;
    blockTaskFactory2 = new TaskFactory<IService<T2, bool>>(localCancellationToken131, localTaskCreationOptions132, localTaskContinuationOptions133, localTaskScheduler134);
    TaskFactory<IService<T2, bool>> localTaskFactory128 = blockTaskFactory2;
    // Creates and starts a task using the instance factory
    transTask = localTaskFactory128.StartNew(localFactory127);
    return transTask;
  }

  [MethodImpl(MethodImplOptions.AggressiveInlining)]
  public Task<IService<T2, T3>> GetMyRootAsync<T2, T3>(CancellationToken cancellationToken)
    where T2: IDisposable
    where T3: struct
  {
    Task<IService<T2, T3>> transTask9; // Injects an instance factory
    Func<IService<T2, T3>> blockFunc10 = new Func<IService<T2, T3>>(
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    () =>
    {
      IService<T2, T3> localValue137 = new Service<T2, T3>(new Dependency<T2>());
      return localValue137;
    });
    Func<IService<T2, T3>> localFactory135 = blockFunc10;
    // Injects a task factory creating and scheduling task objects
    TaskFactory<IService<T2, T3>> blockTaskFactory11;
    CancellationToken localCancellationToken138 = cancellationToken;
    TaskCreationOptions transTaskCreationOptions15 = TaskCreationOptions.None;
    TaskCreationOptions localTaskCreationOptions139 = transTaskCreationOptions15;
    TaskContinuationOptions transTaskContinuationOptions16 = TaskContinuationOptions.None;
    TaskContinuationOptions localTaskContinuationOptions140 = transTaskContinuationOptions16;
    TaskScheduler transTaskScheduler17 = TaskScheduler.Default;
    TaskScheduler localTaskScheduler141 = transTaskScheduler17;
    blockTaskFactory11 = new TaskFactory<IService<T2, T3>>(localCancellationToken138, localTaskCreationOptions139, localTaskContinuationOptions140, localTaskScheduler141);
    TaskFactory<IService<T2, T3>> localTaskFactory136 = blockTaskFactory11;
    // Creates and starts a task using the instance factory
    transTask9 = localTaskFactory136.StartNew(localFactory135);
    return transTask9;
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

