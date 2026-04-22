#### Async Root

Demonstrates how to define asynchronous composition roots that return Task or Task<T>, enabling async operations during composition.


```c#
using Shouldly;
using Pure.DI;

DI.Setup(nameof(Composition))
    .Bind<IFileStore>().To<FileStore>()
    .Bind<IBackupService>().To<BackupService>()

    // Specifies to use CancellationToken from the argument
    // when resolving a composition root
    .RootArg<CancellationToken>("cancellationToken")

    // Composition root
    .Root<Task<IBackupService>>("GetBackupServiceAsync");

var composition = new Composition();

// Resolves composition roots asynchronously
var service = await composition.GetBackupServiceAsync(CancellationToken.None);

interface IFileStore;

class FileStore : IFileStore;

interface IBackupService;

class BackupService(IFileStore fileStore) : IBackupService;
```

<details>
<summary>Running this code sample locally</summary>

- Make sure you have the [.NET SDK 10.0](https://dotnet.microsoft.com/en-us/download/dotnet/10.0) or later installed
```bash
dotnet --list-sdk
```
- Create a net10.0 (or later) console application
```bash
dotnet new console -n Sample
```
- Add references to the NuGet packages
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

>[!NOTE]
>Async roots are useful when you need to perform asynchronous initialization or when your services require async creation.

The following partial class will be generated:

```c#
partial class Composition
{
#if NET9_0_OR_GREATER
  private readonly Lock _lock = new Lock();
#else
  private readonly Object _lock = new Object();
#endif

  [MethodImpl(MethodImplOptions.AggressiveInlining)]
  public Task<IBackupService> GetBackupServiceAsync(CancellationToken cancellationToken)
  {
    Task<IBackupService> transientTask221;
    // Injects an instance factory
    Func<IBackupService> perBlockFunc222 = new Func<IBackupService>(
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    () =>
    {
      return new BackupService(new FileStore());
    });
    Func<IBackupService> localFactory = perBlockFunc222;
    // Injects a task factory creating and scheduling task objects
    TaskFactory<IBackupService> perBlockTaskFactory223;
    CancellationToken localCancellationToken = cancellationToken;
    TaskCreationOptions transientTaskCreationOptions227 = TaskCreationOptions.None;
    TaskCreationOptions localTaskCreationOptions = transientTaskCreationOptions227;
    TaskContinuationOptions transientTaskContinuationOptions228 = TaskContinuationOptions.None;
    TaskContinuationOptions localTaskContinuationOptions = transientTaskContinuationOptions228;
    TaskScheduler transientTaskScheduler229 = TaskScheduler.Default;
    TaskScheduler localTaskScheduler = transientTaskScheduler229;
    perBlockTaskFactory223 = new TaskFactory<IBackupService>(localCancellationToken, localTaskCreationOptions, localTaskContinuationOptions, localTaskScheduler);
    TaskFactory<IBackupService> localTaskFactory = perBlockTaskFactory223;
    // Creates and starts a task using the instance factory
    transientTask221 = localTaskFactory.StartNew(localFactory);
    return transientTask221;
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
	FileStore --|> IFileStore
	BackupService --|> IBackupService
	Composition ..> TaskᐸIBackupServiceᐳ : TaskᐸIBackupServiceᐳ GetBackupServiceAsync(System.Threading.CancellationToken cancellationToken)
	BackupService *--  FileStore : IFileStore
	TaskᐸIBackupServiceᐳ o-- "PerBlock" FuncᐸIBackupServiceᐳ : FuncᐸIBackupServiceᐳ
	TaskᐸIBackupServiceᐳ o-- "PerBlock" TaskFactoryᐸIBackupServiceᐳ : TaskFactoryᐸIBackupServiceᐳ
	FuncᐸIBackupServiceᐳ *--  BackupService : IBackupService
	TaskFactoryᐸIBackupServiceᐳ *--  TaskScheduler : TaskScheduler
	TaskFactoryᐸIBackupServiceᐳ *--  TaskCreationOptions : TaskCreationOptions
	TaskFactoryᐸIBackupServiceᐳ *--  TaskContinuationOptions : TaskContinuationOptions
	TaskFactoryᐸIBackupServiceᐳ o-- CancellationToken : Argument "cancellationToken"
	namespace Pure.DI.UsageTests.Basics.AsyncRootScenario {
		class BackupService {
				<<class>>
			+BackupService(IFileStore fileStore)
		}
		class Composition {
		<<partial>>
		+TaskᐸIBackupServiceᐳ GetBackupServiceAsync(System.Threading.CancellationToken cancellationToken)
		}
		class FileStore {
				<<class>>
			+FileStore()
		}
		class IBackupService {
			<<interface>>
		}
		class IFileStore {
			<<interface>>
		}
	}
	namespace System {
		class FuncᐸIBackupServiceᐳ {
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
		class TaskFactoryᐸIBackupServiceᐳ {
				<<class>>
		}
		class TaskScheduler {
				<<abstract>>
		}
		class TaskᐸIBackupServiceᐳ {
				<<class>>
		}
	}
```

