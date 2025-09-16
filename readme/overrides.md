#### Overrides

This example demonstrates advanced dependency injection techniques using Pure.DI's override mechanism to customize dependency instantiation with runtime arguments and tagged parameters. The implementation creates multiple `IDependency` instances with values manipulated through explicit overrides.


```c#
using Shouldly;
using Pure.DI;
using System.Collections.Immutable;
using System.Drawing;

DI.Setup(nameof(Composition))
    .Bind(Tag.Red).To(_ => Color.Red)
    .Bind().As(Lifetime.Singleton).To<Clock>()
    .Bind().To<Func<int, int, IDependency>>(ctx =>
        (dependencyId, subId) =>
        {
            // Overrides with a lambda argument
            ctx.Override(dependencyId);

            // Overrides with tag using lambda argument
            ctx.Override(subId, "sub");

            // Overrides with some value
            ctx.Override($"Dep {dependencyId} {subId}");

            // Overrides with injected value
            ctx.Inject(Tag.Red, out Color red);
            ctx.Override(red);

            ctx.Inject<Dependency>(out var dependency);
            return dependency;
        })
    .Bind().To<Service>()

    // Composition root
    .Root<IService>("Root");

var composition = new Composition();
var service = composition.Root;
service.Dependencies.Length.ShouldBe(3);

service.Dependencies[0].Id.ShouldBe(0);
service.Dependencies[0].SubId.ShouldBe(99);
service.Dependencies[0].Name.ShouldBe("Dep 0 99");

service.Dependencies[1].Id.ShouldBe(1);
service.Dependencies[1].Name.ShouldBe("Dep 1 99");

service.Dependencies[2].Id.ShouldBe(2);
service.Dependencies[2].Name.ShouldBe("Dep 2 99");

interface IClock
{
    DateTimeOffset Now { get; }
}

class Clock : IClock
{
    public DateTimeOffset Now => DateTimeOffset.Now;
}

interface IDependency
{
    string Name { get; }

    int Id { get; }

    int SubId { get; }
}

class Dependency(
    string name,
    IClock clock,
    int id,
    [Tag("sub")] int subId,
    Color red)
    : IDependency
{
    public string Name => name;

    public int Id => id;

    public int SubId => subId;
}

interface IService
{
    ImmutableArray<IDependency> Dependencies { get; }
}

class Service(Func<int, int, IDependency> dependencyFactory): IService
{
    public ImmutableArray<IDependency> Dependencies { get; } =
    [
        dependencyFactory(0, 99),
        dependencyFactory(1, 99),
        dependencyFactory(2, 99)
    ];
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
#if NET9_0_OR_GREATER
  private readonly Lock _lock;
#else
  private readonly Object _lock;
#endif

  private Clock? _singleClock53;

  [OrdinalAttribute(256)]
  public Composition()
  {
    _root = this;
#if NET9_0_OR_GREATER
    _lock = new Lock();
#else
    _lock = new Object();
#endif
  }

  internal Composition(Composition parentScope)
  {
    _root = (parentScope ?? throw new ArgumentNullException(nameof(parentScope)))._root;
    _lock = parentScope._lock;
  }

  public IService Root
  {
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    get
    {
      Func<int, int, IDependency> transFunc1 =
      [MethodImpl(MethodImplOptions.AggressiveInlining)]
      (localDependencyId1, localSubId1) =>
      {
        // Overrides with a lambda argument
        // Overrides with tag using lambda argument
        // Overrides with some value
        // Overrides with injected value
        int overrInt32 = localDependencyId1;
        int overrInt321 = localSubId1;
        string overrString2 = $"Dep {localDependencyId1} {localSubId1}";
        Drawing.Color transColor2 = Color.Red;
        Drawing.Color localRed1 = transColor2;
        Drawing.Color overrColor3 = localRed1;
        if (_root._singleClock53 is null)
          lock (_lock)
            if (_root._singleClock53 is null)
            {
              _root._singleClock53 = new Clock();
            }

        Dependency localDependency4 = new Dependency(overrString2, _root._singleClock53, overrInt32, overrInt321, overrColor3);
        return localDependency4;
      };
      return new Service(transFunc1);
    }
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
	Service --|> IService
	Composition ..> Service : IService Root
	FuncᐸInt32ˏInt32ˏIDependencyᐳ *--  Color : "Red"  Color
	FuncᐸInt32ˏInt32ˏIDependencyᐳ *--  Dependency : Dependency
	Service *--  FuncᐸInt32ˏInt32ˏIDependencyᐳ : FuncᐸInt32ˏInt32ˏIDependencyᐳ
	Dependency o-- "Singleton" Clock : IClock
	Dependency *--  Int32 : Int32
	Dependency *--  Int32 : "sub"  Int32
	Dependency *--  String : String
	Dependency *--  Color : Color
	namespace Pure.DI.UsageTests.Basics.OverridesScenario {
		class Clock {
			<<class>>
		}
		class Composition {
		<<partial>>
		+IService Root
		}
		class Dependency {
				<<class>>
			+Dependency(String name, IClock clock, Int32 id, Int32 subId, Color red)
		}
		class IService {
			<<interface>>
		}
		class Service {
				<<class>>
			+Service(FuncᐸInt32ˏInt32ˏIDependencyᐳ dependencyFactory)
		}
	}
	namespace System {
		class FuncᐸInt32ˏInt32ˏIDependencyᐳ {
				<<delegate>>
		}
		class Int32 {
			<<struct>>
		}
		class String {
			<<class>>
		}
	}
	namespace System.Drawing {
		class Color {
			<<struct>>
		}
	}
```

