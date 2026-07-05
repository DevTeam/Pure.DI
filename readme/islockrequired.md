#### IsLockRequired

`IsLockRequired` indicates whether a lock is required for thread-safe operations in the current context. This property is useful when you need to conditionally synchronize based on thread safety requirements.
Use this when custom factory logic must respect thread-safety semantics of generated code.


```c#
using Shouldly;
using Pure.DI;

var composition = new Composition();

var service = composition.Service;
service.Locked.ShouldBeTrue();

var singletonService = composition.SingletonService;
singletonService.Locked.ShouldBeFalse();

interface IService
{
    bool Locked { get; }
}

class Service(bool lockRequired) : IService
{
    public bool Locked => lockRequired;
}

partial class Composition
{
    private void Setup() =>

        DI.Setup(nameof(Composition))
            .Hint(Hint.ThreadSafe, "On")
            .Bind().To(ctx =>
            {
                // In a thread-safe context, IsLockRequired is true
                // Use it to conditionally lock the context
                if (ctx.IsLockRequired)
                {
                    lock (ctx.Lock)
                    {
                        return new Service(ctx.IsLockRequired);
                    }
                }

                return new Service(ctx.IsLockRequired);
            })
            .Bind(Tag.Single).As(Lifetime.Singleton).To((IService service) => service)
            .Root<IService>(nameof(Service))
            .Root<IService>(nameof(SingletonService), Tag.Single);
}
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

Limitations: avoid adding business logic inside lock-aware factories; use it only for synchronization concerns.
See also: [ThreadSafe hint](threadsafe-hint.md), [Factory](factory.md).

<details>
<summary>The following partial class will be generated</summary>

```c#
partial class Composition
{
#if NET9_0_OR_GREATER
  private readonly Lock _lock = new Lock();
#else
  private readonly Object _lock = new Object();
#endif

  private IService? _singletonCompositionWithGenericRootsAndArgsInOtherProject;

  public IService Service
  {
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    get
    {
      Service transientService;
      // In a thread-safe context, IsLockRequired is true
      // Use it to conditionally lock the context
      if (true)
      {
        lock (_lock)
        {
          {
            transientService = new Service(true);
            goto transientServiceFinish;
          }
        }
      }

      transientService = new Service(true);
      transientServiceFinish:
        ;
      return transientService;
    }
  }

  public IService SingletonService
  {
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    get
    {
      if (_singletonCompositionWithGenericRootsAndArgsInOtherProject is null)
        lock (_lock)
          if (_singletonCompositionWithGenericRootsAndArgsInOtherProject is null)
          {
            Service transientService;
            // In a thread-safe context, IsLockRequired is true
            // Use it to conditionally lock the context
            if (false)
            {
              lock (_lock)
              {
                {
                  transientService = new Service(false);
                  goto transientServiceFinish;
                }
              }
            }

            transientService = new Service(false);
            transientServiceFinish:
              ;
            IService localService = transientService;
            _singletonCompositionWithGenericRootsAndArgsInOtherProject = localService;
          }

      return _singletonCompositionWithGenericRootsAndArgsInOtherProject;
    }
  }
}
```

</details>

Class diagram:

```mermaid
---
 config:
  class:
   hideEmptyMembersBox: true
---
classDiagram
	Service --|> IService
	Composition ..> IService : IService SingletonService
	Composition ..> Service : IService Service
	IService *-- Service : IService
	namespace Pure.DI.UsageTests.Advanced.IsLockRequiredScenario {
		class Composition {
		<<partial>>
		+IService Service
		+IService SingletonService
		}
		class IService {
				<<interface>>
		}
		class Service {
				<<class>>
		}
	}
```

See also:

- [Thread-safe overrides](thread-safe-overrides.md)

