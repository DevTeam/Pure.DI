#### Build up of an existing generic object

In other words, injecting the necessary dependencies via methods, properties, or fields into an existing object.


```c#
using Shouldly;
using Pure.DI;

DI.Setup(nameof(Composition))
    .RootArg<string>("name")
    .Bind().To(_ => Guid.NewGuid())
    .Bind().To(ctx =>
    {
        var dependency = new Dependency<TTS>();
        ctx.BuildUp(dependency);
        return dependency;
    })
    .Bind().To<Service<TTS>>()

    // Composition root
    .Root<IService<Guid>>("GetMyService");

var composition = new Composition();
var service = composition.GetMyService("Some name");
service.Dependency.Name.ShouldBe("Some name");
service.Dependency.Id.ShouldNotBe(Guid.Empty);

interface IDependency<out T>
    where T: struct
{
    string Name { get; }

    T Id { get; }
}

class Dependency<T> : IDependency<T>
    where T: struct
{
    // The Dependency attribute specifies to perform an injection
    [Dependency]
    public string Name { get; set; } = "";

    public T Id { get; private set; }

    // The Dependency attribute specifies to perform an injection
    [Dependency]
    public void SetId(T id) => Id = id;
}

interface IService<out T>
    where T: struct
{
    IDependency<T> Dependency { get; }
}

record Service<T>(IDependency<T> Dependency)
    : IService<T> where T: struct;
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
- Add references to NuGet packages
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
  public IService<Guid> GetMyService(string name)
  {
    if (name is null) throw new ArgumentNullException(nameof(name));
    Guid transientGuid2 = Guid.NewGuid();
    Dependency<Guid> transientDependency1;
    Dependency<Guid> localDependency144 = new Dependency<Guid>();
    localDependency144.Name = name;
    localDependency144.SetId(transientGuid2);
    transientDependency1 = localDependency144;
    return new Service<Guid>(transientDependency1);
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
	ServiceᐸGuidᐳ --|> IServiceᐸGuidᐳ
	ServiceᐸGuidᐳ --|> IEquatableᐸServiceᐸGuidᐳᐳ
	DependencyᐸGuidᐳ --|> IDependencyᐸGuidᐳ
	Composition ..> ServiceᐸGuidᐳ : IServiceᐸGuidᐳ GetMyService(string name)
	ServiceᐸGuidᐳ *--  DependencyᐸGuidᐳ : IDependencyᐸGuidᐳ
	DependencyᐸGuidᐳ o-- String : Argument "name"
	DependencyᐸGuidᐳ *--  Guid : Guid
	namespace Pure.DI.UsageTests.Generics.GenericBuildUpScenario {
		class Composition {
		<<partial>>
		+IServiceᐸGuidᐳ GetMyService(string name)
		}
		class DependencyᐸGuidᐳ {
				<<class>>
			+String Name
			+SetId(T3 id) : Void
		}
		class IDependencyᐸGuidᐳ {
			<<interface>>
		}
		class IServiceᐸGuidᐳ {
			<<interface>>
		}
		class ServiceᐸGuidᐳ {
				<<record>>
			+Service(IDependencyᐸGuidᐳ Dependency)
		}
	}
	namespace System {
		class Guid {
				<<struct>>
		}
		class IEquatableᐸServiceᐸGuidᐳᐳ {
			<<interface>>
		}
		class String {
				<<class>>
		}
	}
```

