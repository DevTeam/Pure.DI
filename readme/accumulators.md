#### Accumulators

[![CSharp](https://img.shields.io/badge/C%23-code-blue.svg)](../tests/Pure.DI.UsageTests/Advanced/AccumulatorScenario.cs)

Accumulators allow you to accumulate instances of certain types and lifetimes.


```c#
using Pure.DI;
using static Pure.DI.Lifetime;
using Shouldly;

DI.Setup(nameof(Composition))
    .Accumulate<IAccumulating, MyAccumulator>(Transient, Singleton)
    .Bind<IDependency>().As(PerBlock).To<AbcDependency>()
    .Bind<IDependency>(Tag.Type).To<AbcDependency>()
    .Bind<IDependency>(Tag.Type).As(Singleton).To<XyzDependency>()
    .Bind<IService>().To<Service>()
    .Root<(IService service, MyAccumulator accumulator)>("Root");

var composition = new Composition();
var (service, accumulator) = composition.Root;
accumulator.Count.ShouldBe(3);
accumulator[0].ShouldBeOfType<XyzDependency>();
accumulator[1].ShouldBeOfType<AbcDependency>();
accumulator[2].ShouldBeOfType<Service>();

interface IAccumulating;

class MyAccumulator : List<IAccumulating>;

interface IDependency;

class AbcDependency : IDependency, IAccumulating;

class XyzDependency : IDependency, IAccumulating;

interface IService;

class Service(
    [Tag(typeof(AbcDependency))] IDependency dependency1,
    [Tag(typeof(XyzDependency))] IDependency dependency2,
    IDependency dependency3)
    : IService, IAccumulating;
```


Class diagram:

```mermaid
---
 config:
  class:
   hideEmptyMembersBox: true
---
classDiagram
	Service --|> IService
	AbcDependency --|> IDependency : typeof(Pure.DI.UsageTests.Advanced.AccumulatorScenario.AbcDependency) 
	XyzDependency --|> IDependency : typeof(Pure.DI.UsageTests.Advanced.AccumulatorScenario.XyzDependency) 
	Composition ..> ValueTupleᐸIServiceˏMyAccumulatorᐳ : ValueTupleᐸIServiceˏMyAccumulatorᐳ Root
	ValueTupleᐸIServiceˏMyAccumulatorᐳ *--  Service : IService
	ValueTupleᐸIServiceˏMyAccumulatorᐳ *--  MyAccumulator : MyAccumulator
	Service *--  AbcDependency : typeof(Pure.DI.UsageTests.Advanced.AccumulatorScenario.AbcDependency)  IDependency
	Service o-- "Singleton" XyzDependency : typeof(Pure.DI.UsageTests.Advanced.AccumulatorScenario.XyzDependency)  IDependency
	Service o-- "PerBlock" AbcDependency : IDependency
	namespace Pure.DI.UsageTests.Advanced.AccumulatorScenario {
		class AbcDependency {
			+AbcDependency()
		}
		class Composition {
		<<partial>>
		+ValueTupleᐸIServiceˏMyAccumulatorᐳ Root
		+ T ResolveᐸTᐳ()
		+ T ResolveᐸTᐳ(object? tag)
		+ object Resolve(Type type)
		+ object Resolve(Type type, object? tag)
		}
		class IDependency {
			<<interface>>
		}
		class IService {
			<<interface>>
		}
		class MyAccumulator {
		}
		class Service {
			+Service(IDependency dependency1, IDependency dependency2, IDependency dependency3)
		}
		class XyzDependency {
			+XyzDependency()
		}
	}
	namespace System {
		class ValueTupleᐸIServiceˏMyAccumulatorᐳ {
			<<struct>>
		}
	}
```

