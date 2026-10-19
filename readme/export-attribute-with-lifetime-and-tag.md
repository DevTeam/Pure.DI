#### Export attribute with lifetime and tag

The `[Export]` attribute accepts optional `lifetime` and `tags` parameters, so an exported member is registered exactly like a hand-written binding. Here the `GraphicsAdapter.HighPerfGpu` property is exported as a `Singleton` with the tag `"HighPerformance"`, and `RayTracer` receives that instance by requesting `[Tag("HighPerformance")] IGpu`.


```c#
using Shouldly;
using Pure.DI;

DI.Setup(nameof(Composition))
    .Bind().As(Lifetime.Singleton).To<GraphicsAdapter>()
    .Bind().To<RayTracer>()

    // Composition root
    .Root<IRenderer>("Renderer");

var composition = new Composition();
var renderer = composition.Renderer;
renderer.Render();

interface IGpu
{
    void RenderFrame();
}

class DiscreteGpu : IGpu
{
    public void RenderFrame() => Console.WriteLine("Rendering with Discrete GPU");
}

class GraphicsAdapter
{
    // Binds the property to the composition with the specified
    // lifetime and tag. This allows the "HighPerformance" GPU
    // to be injected into other components.
    [Export(lifetime: Lifetime.Singleton, tags: ["HighPerformance"])]
    public IGpu HighPerfGpu { get; } = new DiscreteGpu();
}

interface IRenderer
{
    void Render();
}

class RayTracer([Tag("HighPerformance")] IGpu gpu) : IRenderer
{
    public void Render() => gpu.RenderFrame();
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
>Specifying lifetime and tag in the Export attribute allows for fine-grained control over instance creation and binding resolution.

Because the exported value is tagged, it does not compete with an untagged `IGpu` binding, and a consumer has to ask for it explicitly through `[Tag("HighPerformance")]`. That keeps an expensive or optional capability from becoming the default by accident.
The tag on `Export` and the tag on the injection site must match exactly; a mismatch leaves the contract unresolved at compile time. With `Lifetime.Singleton` on the export, every consumer shares the one property value the adapter already holds.

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

  private IGpu? _singletonIGpu;
  private GraphicsAdapter? _singletonWcfLikeChannel;

  public IRenderer Renderer
  {
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    get
    {
      if (_singletonIGpu is null)
        lock (_lock)
          if (_singletonIGpu is null)
          {
            if (_singletonWcfLikeChannel is null)
            {
              _singletonWcfLikeChannel = new GraphicsAdapter();
            }

            GraphicsAdapter localInstance_1182D127 = _singletonWcfLikeChannel;
            _singletonIGpu = localInstance_1182D127.HighPerfGpu;
          }

      return new RayTracer(_singletonIGpu);
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
	RayTracer --|> IRenderer
	Composition ..> RayTracer : IRenderer Renderer
	RayTracer o-- "Singleton" IGpu : "HighPerformance" IGpu
	IGpu o-- "Singleton" GraphicsAdapter : GraphicsAdapter
	namespace Pure.DI.UsageTests.Basics.ExportAttributeWithLifetimeAndTagScenario {
		class Composition {
		<<partial>>
		+IRenderer Renderer
		}
		class GraphicsAdapter {
				<<class>>
			+GraphicsAdapter()
		}
		class IGpu {
				<<interface>>
		}
		class IRenderer {
			<<interface>>
		}
		class RayTracer {
				<<class>>
			+RayTracer(IGpu gpu)
		}
	}
```

See also:

- [Export attribute](export-attribute.md)

