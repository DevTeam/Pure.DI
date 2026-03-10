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
  public ICommandHandler<T2> GetUpdateCommandHandler<T2>()
  {
    UpdateCommandHandler<T2> transientUpdateCommandHandler490;
    IRepository<T2> localRepository = new Repository<T2>();
    transientUpdateCommandHandler490 = new UpdateCommandHandler<T2>(localRepository);
    return transientUpdateCommandHandler490;
  }

  [MethodImpl(MethodImplOptions.AggressiveInlining)]
  public ICommandHandler<T2> GetCreateCommandHandler<T2>()
  {
    return new CreateCommandHandler<T2>(new Repository<T2>());
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
	UpdateCommandHandlerᐸT2ᐳ --|> ICommandHandlerᐸT2ᐳ : "Update" 
	CreateCommandHandlerᐸT2ᐳ --|> ICommandHandlerᐸT2ᐳ
	RepositoryᐸT2ᐳ --|> IRepositoryᐸT2ᐳ
	Composition ..> UpdateCommandHandlerᐸT2ᐳ : ICommandHandlerᐸT2ᐳ GetUpdateCommandHandlerᐸT2ᐳ()
	Composition ..> CreateCommandHandlerᐸT2ᐳ : ICommandHandlerᐸT2ᐳ GetCreateCommandHandlerᐸT2ᐳ()
	UpdateCommandHandlerᐸT2ᐳ *--  RepositoryᐸT2ᐳ : IRepositoryᐸT2ᐳ
	CreateCommandHandlerᐸT2ᐳ *--  RepositoryᐸT2ᐳ : IRepositoryᐸT2ᐳ
	namespace Pure.DI.UsageTests.Generics.GenericsCompositionRootsScenario {
		class Composition {
		<<partial>>
		+ICommandHandlerᐸT2ᐳ GetCreateCommandHandlerᐸT2ᐳ()
		+ICommandHandlerᐸT2ᐳ GetUpdateCommandHandlerᐸT2ᐳ()
		}
		class CreateCommandHandlerᐸT2ᐳ {
				<<class>>
			+CreateCommandHandler(IRepositoryᐸT2ᐳ repository)
		}
		class ICommandHandlerᐸT2ᐳ {
			<<interface>>
		}
		class IRepositoryᐸT2ᐳ {
			<<interface>>
		}
		class RepositoryᐸT2ᐳ {
				<<class>>
			+Repository()
		}
		class UpdateCommandHandlerᐸT2ᐳ {
				<<class>>
		}
	}
```

