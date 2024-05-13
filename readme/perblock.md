#### PerBlock

[![CSharp](https://img.shields.io/badge/C%23-code-blue.svg)](../tests/Pure.DI.UsageTests/Lifetimes/PerBlockScenario.cs)

The _PreBlock_ lifetime does not guarantee that there will be a single instance of the dependency for each root of the composition, but is useful to reduce the number of instances of type.


```c#
interface IDependency;

class Dependency : IDependency;

class Service(
    IDependency dep1,
    IDependency dep2,
    Lazy<(IDependency dep3, IDependency dep4)> deps)
{
    public IDependency Dep1 { get; } = dep1;

    public IDependency Dep2 { get; } = dep2;

    public IDependency Dep3 { get; } = deps.Value.dep3;

    public IDependency Dep4 { get; } = deps.Value.dep4;
}

DI.Setup(nameof(Composition))
    // This hint indicates to not generate methods such as Resolve
    .Hint(Hint.Resolve, "Off")
    .Bind().As(Lifetime.PerBlock).To<Dependency>()

    // Composition root
    .Root<Service>("Root");

var composition = new Composition();

var service1 = composition.Root;
service1.Dep1.ShouldBe(service1.Dep2);
service1.Dep3.ShouldBe(service1.Dep4);
service1.Dep1.ShouldNotBe(service1.Dep3);
        
var service2 = composition.Root;
service2.Dep1.ShouldNotBe(service1.Dep1);
```

The following partial class will be generated:

```c#
partial class Composition
{
  private readonly Composition _root;
  private readonly object _lock;

  public Composition()
  {
    _root = this;
    _lock = new object();
  }

  internal Composition(Composition parentScope)
  {
    _root = (parentScope ?? throw new ArgumentNullException(nameof(parentScope)))._root;
    _lock = _root._lock;
  }

  public Service Root
  {
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    get
    {
      var perResolve43_Func = default(Func<(IDependency dep3, IDependency dep4)>);
      perResolve43_Func = new Func<(IDependency dep3, IDependency dep4)>(
      [MethodImpl(MethodImplOptions.AggressiveInlining)]
      () =>
      {
          Dependency perBlock4_Dependency = new Dependency();
          var value_1 = (perBlock4_Dependency, perBlock4_Dependency);
          return value_1;
      });
      Lazy<(IDependency dep3, IDependency dep4)> transient2_Lazy;
      {
          var factory_2 = perResolve43_Func!;
          transient2_Lazy = new Lazy<(IDependency dep3, IDependency dep4)>(factory_2, true);
      }
      Dependency perBlock1_Dependency = new Dependency();
      return new Service(perBlock1_Dependency, perBlock1_Dependency, transient2_Lazy);
    }
  }
}
```

Class diagram:

```mermaid
classDiagram
	class Composition {
		<<partial>>
		+Service Root
	}
	class ValueTupleᐸIDependencyˏIDependencyᐳ {
		+ValueTuple(IDependency item1, IDependency item2)
	}
	class Service {
		+Service(IDependency dep1, IDependency dep2, LazyᐸValueTupleᐸIDependencyˏIDependencyᐳᐳ deps)
	}
	Dependency --|> IDependency : 
	class Dependency {
		+Dependency()
	}
	class LazyᐸValueTupleᐸIDependencyˏIDependencyᐳᐳ
	class FuncᐸValueTupleᐸIDependencyˏIDependencyᐳᐳ
	class IDependency {
		<<interface>>
	}
	ValueTupleᐸIDependencyˏIDependencyᐳ o-- "2 PerBlock" Dependency : IDependency
	Service o-- "2 PerBlock" Dependency : IDependency
	Service *--  LazyᐸValueTupleᐸIDependencyˏIDependencyᐳᐳ : LazyᐸValueTupleᐸIDependencyˏIDependencyᐳᐳ
	Composition ..> Service : Service Root
	LazyᐸValueTupleᐸIDependencyˏIDependencyᐳᐳ o-- "PerResolve" FuncᐸValueTupleᐸIDependencyˏIDependencyᐳᐳ : FuncᐸValueTupleᐸIDependencyˏIDependencyᐳᐳ
	FuncᐸValueTupleᐸIDependencyˏIDependencyᐳᐳ *--  ValueTupleᐸIDependencyˏIDependencyᐳ : ValueTupleᐸIDependencyˏIDependencyᐳ
```

