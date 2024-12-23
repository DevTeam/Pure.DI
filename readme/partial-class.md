#### Partial class

[![CSharp](https://img.shields.io/badge/C%23-code-blue.svg)](../tests/Pure.DI.UsageTests/Advanced/PartialClassScenario.cs)

A partial class can contain setup code.


```c#
using Pure.DI;
using static Pure.DI.RootKinds;
using Shouldly;
using System.Diagnostics;

var composition = new Composition("Abc");
var service = composition.Root;

service.Dependency1.Id.ShouldBe(1);
service.Dependency2.Id.ShouldBe(2);
service.Name.ShouldBe("Abc_3");

interface IDependency
{
    long Id { get; }
}

class Dependency(long id) : IDependency
{
    public long Id { get; } = id;
}

class Service(
    [Tag("name with id")] string name,
    IDependency dependency1,
    IDependency dependency2)
{
    public string Name { get; } = name;

    public IDependency Dependency1 { get; } = dependency1;

    public IDependency Dependency2 { get; } = dependency2;
}

// The partial class is also useful for specifying access modifiers to the generated class
public partial class Composition
{
    private readonly string _serviceName = "";
    private long _id;

    // Customizable constructor
    public Composition(string serviceName)
        : this()
    {
        _serviceName = serviceName;
    }

    private long GenerateId() => Interlocked.Increment(ref _id);

    // In fact, this method will not be called at runtime
    [Conditional("DI")]
    void Setup() =>
        DI.Setup()
            .Bind<IDependency>().To<Dependency>()
            .Bind<long>().To(_ => GenerateId())
            .Bind<string>("name with id").To(
                _ => $"{_serviceName}_{GenerateId()}")
            .Root<Service>("Root", kind: Internal);
}
```

The partial class is also useful for specifying access modifiers to the generated class.


Class diagram:

```mermaid
---
 config:
  class:
   hideEmptyMembersBox: true
---
classDiagram
	Dependency --|> IDependency
	Composition ..> Service : Service Root
	Service *--  String : "name with id"  String
	Service *-- "2 " Dependency : IDependency
	Dependency *--  Int64 : Int64
	namespace Pure.DI.UsageTests.Advanced.PartialClassScenario {
		class Composition {
		<<partial>>
		~Service Root
		+ T ResolveᐸTᐳ()
		+ T ResolveᐸTᐳ(object? tag)
		+ object Resolve(Type type)
		+ object Resolve(Type type, object? tag)
		}
		class Dependency {
			+Dependency(Int64 id)
		}
		class IDependency {
			<<interface>>
		}
		class Service {
		}
	}
	namespace System {
		class Int64 {
				<<struct>>
		}
		class String {
		}
	}
```

