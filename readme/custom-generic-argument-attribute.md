#### Custom generic argument attribute

[![CSharp](https://img.shields.io/badge/C%23-code-blue.svg)](../tests/Pure.DI.UsageTests/Attributes/CustomGenericArgumentAttributeScenario.cs)


```c#
using Pure.DI;
using Shouldly;

DI.Setup(nameof(Composition))
    // Registers custom generic argument
    .GenericTypeArgumentAttribute<MyGenericTypeArgumentAttribute>()
    .Bind<IDependency<TTMy>>().To<Dependency<TTMy>>()
    .Bind<IService>().To<Service>()

    // Composition root
    .Root<IService>("Root");

var composition = new Composition();
var service = composition.Root;
service.IntDependency.ShouldBeOfType<Dependency<int>>();
service.StringDependency.ShouldBeOfType<Dependency<string>>();

[AttributeUsage(AttributeTargets.Interface | AttributeTargets.Class | AttributeTargets.Struct)]
class MyGenericTypeArgumentAttribute : Attribute;

[MyGenericTypeArgument]
interface TTMy;

interface IDependency<T>;

class Dependency<T> : IDependency<T>;

interface IService
{
    IDependency<int> IntDependency { get; }

    IDependency<string> StringDependency { get; }
}

class Service(
    IDependency<int> intDependency,
    IDependency<string> stringDependency)
    : IService
{
    public IDependency<int> IntDependency { get; } = intDependency;

    public IDependency<string> StringDependency { get; } = stringDependency;
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
	DependencyᐸInt32ᐳ --|> IDependencyᐸInt32ᐳ
	DependencyᐸStringᐳ --|> IDependencyᐸStringᐳ
	Composition ..> Service : IService Root
	Service *--  DependencyᐸInt32ᐳ : IDependencyᐸInt32ᐳ
	Service *--  DependencyᐸStringᐳ : IDependencyᐸStringᐳ
	namespace Pure.DI.UsageTests.Attributes.CustomGenericArgumentAttributeScenario {
		class Composition {
		<<partial>>
		+IService Root
		+ T ResolveᐸTᐳ()
		+ T ResolveᐸTᐳ(object? tag)
		+ object Resolve(Type type)
		+ object Resolve(Type type, object? tag)
		}
		class DependencyᐸInt32ᐳ {
			+Dependency()
		}
		class DependencyᐸStringᐳ {
			+Dependency()
		}
		class IDependencyᐸInt32ᐳ {
			<<interface>>
		}
		class IDependencyᐸStringᐳ {
			<<interface>>
		}
		class IService {
			<<interface>>
		}
		class Service {
			+Service(IDependencyᐸInt32ᐳ intDependency, IDependencyᐸStringᐳ stringDependency)
		}
	}
```

