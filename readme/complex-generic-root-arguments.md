#### Complex generic root arguments

[![CSharp](https://img.shields.io/badge/C%23-code-blue.svg)](../tests/Pure.DI.UsageTests/Generics/ComplexGenericRootArgScenario.cs)


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
  private readonly Composition _root;

  [OrdinalAttribute(128)]
  public Composition()
  {
    _root = this;
  }

  internal Composition(Composition parentScope)
  {
    _root = (parentScope ?? throw new ArgumentNullException(nameof(parentScope)))._root;
  }

  [MethodImpl(MethodImplOptions.AggressiveInlining)]
  public IService<T> GetMyService<T>(MyData<T> complexArg)
  {
    if (Object.ReferenceEquals(complexArg, null)) throw new ArgumentNullException(nameof(complexArg));
    Service<T> transientService0 = new Service<T>();
    transientService0.SetDependency(complexArg);
    return transientService0;
  }
}
```

Class diagram:

```mermaid
---
 config:
  class:
   hideEmptyMembersBox: true
---
classDiagram
	ServiceᐸTᐳ --|> IServiceᐸTᐳ
	Composition ..> ServiceᐸTᐳ : IServiceᐸTᐳ GetMyServiceᐸTᐳ(Pure.DI.UsageTests.Basics.ComplexGenericRootArgScenario.MyData<T> complexArg)
	ServiceᐸTᐳ o-- MyDataᐸTᐳ : Argument "complexArg"
	namespace Pure.DI.UsageTests.Basics.ComplexGenericRootArgScenario {
		class Composition {
		<<partial>>
		+IServiceᐸTᐳ GetMyServiceᐸTᐳ(Pure.DI.UsageTests.Basics.ComplexGenericRootArgScenario.MyData<T> complexArg)
		}
		class IServiceᐸTᐳ {
			<<interface>>
		}
		class MyDataᐸTᐳ {
				<<record>>
		}
		class ServiceᐸTᐳ {
			+Service()
			+SetDependency(MyDataᐸTᐳ data) : Void
		}
	}
```

