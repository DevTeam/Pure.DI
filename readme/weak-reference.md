#### Weak Reference

Injecting `WeakReference<T>` lets a service hold a dependency without keeping it alive — useful for large, recreatable objects such as caches. Bind the underlying type as usual and request `WeakReference<T>`; the consumer then calls `TryGetTarget`, which returns `false` once the object has been garbage-collected.


```c#
using Shouldly;
using Pure.DI;

DI.Setup(nameof(Composition))
    .Bind<ILargeCache>().To<LargeCache>()
    .Bind<IService>().To<Service>()

    // Composition root
    .Root<IService>("MyService");

var composition = new Composition();
var service = composition.MyService;

// Represents a large memory object (e.g., a cache of images or large datasets)
interface ILargeCache;

class LargeCache : ILargeCache;

interface IService;

class Service(WeakReference<ILargeCache> cache) : IService
{
    public ILargeCache? Cache =>
        // Tries to retrieve the target object from the WeakReference.
        // If the object has been collected by the GC, it returns null.
        cache.TryGetTarget(out var value)
            ? value
            : null;
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

>[!NOTE]
>`WeakReference<T>` is useful for caching scenarios where you want to allow garbage collection when memory is constrained.

<details>
<summary>The following partial class will be generated</summary>

```c#
partial class Composition
{
  public IService MyService
  {
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    get
    {
      return new Service(new WeakReference<ILargeCache>(new LargeCache()));
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
	LargeCache --|> ILargeCache
	Service --|> IService
	Composition ..> Service : IService MyService
	Service *-- WeakReferenceᐸILargeCacheᐳ : WeakReferenceᐸILargeCacheᐳ
	WeakReferenceᐸILargeCacheᐳ *-- LargeCache : ILargeCache
	namespace Pure.DI.UsageTests.BCL.WeakReferenceScenario {
		class Composition {
		<<partial>>
		+IService MyService
		}
		class ILargeCache {
			<<interface>>
		}
		class IService {
			<<interface>>
		}
		class LargeCache {
				<<class>>
			+LargeCache()
		}
		class Service {
				<<class>>
			+Service(WeakReferenceᐸILargeCacheᐳ cache)
		}
	}
	namespace System {
		class WeakReferenceᐸILargeCacheᐳ {
				<<class>>
			+WeakReference(ILargeCache target)
		}
	}
```

