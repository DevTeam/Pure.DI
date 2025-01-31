#### Generic builders

[![CSharp](https://img.shields.io/badge/C%23-code-blue.svg)](../tests/Pure.DI.UsageTests/Generics/GenericBuildersScenario.cs)


```c#
using Shouldly;
using Pure.DI;

DI.Setup(nameof(Composition))
    .Bind(Tag.Id).To<TT>(_ => (TT)(object)Guid.NewGuid())
    .Bind().To<Dependency<TT>>()
    // Generic service builder
    .Builders<IService<TT, TT2>>("BuildUpGeneric");

var composition = new Composition();

var service1 = composition.BuildUpGeneric(new Service1<Guid, string>());
service1.Id.ShouldNotBe(Guid.Empty);
service1.Dependency.ShouldBeOfType<Dependency<string>>();

var service2 = composition.BuildUpGeneric(new Service2<Guid, int>());
service2.Id.ShouldBe(Guid.Empty);
service2.Dependency.ShouldBeOfType<Dependency<int>>();

interface IDependency<T>;

class Dependency<T> : IDependency<T>;

interface IService<out T, T2>
{
    T Id { get; }

    IDependency<T2>? Dependency { get; }
}

record Service1<T, T2>: IService<T, T2>
    where T: struct
{
    public T Id { get; private set; }

    [Dependency]
    public IDependency<T2>? Dependency { get; set; }

    [Dependency]
    public void SetId([Tag(Tag.Id)] T id) => Id = id;
}

record Service2<T, T2>: IService<T, T2>
    where T: struct
{
    public T Id { get; }

    [Dependency]
    public IDependency<T2>? Dependency { get; set; }
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
  public Service2<T3, T4> BuildUpGeneric<T3, T4>(Service2<T3, T4> buildingInstance)
    where T3: struct
  {
    if (buildingInstance is null) throw new ArgumentNullException(nameof(buildingInstance));
    Service2<T3, T4> transientService20;
    Service2<T3, T4> localBuildingInstance59 = buildingInstance;
    localBuildingInstance59.Dependency = new Dependency<T4>();
    transientService20 = localBuildingInstance59;
    return transientService20;
  }

  [MethodImpl(MethodImplOptions.AggressiveInlining)]
  public Service1<T3, T4> BuildUpGeneric<T3, T4>(Service1<T3, T4> buildingInstance)
    where T3: struct
  {
    if (buildingInstance is null) throw new ArgumentNullException(nameof(buildingInstance));
    T3 transientTT2 = (T3)(object)Guid.NewGuid();
    Service1<T3, T4> transientService10;
    Service1<T3, T4> localBuildingInstance60 = buildingInstance;
    localBuildingInstance60.Dependency = new Dependency<T4>();
    localBuildingInstance60.SetId(transientTT2);
    transientService10 = localBuildingInstance60;
    return transientService10;
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
	DependencyᐸT4ᐳ --|> IDependencyᐸT4ᐳ
	Composition ..> Service2ᐸT3ˏT4ᐳ : Service2ᐸT3ˏT4ᐳ BuildUpGenericᐸT3ˏT4ᐳ(Pure.DI.UsageTests.Basics.GenericBuildersScenario.Service2<T3, T4> buildingInstance)
	Composition ..> Service1ᐸT3ˏT4ᐳ : Service1ᐸT3ˏT4ᐳ BuildUpGenericᐸT3ˏT4ᐳ(Pure.DI.UsageTests.Basics.GenericBuildersScenario.Service1<T3, T4> buildingInstance)
	Service2ᐸT3ˏT4ᐳ *--  DependencyᐸT4ᐳ : IDependencyᐸT4ᐳ
	Service1ᐸT3ˏT4ᐳ *--  DependencyᐸT4ᐳ : IDependencyᐸT4ᐳ
	Service1ᐸT3ˏT4ᐳ *--  T3 : "Id"  T3
	namespace Pure.DI.UsageTests.Basics.GenericBuildersScenario {
		class Composition {
		<<partial>>
		+Service2ᐸT3ˏT4ᐳ BuildUpGenericᐸT3ˏT4ᐳ(Pure.DI.UsageTests.Basics.GenericBuildersScenario.Service2<T3, T4> buildingInstance)
		+Service1ᐸT3ˏT4ᐳ BuildUpGenericᐸT3ˏT4ᐳ(Pure.DI.UsageTests.Basics.GenericBuildersScenario.Service1<T3, T4> buildingInstance)
		+ T ResolveᐸTᐳ()
		+ T ResolveᐸTᐳ(object? tag)
		+ object Resolve(Type type)
		+ object Resolve(Type type, object? tag)
		}
		class DependencyᐸT4ᐳ {
			+Dependency()
		}
		class IDependencyᐸT4ᐳ {
			<<interface>>
		}
		class Service1ᐸT3ˏT4ᐳ {
			<<record>>
		}
		class Service2ᐸT3ˏT4ᐳ {
			<<record>>
		}
	}
```

