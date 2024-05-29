#### Decorator

[![CSharp](https://img.shields.io/badge/C%23-code-blue.svg)](../tests/Pure.DI.UsageTests/Interception/DecoratorScenario.cs)

Interception is the ability to intercept calls between objects in order to enrich or change their behavior, but without having to change their code. A prerequisite for interception is weak binding. That is, if programming is abstraction-based, the underlying implementation can be transformed or improved by "packaging" it into other implementations of the same abstraction. At its core, intercept is an application of the Decorator design pattern. This pattern provides a flexible alternative to inheritance by dynamically "attaching" additional responsibility to an object. Decorator "packs" one implementation of an abstraction into another implementation of the same abstraction like a "matryoshka doll".
_Decorator_ is a well-known and useful design pattern. It is convenient to use tagged dependencies to build a chain of nested decorators, as in the example below:


```c#
interface IService { string GetMessage(); }

class Service : IService
{
    public string GetMessage() => "Hello World";
}

class GreetingService([Tag("base")] IService baseService): IService
{
    public string GetMessage() => $"{baseService.GetMessage()} !!!";
}


DI.Setup(nameof(Composition))
    .Bind("base").To<Service>()
    .RootBind<IService>("Root").To<GreetingService>();

var composition = new Composition();
var service = composition.Root;
service.GetMessage().ShouldBe("Hello World !!!");
```

Here an instance of the _Service_ type, labeled _"base"_, is injected in the decorator _DecoratorService_. You can use any tag that semantically reflects the feature of the abstraction being embedded. The tag can be a constant, a type, or a value of an enumerated type.

The following partial class will be generated:

```c#
partial class Composition
{
  private readonly Composition _root;

  public Composition()
  {
    _root = this;
  }

  internal Composition(Composition parentScope)
  {
    _root = (parentScope ?? throw new ArgumentNullException(nameof(parentScope)))._root;
  }

  public IService Root
  {
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    get
    {
      return new GreetingService(new Service());
    }
  }
}
```

Class diagram:

```mermaid
classDiagram
	class Composition {
		<<partial>>
		+IService Root
	}
	Service --|> IService : "base" 
	class Service {
		+Service()
	}
	GreetingService --|> IService
	class GreetingService {
		+GreetingService(IService baseService)
	}
	class IService {
		<<interface>>
	}
	Composition ..> GreetingService : IService Root
	GreetingService *--  Service : "base"  IService
```

