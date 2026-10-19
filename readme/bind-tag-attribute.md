#### Bind tag attribute

Shows how tags can be declared directly on implementation types, including several tags for one implementation. The tag becomes part of the binding, so a consumer can request the implementation through `[Tag(...)]` without the setup naming it.


```c#
using Shouldly;
using Pure.DI;

DI.Setup(nameof(Composition))

    // Composition roots
    .Root<IMessageWriter>("ConsoleWriter", "console")
    .Root<IMessageWriter>("DefaultWriter", "default")
    .Root<IMessageWriter>("FileWriter", "file");

var composition = new Composition();

composition.ConsoleWriter.ShouldBeOfType<ConsoleMessageWriter>();
composition.DefaultWriter.ShouldBeOfType<ConsoleMessageWriter>();
composition.FileWriter.ShouldBeOfType<FileMessageWriter>();

interface IMessageWriter;

[Tag("console")]
[Tag("default")]
class ConsoleMessageWriter : IMessageWriter;

[Tag("file")]
class FileMessageWriter : IMessageWriter;
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
>A tag attribute on an implementation type becomes a binding tag. Several tag attributes in the same square-bracket binding group are merged into one binding, so the same implementation can be resolved by any of those tags.

Several tags on one group are aliases for one instance: resolving by any of them returns the same binding and, with a singleton lifetime, the same object. That is useful when a legacy tag and a new tag must both keep working during a migration.
Tags declared in separate groups create separate bindings rather than aliases, which changes collection injection. Keep aliases in one group and split only for genuinely different registrations.
>[!TIP]
>Prefer an enum or a [smart tag](smart-tags.md) over string literals once more than a couple of tags exist.

<details>
<summary>The following partial class will be generated</summary>

```c#
partial class Composition
{
  public IMessageWriter FileWriter
  {
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    get
    {
      return new FileMessageWriter();
    }
  }

  public IMessageWriter ConsoleWriter
  {
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    get
    {
      return new ConsoleMessageWriter();
    }
  }

  public IMessageWriter DefaultWriter
  {
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    get
    {
      return new ConsoleMessageWriter();
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
	FileMessageWriter --|> IMessageWriter : "file"
	ConsoleMessageWriter --|> IMessageWriter : "console", "default"
	Composition ..> FileMessageWriter : IMessageWriter FileWriter
	Composition ..> ConsoleMessageWriter : IMessageWriter DefaultWriter
	Composition ..> ConsoleMessageWriter : IMessageWriter ConsoleWriter
	namespace Pure.DI.UsageTests.Attributes.BindTagAttributeScenario {
		class Composition {
		<<partial>>
		+IMessageWriter ConsoleWriter
		+IMessageWriter DefaultWriter
		+IMessageWriter FileWriter
		}
		class ConsoleMessageWriter {
				<<class>>
			+ConsoleMessageWriter()
		}
		class FileMessageWriter {
				<<class>>
			+FileMessageWriter()
		}
		class IMessageWriter {
			<<interface>>
		}
	}
```

See also:

- [Bind attribute](bind-attribute.md)

