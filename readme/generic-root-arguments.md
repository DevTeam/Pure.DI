#### Generic root arguments

[![CSharp](https://img.shields.io/badge/C%23-code-blue.svg)](../tests/Pure.DI.UsageTests/Generics/GenericRootArgScenario.cs)


```c#
using Pure.DI;

DI.Setup(nameof(Composition))
    .RootArg<TT>("someArg")
    .Bind<IService<TT>>().To<Service<TT>>()

    // Composition root
    .Root<IService<TT>>("GetMyService");

var composition = new Composition();
IService<int> service = composition.GetMyService<int>(someArg: 33);

interface IService<out T>
{
    T? Dependency { get; }
}

class Service<T> : IService<T>
{
    // The Dependency attribute specifies to perform an injection,
    // the integer value in the argument specifies
    // the ordinal of injection
    [Dependency]
    public void SetDependency(T dependency) =>
        Dependency = dependency;

    public T? Dependency { get; private set; }
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
  public IService<T3> GetMyService<T3>(T3 someArg)
  {
    if (Object.ReferenceEquals(someArg, null)) throw new ArgumentNullException(nameof(someArg));
    Service<T3> transientService0 = new Service<T3>();
    transientService0.SetDependency(someArg);
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
	ServiceᐸT3ᐳ --|> IServiceᐸT3ᐳ
	Composition ..> ServiceᐸT3ᐳ : IServiceᐸT3ᐳ GetMyServiceᐸT3ᐳ(T3 someArg)
	ServiceᐸT3ᐳ o-- T3 : Argument "someArg"
	namespace Pure.DI.UsageTests.Basics.GenericRootArgScenario {
		class Composition {
		<<partial>>
		+IServiceᐸT3ᐳ GetMyServiceᐸT3ᐳ(T3 someArg)
		}
		class IServiceᐸT3ᐳ {
			<<interface>>
		}
		class ServiceᐸT3ᐳ {
			+Service()
			+SetDependency(T3 dependency) : Void
		}
	}
```

