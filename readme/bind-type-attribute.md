#### Bind type attribute

Shows how the `Type` attribute can declare a contract directly on an implementation type.


```c#
using Shouldly;
using Pure.DI;

DI.Setup(nameof(Composition))

    // Composition root
    .Root<IMessageWriter>("Writer");

var composition = new Composition();
var writer = composition.Writer;

writer.ShouldBeOfType<ConsoleMessageWriter>();

interface IMessageWriter;

[Type(typeof(IMessageWriter))]
class ConsoleMessageWriter : IMessageWriter;
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
>When a registered type attribute is applied to a class or struct, Pure.DI treats it as binding metadata for that implementation.

<details>
<summary>The following partial class will be generated</summary>

```c#
partial class Composition
{
  public IMessageWriter Writer
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
	ConsoleMessageWriter --|> IMessageWriter
	Composition ..> ConsoleMessageWriter : IMessageWriter Writer
	namespace Pure.DI.UsageTests.Attributes.BindTypeAttributeScenario {
		class Composition {
		<<partial>>
		+IMessageWriter Writer
		}
		class ConsoleMessageWriter {
				<<class>>
			+ConsoleMessageWriter()
		}
		class IMessageWriter {
			<<interface>>
		}
	}
```

See also:

- [Bind attribute](bind-attribute.md)
- [Bind type attributes](bind-type-attributes.md)

