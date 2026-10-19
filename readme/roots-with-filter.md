#### Roots with filter

`Roots<T>(name, filter)` creates a composition root for every implementation of `T` whose type name matches a wildcard filter, with `{type}` in the name template replaced by each type's name.
Filtering is how a plugin host exposes only the implementations it can actually build: here `filter: "*Email*"` picks up `EmailService` and skips `SmsService`, whose `string apiKey` dependency has no binding and therefore cannot be resolved.


```c#
using Shouldly;
using Pure.DI;

DI.Setup(nameof(Composition))
    .Bind().As(Lifetime.Singleton).To<Configuration>()
    .Roots<INotificationService>("My{type}", filter: "*Email*");

var composition = new Composition();
composition.MyEmailService.ShouldBeOfType<EmailService>();

interface IConfiguration;

class Configuration : IConfiguration;

interface INotificationService;

// This service requires an API key which is not bound,
// so it cannot be resolved and should be filtered out.
class SmsService(string apiKey) : INotificationService;

class EmailService(IConfiguration config) : INotificationService;
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

Without a filter, an implementation that the graph cannot satisfy fails the compilation; the filter is therefore a statement about which implementations are in scope, not a cosmetic option.
The filter is a case-sensitive wildcard over the type name and is evaluated at compile time; a filter that matches nothing is reported as `DIE016`.
Every generated root is an ordinary property or method, so name the template with a recognizable prefix (`My{type}`) to keep generated members distinct from hand-written ones.
Prefer `Roots<T>` when implementations are discovered from the assembly; use explicit `Root<T>` calls when the set is small and stable.

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

  private Configuration? _singletonWcfLikeChannel;

  public EmailService MyEmailService
  {
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    get
    {
      if (_singletonWcfLikeChannel is null)
        lock (_lock)
          if (_singletonWcfLikeChannel is null)
          {
            _singletonWcfLikeChannel = new Configuration();
          }

      return new EmailService(_singletonWcfLikeChannel);
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
	Configuration --|> IConfiguration
	Composition ..> EmailService : EmailService MyEmailService
	EmailService o-- "Singleton" Configuration : IConfiguration
	namespace Pure.DI.UsageTests.Basics.RootsWithFilterScenario {
		class Composition {
		<<partial>>
		+EmailService MyEmailService
		}
		class Configuration {
				<<class>>
			+Configuration()
		}
		class EmailService {
				<<class>>
			+EmailService(IConfiguration config)
		}
		class IConfiguration {
			<<interface>>
		}
	}
```

See also:

- [Roots](roots.md)

