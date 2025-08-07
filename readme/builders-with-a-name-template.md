#### Builders with a name template

Sometimes you need to build up an existing composition root and inject all of its dependencies, in which case the `Builder` method will be useful, as in the example below:


```c#
using Shouldly;
using Pure.DI;

DI.Setup(nameof(Composition))
    .Bind().To(_ => Guid.NewGuid())
    .Bind().To<Dependency>()
    // Creates a builder based on the name template
    // for each type inherited from IService.
    // These types must be available at this point in the code.
    .Builders<IService>("BuildUp{type}");

var composition = new Composition();
        
var service1 = composition.BuildUpService1(new Service1());
service1.Id.ShouldNotBe(Guid.Empty);
service1.Dependency.ShouldBeOfType<Dependency>();

var service2 = composition.BuildUpService2(new Service2());
service2.Id.ShouldBe(Guid.Empty);
service2.Dependency.ShouldBeOfType<Dependency>();

// Uses a common method to build an instance
IService abstractService = new Service1();
abstractService = composition.BuildUpIService(abstractService);
abstractService.ShouldBeOfType<Service1>();
abstractService.Id.ShouldNotBe(Guid.Empty);
abstractService.Dependency.ShouldBeOfType<Dependency>();

interface IDependency;

class Dependency : IDependency;

interface IService
{
    Guid Id { get; }

    IDependency? Dependency { get; }
}

record Service1: IService
{
    public Guid Id { get; private set; } = Guid.Empty;

    // The Dependency attribute specifies to perform an injection
    [Dependency]
    public IDependency? Dependency { get; set; }

    [Dependency]
    public void SetId(Guid id) => Id = id;
}

record Service2 : IService
{
    public Guid Id => Guid.Empty;

    // The Dependency attribute specifies to perform an injection
    [Dependency]
    public IDependency? Dependency { get; set; }
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

The default builder method name is `BuildUp`. The first argument to this method will always be the instance to be built.

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
  public Service1 BuildUpService1(Service1 buildingInstance)
  {
    if (buildingInstance is null) throw new ArgumentNullException(nameof(buildingInstance));
    Service1 transService15;
    Service1 localBuildingInstance102 = buildingInstance;
    Guid transGuid8 = Guid.NewGuid();
    localBuildingInstance102.Dependency = new Dependency();
    localBuildingInstance102.SetId(transGuid8);
    transService15 = localBuildingInstance102;
    return transService15;
  }

  [MethodImpl(MethodImplOptions.AggressiveInlining)]
  public Service2 BuildUpService2(Service2 buildingInstance)
  {
    if (buildingInstance is null) throw new ArgumentNullException(nameof(buildingInstance));
    Service2 transService22;
    Service2 localBuildingInstance101 = buildingInstance;
    localBuildingInstance101.Dependency = new Dependency();
    transService22 = localBuildingInstance101;
    return transService22;
  }

  #pragma warning disable CS0162
  [MethodImpl(MethodImplOptions.AggressiveInlining)]
  public IService BuildUpIService(IService buildingInstance)
  {
    if (buildingInstance is null) throw new ArgumentNullException(nameof(buildingInstance));
    IService transIService;
    IService localBuildingInstance98 = buildingInstance;
    switch (localBuildingInstance98)
    {
      case Service1 localService1199:
      {
        transIService = BuildUpService1(localService1199);
        goto transIServiceFinish;
      }

      case Service2 localService21100:
      {
        transIService = BuildUpService2(localService21100);
        goto transIServiceFinish;
      }

      default:
        throw new ArgumentException($"Unable to build an instance of typeof type {localBuildingInstance98.GetType()}.", "buildingInstance");
    }

    transIService = localBuildingInstance98;
    transIServiceFinish:
      ;
    return transIService;
  }
  #pragma warning restore CS0162
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
	Dependency --|> IDependency
	Composition ..> IService : IService BuildUpIService(Pure.DI.UsageTests.Basics.BuildersWithNameTemplateScenario.IService buildingInstance)
	Composition ..> Service2 : Service2 BuildUpService2(Pure.DI.UsageTests.Basics.BuildersWithNameTemplateScenario.Service2 buildingInstance)
	Composition ..> Service1 : Service1 BuildUpService1(Pure.DI.UsageTests.Basics.BuildersWithNameTemplateScenario.Service1 buildingInstance)
	Service1 *--  Guid : Guid
	Service1 *--  Dependency : IDependency
	Service2 *--  Dependency : IDependency
	namespace Pure.DI.UsageTests.Basics.BuildersWithNameTemplateScenario {
		class Composition {
		<<partial>>
		+IService BuildUpIService(Pure.DI.UsageTests.Basics.BuildersWithNameTemplateScenario.IService buildingInstance)
		+Service1 BuildUpService1(Pure.DI.UsageTests.Basics.BuildersWithNameTemplateScenario.Service1 buildingInstance)
		+Service2 BuildUpService2(Pure.DI.UsageTests.Basics.BuildersWithNameTemplateScenario.Service2 buildingInstance)
		}
		class Dependency {
				<<class>>
			+Dependency()
		}
		class IDependency {
			<<interface>>
		}
		class IService {
				<<interface>>
		}
		class Service1 {
				<<record>>
			+IDependency Dependency
			+SetId(Guid id) : Void
		}
		class Service2 {
				<<record>>
			+IDependency Dependency
		}
	}
	namespace System {
		class Guid {
				<<struct>>
		}
	}
```

