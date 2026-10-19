#### Decorator

Interception is the ability to intercept calls between objects in order to enrich or change their behavior, but without having to change their code. A prerequisite for interception is weak binding. That is, if programming is abstraction-based, the underlying implementation can be transformed or improved by "packaging" it into other implementations of the same abstraction. At its core, intercept is an application of the Decorator design pattern. This pattern provides a flexible alternative to inheritance by dynamically "attaching" additional responsibility to an object. Decorator "packs" one implementation of an abstraction into another implementation of the same abstraction like a "matryoshka doll".
`Decorator` is a well-known and useful design pattern. It is convenient to use tagged dependencies to build a chain of nested decorators, as in the example below:


```c#
using Shouldly;
using Pure.DI;

DI.Setup(nameof(Composition))
    .Bind("base").To<TextWidget>()
    .Bind().To<BoxWidget>()
    .Root<IWidget>("Widget");

var composition = new Composition();
var widget = composition.Widget;
widget.Render().ShouldBe("[ Hello World ]");

interface IWidget
{
    string Render();
}

class TextWidget : IWidget
{
    public string Render() => "Hello World";
}

class BoxWidget([Tag("base")] IWidget baseWidget) : IWidget
{
    public string Render() => $"[ {baseWidget.Render()} ]";
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

Here an instance of the `TextWidget` type, labeled `"base"`, is injected in the decorator `BoxWidget`. You can use any tag that semantically reflects the feature of the abstraction being embedded. The tag can be a constant, a type, or a value of an enumerated type.

This is the default way to add behavior to a contract in a real project - caching, retries, validation, redaction, metrics - because the decorator is a plain class: it is resolved, tested and profiled like any other, and the composition shows the whole chain in one place.
A chain grows by adding one more binding and one more tag, and the outermost untagged binding is what consumers receive. Keep the tags stable: renaming a tag silently binds the decorator to a different implementation.
>[!NOTE]
>The decorator inherits the lifetime of its binding, and it captures the inner instance. A singleton decorator around a transient inner instance freezes that single inner instance for the composition's whole life.

Prefer a decorator whenever the behavior can be written as a normal implementation of the contract. See [Interception](interception.md) when the behavior must be applied to types you do not own or cannot wrap by hand.

<details>
<summary>The following partial class will be generated</summary>

```c#
partial class Composition
{
  public IWidget Widget
  {
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    get
    {
      return new BoxWidget(new TextWidget());
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
	TextWidget --|> IWidget : "base"
	BoxWidget --|> IWidget
	Composition ..> BoxWidget : IWidget Widget
	BoxWidget *-- TextWidget : "base" IWidget
	namespace Pure.DI.UsageTests.Interception.DecoratorScenario {
		class BoxWidget {
				<<class>>
			+BoxWidget(IWidget baseWidget)
		}
		class Composition {
		<<partial>>
		+IWidget Widget
		}
		class IWidget {
			<<interface>>
		}
		class TextWidget {
				<<class>>
			+TextWidget()
		}
	}
```

See also:

- [Interception](interception.md)

