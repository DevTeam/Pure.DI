#### Lazy

Injecting `Lazy<T>` defers creation of a dependency until its `Value` property is first accessed, after which the same instance is returned every time. No extra setup is needed: bind the underlying type as usual and request `Lazy<T>` in the constructor.


```c#
using Shouldly;
using Pure.DI;

DI.Setup(nameof(Composition))
    .Bind<IGraphicsEngine>().To<GraphicsEngine>()
    .Bind<IWindow>().To<Window>()

    // Composition root
    .Root<IWindow>("Window");

var composition = new Composition();
var window = composition.Window;

// The graphics engine is created only when it is first accessed
window.Engine.ShouldBe(window.Engine);

interface IGraphicsEngine;

class GraphicsEngine : IGraphicsEngine;

interface IWindow
{
    IGraphicsEngine Engine { get; }
}

class Window(Lazy<IGraphicsEngine> engine) : IWindow
{
    public IGraphicsEngine Engine => engine.Value;
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
>Lazy<T> is useful for expensive-to-create objects or when the instance may never be needed, improving application startup performance.

<details>
<summary>The following partial class will be generated</summary>

```c#
partial class Composition
{
  public IWindow Window
  {
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    get
    {
      Lazy<IGraphicsEngine> transientLazyIGraphicsEngine;
      // Creates a lazy value factory
      Func<IGraphicsEngine> perBlockFuncIGraphicsEngine = new Func<IGraphicsEngine>(
      [MethodImpl(MethodImplOptions.AggressiveInlining)]
      () =>
      {
        // Creates a deferred value
        return new GraphicsEngine();
      });
      Func<IGraphicsEngine> localFactory = perBlockFuncIGraphicsEngine;
      // Wraps it in Lazy<T>
      transientLazyIGraphicsEngine = new Lazy<IGraphicsEngine>(localFactory, true);
      return new Window(transientLazyIGraphicsEngine);
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
	GraphicsEngine --|> IGraphicsEngine
	Window --|> IWindow
	Composition ..> Window : IWindow Window
	Window *-- LazyᐸIGraphicsEngineᐳ : LazyᐸIGraphicsEngineᐳ
	LazyᐸIGraphicsEngineᐳ o-- "PerBlock" FuncᐸIGraphicsEngineᐳ : FuncᐸIGraphicsEngineᐳ
	FuncᐸIGraphicsEngineᐳ *-- GraphicsEngine : IGraphicsEngine
	namespace Pure.DI.UsageTests.BCL.LazyScenario {
		class Composition {
		<<partial>>
		+IWindow Window
		}
		class GraphicsEngine {
				<<class>>
			+GraphicsEngine()
		}
		class IGraphicsEngine {
			<<interface>>
		}
		class IWindow {
			<<interface>>
		}
		class Window {
				<<class>>
			+Window(LazyᐸIGraphicsEngineᐳ engine)
		}
	}
	namespace System {
		class FuncᐸIGraphicsEngineᐳ {
				<<delegate>>
		}
		class LazyᐸIGraphicsEngineᐳ {
				<<class>>
		}
	}
```

See also:

- [Func](func.md)
- [Manually started tasks](manually-started-tasks.md)

