#### Composition Root

[![CSharp](https://img.shields.io/badge/C%23-code-blue.svg)](tests\Pure.DI.UsageTests\Basics\CompositionRootScenario.cs)

This example demonstrates the most efficient way to get the root object of a composition without impacting memory consumption or performance.

``` CSharp
internal interface IDependency { }

internal class Dependency : IDependency { }

internal interface IService { }

internal class Service : IService
{
    public Service(IDependency dependency)
    {
    }
}

DI.Setup("Composition")
    .Bind<IDependency>().To<Dependency>()
    .Bind<IService>().To<Service>()
    .Root<IService>("Root");

var composition = new Composition();
var service = composition.Root;
```

Actually, the property _Root_ looks like:
```csharp
public Pure.DI.UsageTests.CompositionRoot.IService Root
{
  get
  {
    return new Service(new Dependency());
  }
}
``` 

#### Resolve methods

[![CSharp](https://img.shields.io/badge/C%23-code-blue.svg)](tests\Pure.DI.UsageTests\Basics\ResolveScenario.cs)

This example shows how to resolve the composition roots using the _Resolve_ methods. 

``` CSharp
internal interface IDependency { }

internal class Dependency : IDependency { }

internal interface IService { }

internal class Service : IService
{
    public Service(IDependency dependency)
    {
    }
}

DI.Setup("Composition")
    .Bind<IDependency>().To<Dependency>()
    .Bind<IService>().To<Service>();

var composition = new Composition();
var service1 = composition.Resolve<IService>();
var service2 = composition.Resolve(typeof(IService));
```

#### Factory

[![CSharp](https://img.shields.io/badge/C%23-code-blue.svg)](tests\Pure.DI.UsageTests\Basics\FactoryScenario.cs)

This example demonstrates how to create and initialize an instance manually. 

``` CSharp
internal interface IDependency
{
    DateTimeOffset Time { get; }

    bool IsInitialized { get; }
}

internal class Dependency : IDependency
{
    public Dependency(DateTimeOffset time)
    {
        Time = time;
    }

    public DateTimeOffset Time { get; }

    public bool IsInitialized { get; private set; }

    public void Initialize()
    {
        IsInitialized = true;
    }
}

internal interface IService
{
    IDependency Dependency { get; }
}

internal class Service : IService
{
    public Service(IDependency dependency)
    {
        Dependency = dependency;
    }

    public IDependency Dependency { get; }
}

DI.Setup("Composition")
    .Bind<IDependency>().To(_ =>
    {
        var dependency = new Dependency(DateTimeOffset.Now);
        dependency.Initialize();
        return dependency;
    })
    .Bind<IService>().To<Service>()
    .Root<IService>("Root");

var composition = new Composition();
var service = composition.Root;
service.Dependency.IsInitialized.ShouldBeTrue();
```

#### Injection

[![CSharp](https://img.shields.io/badge/C%23-code-blue.svg)](tests\Pure.DI.UsageTests\Basics\InjectScenario.cs)

This example shows how to create and initialize an instance manually injecting required dependencies. 

``` CSharp
internal interface IDependency { }

internal class Dependency : IDependency
{
}

internal interface IService
{
    IDependency Dependency { get; }
}

internal class Service : IService
{
    public Service(IDependency dependency)
    {
        Dependency = dependency;
    }

    public IDependency Dependency { get; }
}

DI.Setup("Composition")
    .Bind<IDependency>().To<Dependency>()
    .Bind<IService>().To(ctx =>
    {
        ctx.Inject<IDependency>(out var dependency);
        return new Service(dependency);
    })
    .Root<IService>("Root");

var composition = new Composition();
var service = composition.Root;
```

#### Auto-bindings

[![CSharp](https://img.shields.io/badge/C%23-code-blue.svg)](tests\Pure.DI.UsageTests\Basics\AutoBindingScenario.cs)

This approach works great even if DI doesn't have the appropriate bindings. :warning: But it can't be recommended if you follow the dependency inversion principle and want to make sure your types only depend on abstractions. 

``` CSharp
internal class Dependency
{
}

internal class Service
{
    public Service(Dependency dependency)
    {
    }
}

DI.Setup("Composition")
    .Root<Service>("Root");

var composition = new Composition();
var service1 = composition.Root;
var service2 = composition.Resolve<Service>();
var service3 = composition.Resolve(typeof(Service));
```

#### Arguments

[![CSharp](https://img.shields.io/badge/C%23-code-blue.svg)](tests\Pure.DI.UsageTests\Basics\ArgumentsScenario.cs)

``` CSharp
internal interface IDependency { }

internal class Dependency : IDependency { }

internal interface IService
{
    string Name { get; }
}

internal class Service : IService
{
    public Service(string name, IDependency dependency)
    {
        Name = name;
    }

    public string Name { get; }
}

DI.Setup("Composition")
    .Bind<IDependency>().To<Dependency>()
    .Bind<IService>().To<Service>()
    .Arg<string>("serviceName")
    .Root<IService>("Root");

var composition = new Composition("Abc");
var service = composition.Root;
service.Name.ShouldBe("Abc");
```

#### Tags

[![CSharp](https://img.shields.io/badge/C%23-code-blue.svg)](tests\Pure.DI.UsageTests\Basics\TagsScenario.cs)

This example demonstrates the most efficient way to get the root object of a composition without impacting memory consumption or performance.

``` CSharp
internal interface IDependency { }

internal class AbcDependency : IDependency { }

internal class XyzDependency : IDependency { }

internal class Dependency : IDependency { }

internal interface IService
{
    IDependency Dependency1 { get; }

    IDependency Dependency2 { get; }
}

internal class Service : IService
{
    public Service(
        [Tag("Abc")] IDependency dependency1,
        [Tag("Xyz")] IDependency dependency2)
    {
        Dependency1 = dependency1;
        Dependency2 = dependency2;
    }

    public IDependency Dependency1 { get; }

    public IDependency Dependency2 { get; }
}

DI.Setup("Composition")
    .Bind<IDependency>("Abc").To<AbcDependency>()
    .Bind<IDependency>("Xyz").To<XyzDependency>()
    .Bind<IService>().To<Service>()
    .Root<IService>("Root");

var composition = new Composition();
var service = composition.Root;
service.Dependency1.ShouldBeOfType<AbcDependency>();
service.Dependency2.ShouldBeOfType<XyzDependency>();
```

Sometimes it's important to take control of building a dependency graph. In this case, _tags_ help: 

#### Singleton

[![CSharp](https://img.shields.io/badge/C%23-code-blue.svg)](tests\Pure.DI.UsageTests\Lifetimes\SingletonScenario.cs)

``` CSharp
internal interface IDependency { }

internal class Dependency : IDependency { }

internal interface IService
{
    public IDependency Dependency1 { get; }

    public IDependency Dependency2 { get; }
}

internal class Service : IService
{
    public Service(IDependency dependency1, IDependency dependency2)
    {
        Dependency1 = dependency1;
        Dependency2 = dependency2;
    }

    public IDependency Dependency1 { get; }

    public IDependency Dependency2 { get; }
}

DI.Setup("Composition")
    .Bind<IDependency>().As(Lifetime.Singleton).To<Dependency>()
    .Bind<IService>().To<Service>()
    .Root<IService>("Root");

var composition = new Composition();
var service1 = composition.Root;
var service2 = composition.Root;
service1.Dependency1.ShouldBe(service1.Dependency2);
service2.Dependency1.ShouldBe(service1.Dependency1);
```

#### PerResolve

[![CSharp](https://img.shields.io/badge/C%23-code-blue.svg)](tests\Pure.DI.UsageTests\Lifetimes\PerResolveScenario.cs)

``` CSharp
internal interface IDependency { }

internal class Dependency : IDependency { }

internal interface IService
{
    public IDependency Dependency1 { get; }

    public IDependency Dependency2 { get; }
}

internal class Service : IService
{
    public Service(IDependency dependency1, IDependency dependency2)
    {
        Dependency1 = dependency1;
        Dependency2 = dependency2;
    }

    public IDependency Dependency1 { get; }

    public IDependency Dependency2 { get; }
}

DI.Setup("Composition")
    .Bind<IDependency>().As(Lifetime.PerResolve).To<Dependency>()
    .Bind<IService>().To<Service>()
    .Root<IService>("Root");

var composition = new Composition();
var service1 = composition.Root;
var service2 = composition.Root;
service1.Dependency1.ShouldBe(service1.Dependency2);
service2.Dependency1.ShouldNotBe(service1.Dependency1);
```

#### Transient

[![CSharp](https://img.shields.io/badge/C%23-code-blue.svg)](tests\Pure.DI.UsageTests\Lifetimes\TransientScenario.cs)

``` CSharp
internal interface IDependency { }

internal class Dependency : IDependency { }

internal interface IService
{
    public IDependency Dependency1 { get; }

    public IDependency Dependency2 { get; }
}

internal class Service : IService
{
    public Service(IDependency dependency1, IDependency dependency2)
    {
        Dependency1 = dependency1;
        Dependency2 = dependency2;
    }

    public IDependency Dependency1 { get; }

    public IDependency Dependency2 { get; }
}

DI.Setup("Composition")
    .Bind<IDependency>().As(Lifetime.Transient).To<Dependency>()
    .Bind<IService>().To<Service>()
    .Root<IService>("Root");

var composition = new Composition();
var service1 = composition.Root;
var service2 = composition.Root;
service1.Dependency1.ShouldNotBe(service1.Dependency2);
service2.Dependency1.ShouldNotBe(service1.Dependency1);
```

#### Disposable Singleton

[![CSharp](https://img.shields.io/badge/C%23-code-blue.svg)](tests\Pure.DI.UsageTests\Lifetimes\DisposableSingletonScenario.cs)

To dispose all created singleton instances, simply dispose the composition instance:

``` CSharp
internal interface IDependency
{
    bool IsDisposed { get; }
}

internal class Dependency : IDependency, IDisposable
{
    public bool IsDisposed { get; private set; }

    public void Dispose()
    {
        IsDisposed = true;
    }
}

internal interface IService
{
    public IDependency Dependency { get; }
}

internal class Service : IService
{
    public Service(IDependency dependency)
    {
        Dependency = dependency;
    }

    public IDependency Dependency { get; }
}

DI.Setup("Composition")
    .Bind<IDependency>().As(Lifetime.Singleton).To<Dependency>()
    .Bind<IService>().To<Service>()
    .Root<IService>("Root");

IDependency dependency;
using (var composition = new Composition())
{
    var service = composition.Root;
    dependency = service.Dependency;
}

dependency.IsDisposed.ShouldBeTrue();
```

#### Default lifetime

[![CSharp](https://img.shields.io/badge/C%23-code-blue.svg)](tests\Pure.DI.UsageTests\Lifetimes\DefaultLifetimeScenario.cs)

``` CSharp
internal interface IDependency { }

internal class Dependency : IDependency { }

internal interface IService
{
    public IDependency Dependency1 { get; }

    public IDependency Dependency2 { get; }
}

internal class Service : IService
{
    public Service(IDependency dependency1, IDependency dependency2)
    {
        Dependency1 = dependency1;
        Dependency2 = dependency2;
    }

    public IDependency Dependency1 { get; }

    public IDependency Dependency2 { get; }
}

DI.Setup("Composition")
    .DefaultLifetime(Lifetime.Singleton)
    .Bind<IDependency>().To<Dependency>()
    .Bind<IService>().To<Service>()
    .Root<IService>("Root");

var composition = new Composition();
var service1 = composition.Root;
var service2 = composition.Root;
service1.ShouldBe(service2);
service1.Dependency1.ShouldBe(service1.Dependency2);
```

