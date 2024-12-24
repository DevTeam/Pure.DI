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
    // The Ordinal attribute specifies to perform an injection,
    // the integer value in the argument specifies
    // the ordinal of injection
    [Ordinal(0)]
    public void SetDependency(T dependency) =>
        Dependency = dependency;

    public T? Dependency { get; private set; }
}
```

<details>
<summary>Running this code sample locally</summary>

- Make sure you have the [.NET SDK 9.0](https://dotnet.microsoft.com/en-us/download/dotnet/9.0) or later is installed
- Create a net9.0 (or later) console application
- Add reference to NuGet package
  - [Pure.DI](https://www.nuget.org/packages/Pure.DI)
- Copy the example code into the _Program.cs_ file

You are ready to run the example!

</details>


Class diagram:

```mermaid
---
 config:
  class:
   hideEmptyMembersBox: true
---
classDiagram
	ServiceᐸT1ᐳ --|> IServiceᐸT1ᐳ
	Composition ..> ServiceᐸT1ᐳ : IServiceᐸT1ᐳ GetMyServiceᐸT1ᐳ(T1 someArg)
	ServiceᐸT1ᐳ o-- T1 : Argument "someArg"
	namespace Pure.DI.UsageTests.Basics.GenericRootArgScenario {
		class Composition {
		<<partial>>
		+IServiceᐸT1ᐳ GetMyServiceᐸT1ᐳ(T1 someArg)
		}
		class IServiceᐸT1ᐳ {
			<<interface>>
		}
		class ServiceᐸT1ᐳ {
			+Service()
			+SetDependency(T1 dependency) : Void
		}
	}
```

