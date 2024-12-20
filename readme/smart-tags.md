#### Smart tags

[![CSharp](https://img.shields.io/badge/C%23-code-blue.svg)](../tests/Pure.DI.UsageTests/Basics/SmartTagsScenario.cs)

When you have a large graph of objects, you may need a lot of tags to neatly define all the dependencies in it. Strings or other constant values are not always convenient to use, because they have too much variability. And there are often cases when you specify one tag in the binding, but the same tag in the dependency, but with a typo, which leads to a compilation error when checking the dependency graph. The solution to this problem is to create an `Enum` type and use its values as tags. _Pure.DI_ makes it easier to solve this problem.

When you specify a tag in a binding and the compiler can't determine what that value is, _Pure.DI_ will automatically create a constant for it inside the `Pure.DI.Tag` type. For the example below, the set of constants would look like this:

```c#
namespace Pure.DI
{
  internal partial class Tag
  {
    public const string Abc = "Abc";
    public const string Xyz = "Xyz";
  }
}
```
So you can apply refactoring in the development environment. And also tag changes in bindings will be automatically checked by the compiler. This will reduce the number of errors.
The example below also uses the `using static Pure.DI.Tag;` directive to access tags in `Pure.DI.Tag` without specifying a type name:


```c#
using static Pure.DI.Tag;
using static Pure.DI.Lifetime;

interface IDependency;

class AbcDependency : IDependency;

class XyzDependency : IDependency;

class Dependency : IDependency;

interface IService
{
    IDependency Dependency1 { get; }

    IDependency Dependency2 { get; }

    IDependency Dependency3 { get; }
}

class Service(
    [Tag(Abc)] IDependency dependency1,
    [Tag(Xyz)] IDependency dependency2,
    IDependency dependency3)
    : IService
{
    public IDependency Dependency1 { get; } = dependency1;

    public IDependency Dependency2 { get; } = dependency2;

    public IDependency Dependency3 { get; } = dependency3;
}

DI.Setup(nameof(Composition))
    // The `default` tag is used to resolve dependencies
    // when the tag was not specified by the consumer
    .Bind<IDependency>(Abc, default).To<AbcDependency>()
    .Bind<IDependency>(Xyz).As(Singleton).To<XyzDependency>()
    .Bind<IService>().To<Service>()

    // "XyzRoot" is root name, Xyz is tag
    .Root<IDependency>("XyzRoot", Xyz)

    // Specifies to create the composition root named "Root"
    .Root<IService>("Root");

var composition = new Composition();
var service = composition.Root;
service.Dependency1.ShouldBeOfType<AbcDependency>();
service.Dependency2.ShouldBeOfType<XyzDependency>();
service.Dependency2.ShouldBe(composition.XyzRoot);
service.Dependency3.ShouldBeOfType<AbcDependency>();
```

The following partial class will be generated:

```c#
partial class Composition
{
  private readonly Composition _root;
  private readonly Lock _lock;

  private XyzDependency? _singletonXyzDependency44;

  [OrdinalAttribute(256)]
  public Composition()
  {
    _root = this;
    _lock = new Lock();
  }

  internal Composition(Composition parentScope)
  {
    _root = (parentScope ?? throw new ArgumentNullException(nameof(parentScope)))._root;
    _lock = _root._lock;
  }

  public IDependency XyzRoot
  {
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    get
    {
      if (_root._singletonXyzDependency44 is null)
      {
        using (_lock.EnterScope())
        {
          if (_root._singletonXyzDependency44 is null)
          {
            _root._singletonXyzDependency44 = new XyzDependency();
          }
        }
      }

      return _root._singletonXyzDependency44!;
    }
  }

  public IService Root
  {
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    get
    {
      if (_root._singletonXyzDependency44 is null)
      {
        using (_lock.EnterScope())
        {
          if (_root._singletonXyzDependency44 is null)
          {
            _root._singletonXyzDependency44 = new XyzDependency();
          }
        }
      }

      return new Service(new AbcDependency(), _root._singletonXyzDependency44!, new AbcDependency());
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
	Service --|> IService
	XyzDependency --|> IDependency : "Xyz" 
	AbcDependency --|> IDependency : "Abc" 
	AbcDependency --|> IDependency
	Composition ..> Service : IService Root
	Composition ..> XyzDependency : IDependency XyzRoot
	Service *--  AbcDependency : "Abc"  IDependency
	Service o-- "Singleton" XyzDependency : "Xyz"  IDependency
	Service *--  AbcDependency : IDependency
	namespace Pure.DI.UsageTests.Basics.SmartTagsScenario {
		class AbcDependency {
			+AbcDependency()
		}
		class Composition {
		<<partial>>
		+IService Root
		+IDependency XyzRoot
		}
		class IDependency {
			<<interface>>
		}
		class IService {
			<<interface>>
		}
		class Service {
			+Service(IDependency dependency1, IDependency dependency2, IDependency dependency3)
		}
		class XyzDependency {
			+XyzDependency()
		}
	}
```

