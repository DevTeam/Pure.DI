#### Generic builder

[![CSharp](https://img.shields.io/badge/C%23-code-blue.svg)](../tests/Pure.DI.UsageTests/Generics/GenericBuilderScenario.cs)


```c#
using Shouldly;
using Pure.DI;

DI.Setup(nameof(Composition))
    .Bind(Tag.Id).To<TT>(_ => (TT)(object)Guid.NewGuid())
    .Bind().To<Dependency<TT>>()
    // Generic service builder
    .Builder<Service<TTS, TT2>>("BuildUpGeneric");

var composition = new Composition();
var service = composition.BuildUpGeneric(new Service<Guid, string>());
service.Id.ShouldNotBe(Guid.Empty);
service.Dependency.ShouldBeOfType<Dependency<string>>();

interface IDependency<T>;

class Dependency<T> : IDependency<T>;

interface IService<out T, T2>
{
    T Id { get; }

    IDependency<T2>? Dependency { get; }
}

record Service<T, T2>: IService<T, T2>
    where T: struct
{
    public T Id { get; private set; }

    [Dependency]
    public IDependency<T2>? Dependency { get; set; }

    [Dependency]
    public void SetId([Tag(Tag.Id)] T id) => Id = id;
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
  public Service<T1, T2> BuildUpGeneric<T1, T2>(Service<T1, T2> buildingInstance)
    where T1: struct
  {
    if (buildingInstance is null) throw new ArgumentNullException(nameof(buildingInstance));
    T1 transientTTS2 = (T1)(object)Guid.NewGuid();
    Service<T1, T2> transientService0;
    Service<T1, T2> localBuildingInstance58 = buildingInstance;
    localBuildingInstance58.Dependency = new Dependency<T2>();
    localBuildingInstance58.SetId(transientTTS2);
    transientService0 = localBuildingInstance58;
    return transientService0;
  }

  [MethodImpl(MethodImplOptions.AggressiveInlining)]
  public T Resolve<T>()
  {
    return Resolver<T>.Value.Resolve(this);
  }

  [MethodImpl(MethodImplOptions.AggressiveInlining)]
  public T Resolve<T>(object? tag)
  {
    return Resolver<T>.Value.ResolveByTag(this, tag);
  }

  [MethodImpl(MethodImplOptions.AggressiveInlining)]
  public object Resolve(Type type)
  {
    throw new InvalidOperationException($"{CannotResolveMessage} {OfTypeMessage} {type}.");
  }

  [MethodImpl(MethodImplOptions.AggressiveInlining)]
  public object Resolve(Type type, object? tag)
  {
    throw new InvalidOperationException($"{CannotResolveMessage} \"{tag}\" {OfTypeMessage} {type}.");
  }

  private const string CannotResolveMessage = "Cannot resolve composition root ";
  private const string OfTypeMessage = "of type ";

  private class Resolver<T>: IResolver<Composition, T>
  {
    public static IResolver<Composition, T> Value = new Resolver<T>();

    public virtual T Resolve(Composition composite)
    {
      throw new InvalidOperationException($"{CannotResolveMessage}{OfTypeMessage}{typeof(T)}.");
    }

    public virtual T ResolveByTag(Composition composite, object tag)
    {
      throw new InvalidOperationException($"{CannotResolveMessage}\"{tag}\" {OfTypeMessage}{typeof(T)}.");
    }
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
	DependencyᐸT2ᐳ --|> IDependencyᐸT2ᐳ
	Composition ..> ServiceᐸT1ˏT2ᐳ : ServiceᐸT1ˏT2ᐳ BuildUpGenericᐸT1ˏT2ᐳ(Pure.DI.UsageTests.Basics.GenericBuilderScenario.Service<T1, T2> buildingInstance)
	ServiceᐸT1ˏT2ᐳ *--  DependencyᐸT2ᐳ : IDependencyᐸT2ᐳ
	ServiceᐸT1ˏT2ᐳ *--  T1 : "Id"  T1
	namespace Pure.DI.UsageTests.Basics.GenericBuilderScenario {
		class Composition {
		<<partial>>
		+ServiceᐸT1ˏT2ᐳ BuildUpGenericᐸT1ˏT2ᐳ(Pure.DI.UsageTests.Basics.GenericBuilderScenario.Service<T1, T2> buildingInstance)
		+ T ResolveᐸTᐳ()
		+ T ResolveᐸTᐳ(object? tag)
		+ object Resolve(Type type)
		+ object Resolve(Type type, object? tag)
		}
		class DependencyᐸT2ᐳ {
			+Dependency()
		}
		class IDependencyᐸT2ᐳ {
			<<interface>>
		}
		class ServiceᐸT1ˏT2ᐳ {
			<<record>>
		}
	}
```

