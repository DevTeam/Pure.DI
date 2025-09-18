#### Complex generic root arguments


```c#
using Pure.DI;

DI.Setup(nameof(Composition))
    .RootArg<MyData<TT>>("complexArg")
    .Bind<IService<TT2>>().To<Service<TT2>>()

    // Composition root
    .Root<IService<TT3>>("GetMyService");

var composition = new Composition();
IService<int> service = composition.GetMyService<int>(
    new MyData<int>(33, "Just contains an integer value 33"));

record MyData<T>(T Value, string Description);

interface IService<out T>
{
    T? Val { get; }
}

class Service<T> : IService<T>
{
    // The Dependency attribute specifies to perform an injection,
    // the integer value in the argument specifies
    // the ordinal of injection
    [Dependency]
    public void SetDependency(MyData<T> data) =>
        Val = data.Value;

    public T? Val { get; private set; }
}
```

<details>
<summary>Running this code sample locally</summary>

- Make sure you have the [.NET SDK 9.0](https://dotnet.microsoft.com/en-us/download/dotnet/9.0) or later is installed
```bash
dotnet --list-sdk
```
- Create a net9.0 (or later) console application
```bash
dotnet new console -n Sample
```
- Add reference to NuGet package
  - [Pure.DI](https://www.nuget.org/packages/Pure.DI)
```bash
dotnet add package Pure.DI
```
- Copy the example code into the _Program.cs_ file

You are ready to run the example 🚀
```bash
dotnet run
```

</details>

The following partial class will be generated:

```c#
partial class Composition
{
#if NET9_0_OR_GREATER
  private readonly Lock _lock;
#else
  private readonly Object _lock;
#endif

  [OrdinalAttribute(256)]
  public Composition()
  {
#if NET9_0_OR_GREATER
    _lock = new Lock();
#else
    _lock = new Object();
#endif
  }

  internal Composition(Composition parentScope)
  {
    _lock = parentScope._lock;
  }

  [MethodImpl(MethodImplOptions.AggressiveInlining)]
  public IService<T> GetMyService<T>(MyData<T> complexArg)
  {
    if (complexArg is null) throw new ArgumentNullException(nameof(complexArg));
    var transientService = new Service<T>();
    transientService.SetDependency(complexArg);
    return transientService;
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
	ServiceᐸTᐳ --|> IServiceᐸTᐳ
	Composition ..> ServiceᐸTᐳ : IServiceᐸTᐳ GetMyServiceᐸTᐳ(Pure.DI.UsageTests.Generics.ComplexGenericRootArgScenario.MyData<T> complexArg)
	ServiceᐸTᐳ o-- MyDataᐸTᐳ : Argument "complexArg"
	namespace Pure.DI.UsageTests.Generics.ComplexGenericRootArgScenario {
		class Composition {
		<<partial>>
		+IServiceᐸTᐳ GetMyServiceᐸTᐳ(Pure.DI.UsageTests.Generics.ComplexGenericRootArgScenario.MyData<T> complexArg)
		}
		class IServiceᐸTᐳ {
			<<interface>>
		}
		class MyDataᐸTᐳ {
				<<record>>
		}
		class ServiceᐸTᐳ {
				<<class>>
			+Service()
			+SetDependency(MyDataᐸTᐳ data) : Void
		}
	}
```

