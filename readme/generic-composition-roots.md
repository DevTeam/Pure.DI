#### Generic composition roots

Sometimes you want to be able to create composition roots with type parameters. In this case, the composition root can only be represented by a method.
>[!IMPORTANT]
>`Resolve()` methods cannot be used to resolve generic composition roots.


```c#
using Pure.DI;

DI.Setup(nameof(Composition))
    // Disable Resolve methods to keep the public API minimal
    .Hint(Hint.Resolve, "Off")
    .Bind().To<Repository<TT>>()
    .Bind().To<CreateCommandHandler<TT>>()
    // Creates UpdateCommandHandler manually,
    // just for the sake of example
    .Bind("Update").To(ctx => {
        ctx.Inject(out IRepository<TT> repository);
        return new UpdateCommandHandler<TT>(repository);
    })

    // Specifies to create a regular public method
    // to get a composition root of type ICommandHandler<T>
    // with the name "GetCreateCommandHandler"
    .Root<ICommandHandler<TT>>("GetCreateCommandHandler")

    // Specifies to create a regular public method
    // to get a composition root of type ICommandHandler<T>
    // with the name "GetUpdateCommandHandler"
    // using the "Update" tag
    .Root<ICommandHandler<TT>>("GetUpdateCommandHandler", "Update");

var composition = new Composition();

// createHandler = new CreateCommandHandler<int>(new Repository<int>());
var createHandler = composition.GetCreateCommandHandler<int>();

// updateHandler = new UpdateCommandHandler<string>(new Repository<string>());
var updateHandler = composition.GetUpdateCommandHandler<string>();

interface IRepository<T>;

class Repository<T> : IRepository<T>;

interface ICommandHandler<T>;

class CreateCommandHandler<T>(IRepository<T> repository) : ICommandHandler<T>;

class UpdateCommandHandler<T>(IRepository<T> repository) : ICommandHandler<T>;
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
- Add a reference to the NuGet package
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

>[!IMPORTANT]
>The method `Inject()` cannot be used outside of the binding setup.

The following partial class will be generated:

```c#
partial class Composition
{
  [MethodImpl(MethodImplOptions.AggressiveInlining)]
  public ICommandHandler<T1> GetUpdateCommandHandler<T1>()
  {
    UpdateCommandHandler<T1> transientUpdateCommandHandler525;
    IRepository<T1> localRepository = new Repository<T1>();
    transientUpdateCommandHandler525 = new UpdateCommandHandler<T1>(localRepository);
    return transientUpdateCommandHandler525;
  }

  [MethodImpl(MethodImplOptions.AggressiveInlining)]
  public ICommandHandler<T1> GetCreateCommandHandler<T1>()
  {
    return new CreateCommandHandler<T1>(new Repository<T1>());
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
	UpdateCommandHandlerᐸT1ᐳ --|> ICommandHandlerᐸT1ᐳ : "Update" 
	CreateCommandHandlerᐸT1ᐳ --|> ICommandHandlerᐸT1ᐳ
	RepositoryᐸT1ᐳ --|> IRepositoryᐸT1ᐳ
	Composition ..> UpdateCommandHandlerᐸT1ᐳ : ICommandHandlerᐸT1ᐳ GetUpdateCommandHandlerᐸT1ᐳ()
	Composition ..> CreateCommandHandlerᐸT1ᐳ : ICommandHandlerᐸT1ᐳ GetCreateCommandHandlerᐸT1ᐳ()
	UpdateCommandHandlerᐸT1ᐳ *--  RepositoryᐸT1ᐳ : IRepositoryᐸT1ᐳ
	CreateCommandHandlerᐸT1ᐳ *--  RepositoryᐸT1ᐳ : IRepositoryᐸT1ᐳ
	namespace Pure.DI.UsageTests.Generics.GenericsCompositionRootsScenario {
		class Composition {
		<<partial>>
		+ICommandHandlerᐸT1ᐳ GetCreateCommandHandlerᐸT1ᐳ()
		+ICommandHandlerᐸT1ᐳ GetUpdateCommandHandlerᐸT1ᐳ()
		}
		class CreateCommandHandlerᐸT1ᐳ {
				<<class>>
			+CreateCommandHandler(IRepositoryᐸT1ᐳ repository)
		}
		class ICommandHandlerᐸT1ᐳ {
			<<interface>>
		}
		class IRepositoryᐸT1ᐳ {
			<<interface>>
		}
		class RepositoryᐸT1ᐳ {
				<<class>>
			+Repository()
		}
		class UpdateCommandHandlerᐸT1ᐳ {
				<<class>>
		}
	}
```

