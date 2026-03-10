#### Generic root arguments

Demonstrates how to pass type arguments as parameters to generic composition roots.


```c#
using Shouldly;
using Pure.DI;

DI.Setup(nameof(Composition))
    .RootArg<TT>("model")
    .Bind<IPresenter<TT>>().To<Presenter<TT>>()

    // Composition root
    .Root<IPresenter<TT>>("GetPresenter");

var composition = new Composition();

// The "model" argument is passed to the composition root
// and then injected into the "Presenter" class
var presenter = composition.GetPresenter<string>(model: "Hello World");

presenter.Model.ShouldBe("Hello World");

interface IPresenter<out T>
{
    T? Model { get; }
}

class Presenter<T> : IPresenter<T>
{
    // The Dependency attribute specifies to perform an injection
    [Dependency]
    public void Present(T model) =>
        Model = model;

    public T? Model { get; private set; }
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
>Generic root arguments enable flexible type parameterization while maintaining compile-time type safety.

The following partial class will be generated:

```c#
partial class Composition
{
#if NET9_0_OR_GREATER
  private readonly Lock _lock = new Lock();
#else
  private readonly Object _lock = new Object();
#endif

  [MethodImpl(MethodImplOptions.AggressiveInlining)]
  public IPresenter<T2> GetPresenter<T2>(T2 model)
  {
    if (model is null) throw new ArgumentNullException(nameof(model));
    var transientPresenter488 = new Presenter<T2>();
    transientPresenter488.Present(model);
    return transientPresenter488;
  }
}
```

Class diagram:

```mermaid
---
 config:
  maxTextSize: 2147483647
  maxEdges: 2147483647
  class:
   hideEmptyMembersBox: true
---
classDiagram
	PresenterᐸT2ᐳ --|> IPresenterᐸT2ᐳ
	Composition ..> PresenterᐸT2ᐳ : IPresenterᐸT2ᐳ GetPresenterᐸT2ᐳ(T2 model)
	PresenterᐸT2ᐳ o-- T2 : Argument "model"
	namespace Pure.DI.UsageTests.Generics.GenericRootArgScenario {
		class Composition {
		<<partial>>
		+IPresenterᐸT2ᐳ GetPresenterᐸT2ᐳ(T2 model)
		}
		class IPresenterᐸT2ᐳ {
			<<interface>>
		}
		class PresenterᐸT2ᐳ {
				<<class>>
			+Presenter()
			+Present(T2 model) : Void
		}
	}
```

