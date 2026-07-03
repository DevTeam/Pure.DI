#### Generic builder

Builders can be generic as well. `Builder<ViewModel<TTS, TT2>>("BuildUp")` generates a generic `BuildUp` method that injects dependencies into an instance you already have — handy when objects are created by an external framework (a UI library, a serializer) rather than by the composition.
The marker types define the method's type parameters: `TTS` matches the `struct` constraint on `TId`, and `TT2` stands for the model type.


```c#
using Shouldly;
using Pure.DI;

DI.Setup(nameof(Composition))
    .Bind(Tag.Id).To(() => (TT)(object)Guid.NewGuid())
    .Bind().To<Repository<TT>>()
    // Generic service builder
    // Defines a generic builder "BuildUp".
    // This is useful when instances are created by an external framework
    // (like a UI library or serialization) but require dependencies to be injected.
    .Builder<ViewModel<TTS, TT2>>("BuildUp");

var composition = new Composition();

// A view model instance created manually (or by a UI framework)
var viewModel = new ViewModel<Guid, Customer>();

// Inject dependencies (Id and Repository) into the existing instance
var builtViewModel = composition.BuildUp(viewModel);

builtViewModel.Id.ShouldNotBe(Guid.Empty);
builtViewModel.Repository.ShouldBeOfType<Repository<Customer>>();

// Domain model
record Customer;

interface IRepository<T>;

class Repository<T> : IRepository<T>;

interface IViewModel<out TId, TModel>
{
    TId Id { get; }

    IRepository<TModel>? Repository { get; }
}

// The view model is generic, allowing it to be used for various entities
record ViewModel<TId, TModel> : IViewModel<TId, TModel>
    where TId : struct
{
    public TId Id { get; private set; }

    // The dependency to be injected
    [Dependency]
    public IRepository<TModel>? Repository { get; set; }

    // Method injection for the ID
    [Dependency]
    public void SetId([Tag(Tag.Id)] TId id) => Id = id;
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
>Generic builders enable flexible object initialization while maintaining type safety across different generic types.

<details>
<summary>The following partial class will be generated</summary>

```c#
partial class Composition
{
  [MethodImpl(MethodImplOptions.AggressiveInlining)]
  public ViewModel<T3, T4> BuildUp<T3, T4>(ViewModel<T3, T4> buildingInstance)
    where T3: struct
  {
    if (buildingInstance is null) throw new ArgumentNullException(nameof(buildingInstance));
    ViewModel<T3, T4> transientViewModelTTSTT2;
    ViewModel<T3, T4> localBuildingInstance = buildingInstance;
    T3 transientTTS = (T3)(object)Guid.NewGuid();
    localBuildingInstance.Repository = new Repository<T4>();
    localBuildingInstance.SetId(transientTTS);
    transientViewModelTTSTT2 = localBuildingInstance;
    return transientViewModelTTSTT2;
  }
}
```

</details>

Class diagram:

```mermaid
---
 config:
  class:
   hideEmptyMembersBox: true
---
classDiagram
	RepositoryᐸT4ᐳ --|> IRepositoryᐸT4ᐳ
	Composition ..> ViewModelᐸT3ˏT4ᐳ : ViewModelᐸT3ˏT4ᐳ BuildUpᐸT3ˏT4ᐳ(Pure.DI.UsageTests.Generics.GenericBuilderScenario.ViewModel<T3, T4> buildingInstance)
	ViewModelᐸT3ˏT4ᐳ *-- RepositoryᐸT4ᐳ : IRepositoryᐸT4ᐳ
	ViewModelᐸT3ˏT4ᐳ *-- T3 : "Id" T3
	namespace Pure.DI.UsageTests.Generics.GenericBuilderScenario {
		class Composition {
		<<partial>>
		+ViewModelᐸT3ˏT4ᐳ BuildUpᐸT3ˏT4ᐳ(Pure.DI.UsageTests.Generics.GenericBuilderScenario.ViewModel<T3, T4> buildingInstance)
		}
		class IRepositoryᐸT4ᐳ {
			<<interface>>
		}
		class RepositoryᐸT4ᐳ {
				<<class>>
			+Repository()
		}
		class ViewModelᐸT3ˏT4ᐳ {
				<<record>>
			+IRepositoryᐸT4ᐳɁ Repository
			+SetId(T3 id) : Void
		}
	}
```

See also:

- [Builder](builder.md)
- [Generic builders](generic-builders.md)

