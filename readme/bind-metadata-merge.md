#### Bind metadata merge

Shows how binding metadata attributes on one implementation type are combined into one binding. Contracts and tags placed in the same square-bracket group describe a single registration, which keeps a small shared adapter short and explicit.


```c#
using Shouldly;
using Pure.DI;

DI.Setup(nameof(Composition))

    // Composition roots
    .Root<IMessageWriter>("ConsoleWriter", "console")
    .Root<IMessageWriter>("DefaultWriter", "default")
    .Root<IDiagnosticsSink>("ConsoleDiagnostics", "console");

var composition = new Composition();

composition.ConsoleWriter.ShouldBeOfType<ConsoleChannel>();
composition.DefaultWriter.ShouldBeSameAs(composition.ConsoleWriter);
composition.ConsoleDiagnostics.ShouldBeSameAs(composition.ConsoleWriter);

interface IMessageWriter;

interface IDiagnosticsSink;

[Bind(typeof(IMessageWriter)), Bind(typeof(IDiagnosticsSink)), Tag("console"), Tag("default"), Lifetime(Lifetime.Singleton)]
class ConsoleChannel : IMessageWriter, IDiagnosticsSink;
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
>Attributes inside the same square-bracket group form one binding. Contracts and tags are merged, but lifetime must be specified at most once in the group. Separate `Bind` attribute groups create separate bindings.

Because contracts and tags are merged, `ConsoleChannel` is reachable as an `IMessageWriter` by the `"console"` and `"default"` tags and as an `IDiagnosticsSink` by `"console"`, and every route returns the same instance because of `Lifetime(Singleton)`. That is the usual shape for one infrastructure sink shared by several abstractions.
A second lifetime in the same group is a compilation error rather than a silent winner, which keeps the intent readable. If you need two instances of the same class, split the attributes into separate groups instead of trying to merge them.
>[!TIP]
>Keep the shared registrations in one group and split only when the instances really must be different.

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

  private ConsoleChannel? _singletonConsoleChannel;

  public IDiagnosticsSink ConsoleDiagnostics
  {
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    get
    {
      if (_singletonConsoleChannel is null)
        lock (_lock)
          if (_singletonConsoleChannel is null)
          {
            _singletonConsoleChannel = new ConsoleChannel();
          }

      return _singletonConsoleChannel;
    }
  }

  public IMessageWriter ConsoleWriter
  {
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    get
    {
      if (_singletonConsoleChannel is null)
        lock (_lock)
          if (_singletonConsoleChannel is null)
          {
            _singletonConsoleChannel = new ConsoleChannel();
          }

      return _singletonConsoleChannel;
    }
  }

  public IMessageWriter DefaultWriter
  {
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    get
    {
      if (_singletonConsoleChannel is null)
        lock (_lock)
          if (_singletonConsoleChannel is null)
          {
            _singletonConsoleChannel = new ConsoleChannel();
          }

      return _singletonConsoleChannel;
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
	ConsoleChannel --|> IMessageWriter : "console", "default"
	ConsoleChannel --|> IDiagnosticsSink : "console", "default"
	Composition ..> ConsoleChannel : IDiagnosticsSink ConsoleDiagnostics
	Composition ..> ConsoleChannel : IMessageWriter DefaultWriter
	Composition ..> ConsoleChannel : IMessageWriter ConsoleWriter
	namespace Pure.DI.UsageTests.Attributes.BindMetadataMergeScenario {
		class Composition {
		<<partial>>
		+IDiagnosticsSink ConsoleDiagnostics
		+IMessageWriter ConsoleWriter
		+IMessageWriter DefaultWriter
		}
		class ConsoleChannel {
				<<class>>
			+ConsoleChannel()
		}
		class IDiagnosticsSink {
			<<interface>>
		}
		class IMessageWriter {
			<<interface>>
		}
	}
```

See also:

- [Bind attribute groups](bind-attribute-groups.md)

