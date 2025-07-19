#### Generic builders


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
#if NET9_0_OR_GREATER
  private readonly Lock _lock;
#else
  private readonly Object _lock;
#endif

  [OrdinalAttribute(256)]
  public Composition()
  {
#if NET9_0_OR_GREATER
    _lock = new Lock();
#else
    _lock = new Object();
#endif
  }

  internal Composition(Composition parentScope)
  {
  }

  [MethodImpl(MethodImplOptions.AggressiveInlining)]
  public Service2<T1, T5> BuildUpGeneric<T1, T5>(Service2<T1, T5> buildingInstance)
    where T1: struct
  {
    if (buildingInstance is null) throw new ArgumentNullException(nameof(buildingInstance));
    Service2<T1, T5> transService20;
    Service2<T1, T5> localBuildingInstance143 = buildingInstance;
    localBuildingInstance143.Dependency = new Dependency<T5>();
    transService20 = localBuildingInstance143;
    return transService20;
  }

  [MethodImpl(MethodImplOptions.AggressiveInlining)]
  public Service1<T1, T5> BuildUpGeneric<T1, T5>(Service1<T1, T5> buildingInstance)
    where T1: struct
  {
    if (buildingInstance is null) throw new ArgumentNullException(nameof(buildingInstance));
    Service1<T1, T5> transService13;
    Service1<T1, T5> localBuildingInstance144 = buildingInstance;
    T1 transTT6 = (T1)(object)Guid.NewGuid();
    localBuildingInstance144.Dependency = new Dependency<T5>();
    localBuildingInstance144.SetId(transTT6);
    transService13 = localBuildingInstance144;
    return transService13;
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
	DependencyᐸT5ᐳ --|> IDependencyᐸT5ᐳ
	Composition ..> Service2ᐸT1ˏT5ᐳ : Service2ᐸT1ˏT5ᐳ BuildUpGenericᐸT1ˏT5ᐳ(Pure.DI.UsageTests.Generics.GenericBuildersScenario.Service2<T1, T5> buildingInstance)
	Composition ..> Service1ᐸT1ˏT5ᐳ : Service1ᐸT1ˏT5ᐳ BuildUpGenericᐸT1ˏT5ᐳ(Pure.DI.UsageTests.Generics.GenericBuildersScenario.Service1<T1, T5> buildingInstance)
	Service2ᐸT1ˏT5ᐳ *--  DependencyᐸT5ᐳ : IDependencyᐸT5ᐳ
	Service1ᐸT1ˏT5ᐳ *--  DependencyᐸT5ᐳ : IDependencyᐸT5ᐳ
	Service1ᐸT1ˏT5ᐳ *--  T1 : "Id"  T1
	namespace Pure.DI.UsageTests.Generics.GenericBuildersScenario {
		class Composition {
		<<partial>>
		+Service2ᐸT1ˏT5ᐳ BuildUpGenericᐸT1ˏT5ᐳ(Pure.DI.UsageTests.Generics.GenericBuildersScenario.Service2<T1, T5> buildingInstance)
		+Service1ᐸT1ˏT5ᐳ BuildUpGenericᐸT1ˏT5ᐳ(Pure.DI.UsageTests.Generics.GenericBuildersScenario.Service1<T1, T5> buildingInstance)
		}
		class DependencyᐸT5ᐳ {
				<<class>>
			+Dependency()
		}
		class IDependencyᐸT5ᐳ {
			<<interface>>
		}
		class Service1ᐸT1ˏT5ᐳ {
				<<record>>
			+IDependencyᐸT5ᐳ Dependency
			+SetId(T1 id) : Void
		}
		class Service2ᐸT1ˏT5ᐳ {
				<<record>>
			+IDependencyᐸT5ᐳ Dependency
		}
	}
```

