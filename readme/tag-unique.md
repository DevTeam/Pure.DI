#### Tag Unique

`Tag.Unique` is useful to register a binding with a unique tag. It will not be available through the composition root or `Resolve` methods directly, but can be injected in compositions as some kind of enumeration.
Each `Tag.Unique` binding gets its own tag, so any number of implementations of one contract can coexist and be collected into `IEnumerable<T>`, an array or an `ImmutableArray<T>`.


```c#
using Shouldly;
using Pure.DI;
using System.Collections.Immutable;

DI.Setup(nameof(Composition))
    .Bind<INotificationChannel<TT>>(Tag.Unique).To<EmailChannel<TT>>()
    .Bind<INotificationChannel<TT>>(Tag.Unique).To<SmsChannel<TT>>()
    .Bind<INotificationService<TT>>().To<NotificationService<TT>>()

    // Composition root
    .Root<INotificationService<string>>("NotificationService");

var composition = new Composition();
var notificationService = composition.NotificationService;
notificationService.Channels.Length.ShouldBe(2);

interface INotificationChannel<T>;

class EmailChannel<T> : INotificationChannel<T>;

class SmsChannel<T> : INotificationChannel<T>;

interface INotificationService<T>
{
    ImmutableArray<INotificationChannel<T>> Channels { get; }
}

class NotificationService<T>(IEnumerable<INotificationChannel<T>> channels)
    : INotificationService<T>
{
    public ImmutableArray<INotificationChannel<T>> Channels { get; }
        = [..channels];
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

Use this to aggregate several implementations without exposing each one as a direct root.
This is how satellites are registered in a real project - trace sources, loggers, processors, validators - and then injected as a collection into the one component that orchestrates them.
Limitations: unique-tag bindings are intentionally hidden from direct resolve; document this to avoid confusion in integration code.
A unique binding that nothing collects is reported as `DIW003`, which is a useful signal that an implementation was registered but never reached.
Prefer [Enumerable](enumerable.md) or `Tag.Type` when the consumer must address one specific implementation, because a unique tag cannot be named by the consumer at all.
See also: [Tags](tags.md), [Enumerable](enumerable.md).

<details>
<summary>The following partial class will be generated</summary>

```c#
partial class Composition
{
  public INotificationService<string> NotificationService
  {
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    get
    {
      [MethodImpl(MethodImplOptions.AggressiveInlining)]
      IEnumerable<INotificationChannel<string>> EnumerationOf_perBlockIEnumerableINotificationChannelString()
      {
        yield return new EmailChannel<string>();
        yield return new SmsChannel<string>();
      }

      return new NotificationService<string>(EnumerationOf_perBlockIEnumerableINotificationChannelString());
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
	NotificationServiceᐸStringᐳ --|> INotificationServiceᐸStringᐳ
	EmailChannelᐸStringᐳ --|> INotificationChannelᐸStringᐳ : "Unique tag #47"
	SmsChannelᐸStringᐳ --|> INotificationChannelᐸStringᐳ : "Unique tag #48"
	Composition ..> NotificationServiceᐸStringᐳ : INotificationServiceᐸStringᐳ NotificationService
	NotificationServiceᐸStringᐳ o-- "PerBlock" IEnumerableᐸINotificationChannelᐸStringᐳᐳ : IEnumerableᐸINotificationChannelᐸStringᐳᐳ
	IEnumerableᐸINotificationChannelᐸStringᐳᐳ *-- EmailChannelᐸStringᐳ : "Unique tag #47" INotificationChannelᐸStringᐳ
	IEnumerableᐸINotificationChannelᐸStringᐳᐳ *-- SmsChannelᐸStringᐳ : "Unique tag #48" INotificationChannelᐸStringᐳ
	namespace Pure.DI.UsageTests.Advanced.TagUniqueScenario {
		class Composition {
		<<partial>>
		+INotificationServiceᐸStringᐳ NotificationService
		}
		class EmailChannelᐸStringᐳ {
				<<class>>
			+EmailChannel()
		}
		class INotificationChannelᐸStringᐳ {
			<<interface>>
		}
		class INotificationServiceᐸStringᐳ {
			<<interface>>
		}
		class NotificationServiceᐸStringᐳ {
				<<class>>
			+NotificationService(IEnumerableᐸINotificationChannelᐸStringᐳᐳ channels)
		}
		class SmsChannelᐸStringᐳ {
				<<class>>
			+SmsChannel()
		}
	}
	namespace System.Collections.Generic {
		class IEnumerableᐸINotificationChannelᐸStringᐳᐳ {
				<<interface>>
		}
	}
```

See also:

- [Tags](tags.md)

