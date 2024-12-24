#### Build up of an existing generic object

[![CSharp](https://img.shields.io/badge/C%23-code-blue.svg)](../tests/Pure.DI.UsageTests/Generics/GenericBuildUpScenario.cs)

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
    // The Ordinal attribute specifies to perform an injection and its order
    [Ordinal(1)]
    public string Name { get; set; } = "";

    public T Id { get; private set; }

    // The Ordinal attribute specifies to perform an injection and its order
    [Ordinal(0)]
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
- Create a net9.0 (or later) console application
- Add references to NuGet packages
  - [Pure.DI](https://www.nuget.org/packages/Pure.DI)
  - [Shouldly](https://www.nuget.org/packages/Shouldly)
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
	ServiceᐸGuidᐳ --|> IServiceᐸGuidᐳ
	ServiceᐸGuidᐳ --|> IEquatableᐸServiceᐸGuidᐳᐳ
	DependencyᐸGuidᐳ --|> IDependencyᐸGuidᐳ
	Composition ..> ServiceᐸGuidᐳ : IServiceᐸGuidᐳ GetMyService(string name)
	ServiceᐸGuidᐳ *--  DependencyᐸGuidᐳ : IDependencyᐸGuidᐳ
	DependencyᐸGuidᐳ o-- String : Argument "name"
	DependencyᐸGuidᐳ *--  Guid : Guid
	namespace Pure.DI.UsageTests.Basics.GenericBuildUpScenario {
		class Composition {
		<<partial>>
		+IServiceᐸGuidᐳ GetMyService(string name)
		}
		class DependencyᐸGuidᐳ {
			+String Name
			+SetId(T id) : Void
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
		}
	}
```

