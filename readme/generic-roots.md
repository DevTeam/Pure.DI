#### Generic roots


```c#
using Pure.DI;

DI.Setup(nameof(Composition))
    // This hint indicates to not generate methods such as Resolve
    .Hint(Hint.Resolve, "Off")
    .Bind().To<Dependency<TT>>()
    .Bind().To<Service<TT>>()
    // Creates OtherService manually,
    // just for the sake of example
    .Bind<OtherService<TT>>().To(ctx =>
    {
        ctx.Inject(out IDependency<TT> dependency);
        return new OtherService<TT>(dependency);
    })

    // Specifies to define composition roots for all types inherited from IService<TT>
    // available at compile time at the point where the method is called
    .Roots<IService<TT>>("GetMy{type}");

var composition = new Composition();

// service = new Service<int>(new Dependency<int>());
var service = composition.GetMyService_T<int>();

// someOtherService = new OtherService<int>(new Dependency<int>());
var someOtherService = composition.GetMyOtherService_T<string>();

interface IDependency<T>;

class Dependency<T> : IDependency<T>;

interface IService<T>;

class Service<T>(IDependency<T> dependency) : IService<T>;

class OtherService<T>(IDependency<T> dependency) : IService<T>;
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

  [OrdinalAttribute(256)]
  public Composition()
  {
    _root = this;
  }

  internal Composition(Composition parentScope)
  {
    _root = (parentScope ?? throw new ArgumentNullException(nameof(parentScope)))._root;
  }

  [MethodImpl(MethodImplOptions.AggressiveInlining)]
  public OtherService<T1> GetMyOtherService_T<T1>()
  {
    OtherService<T1> transientOtherService0;
    IDependency<T1> localDependency147 = new Dependency<T1>();
    transientOtherService0 = new OtherService<T1>(localDependency147);
    return transientOtherService0;
  }

  [MethodImpl(MethodImplOptions.AggressiveInlining)]
  public Service<T1> GetMyService_T<T1>()
  {
    return new Service<T1>(new Dependency<T1>());
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
	ServiceᐸT1ᐳ --|> IServiceᐸT1ᐳ
	DependencyᐸT1ᐳ --|> IDependencyᐸT1ᐳ
	Composition ..> OtherServiceᐸT1ᐳ : OtherServiceᐸT1ᐳ GetMyOtherService_TᐸT1ᐳ()
	Composition ..> ServiceᐸT1ᐳ : ServiceᐸT1ᐳ GetMyService_TᐸT1ᐳ()
	OtherServiceᐸT1ᐳ *--  DependencyᐸT1ᐳ : IDependencyᐸT1ᐳ
	ServiceᐸT1ᐳ *--  DependencyᐸT1ᐳ : IDependencyᐸT1ᐳ
	namespace Pure.DI.UsageTests.Generics.GenericsRootsScenario {
		class Composition {
		<<partial>>
		+OtherServiceᐸT1ᐳ GetMyOtherService_TᐸT1ᐳ()
		+ServiceᐸT1ᐳ GetMyService_TᐸT1ᐳ()
		}
		class DependencyᐸT1ᐳ {
			+Dependency()
		}
		class IDependencyᐸT1ᐳ {
			<<interface>>
		}
		class IServiceᐸT1ᐳ {
			<<interface>>
		}
		class OtherServiceᐸT1ᐳ {
		}
		class ServiceᐸT1ᐳ {
			+Service(IDependencyᐸT1ᐳ dependency)
		}
	}
```

