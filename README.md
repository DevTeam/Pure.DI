# Pure DI for .NET

[![License](https://img.shields.io/badge/License-MIT-green.svg)](LICENSE) [<img src="http://tcavs2015.cloudapp.net/app/rest/builds/buildType:(id:DevTeam_PureDI_Build)/statusIcon"/>](http://tcavs2015.cloudapp.net/viewType.html?buildTypeId=DevTeam_PureDI_Build&guest=1)

<img src="https://github.com/DevTeam/Pure.DI/blob/master/Docs/Images/demo.gif"/>

#### Base concepts:

- DI without any IoC/DI containers, frameworks, dependencies, and thus without any performance impacts and side-effects
- A predictable and validated dependencies graph which is building and validating on the fly while you are writing your code
- Does not add dependencies to any other assemblies
- High performance with all .NET compiler/JIT optimizations
- Easy to use
- Ultra-fine tuning of generic types
- Supports major .NET BCL types from the box

## [Schrödinger's cat](Samples/ShroedingersCat) shows how it works

### The reality is that

![Cat](https://github.com/DevTeam/IoCContainer/blob/master/Docs/Images/cat.jpg?raw=true)

### Let's create an abstraction

```csharp
interface IBox<out T> { T Content { get; } }

interface ICat { State State { get; } }

enum State { Alive, Dead }
```

### Here is our implementation

```csharp
class CardboardBox<T> : IBox<T>
{
    public CardboardBox(T content) => Content = content;

    public T Content { get; }
}

class ShroedingersCat : ICat
{
  // Represents the superposition of the states
  private readonly Lazy<State> _superposition;

  public ShroedingersCat(Lazy<State> superposition) => _superposition = superposition;

  // Decoherence of the superposition at the time of observation via an irreversible process
  public State State => _superposition.Value;

  public override string ToString() => $"{State} cat";
}
```

_It is important to note that our abstraction and our implementation do not know anything about any IoC/DI containers at all._

### Let's glue all together

Just add a package reference to [Pure.DI](https://www.nuget.org/packages/Pure.DI). Using NuGet packages allows you to optimize your application to include only the necessary dependencies.

- Package Manager

  ```
  Install-Package Pure.DI
  ```
  
- .NET CLI
  
  ```
  dotnet add package Pure.DI
  ```

Declare required dependencies in a class like:

```csharp
static partial class Glue
{
  // Models a random subatomic event that may or may not occur
  private static readonly Random Indeterminacy = new();

  static Glue()
  {
    DI.Setup()
      // Represents a quantum superposition of 2 states: Alive or Dead
      .Bind<State>().To(_ => (State)Indeterminacy.Next(2))
      // Represents schrodinger's cat
      .Bind<ICat>().To<ShroedingersCat>()
      // Represents a cardboard box with any content
      .Bind<IBox<TT>>().To<CardboardBox<TT>>()
      // Composition Root
      .Bind<Program>().As(Singleton).To<Program>();
  }
}
```

_Defining generic type arguments using special marker types like *__TT__* in the sample above is one of the distinguishing features of this library. So there is an easy way to bind complex generic types with nested generic types and with any type constraints._

### Time to open boxes!

```csharp
class Program
{
  // Composition Root, a single place in an application where the composition of the object graphs for an application take place
  public static void Main() => Glue.Resolve<Program>().Run();

  private readonly IBox<ICat> _box;

  internal Program(IBox<ICat> box) => _box = box;

  private void Run() => Console.WriteLine(_box);
}
```

_Program_ is a [*__Composition Root__*](https://blog.ploeh.dk/2011/07/28/CompositionRoot/) here, a single place in an application where the composition of the object graphs for an application take place. To have an ability create multiple instances or to do it on demand you could use *__Func<>__* with required type specified. Each instance is resolved by a strongly-typed block of statements like the operator new which are compiled with all optimizations with minimal impact on performance or memory consumption. For instance, the creating of a composition root *__Program__* looks like this:
```csharp
Random Indeterminacy = new();

new Program(
  new CardboardBox<ICat>(
    new ShroedingersCat(
      new Lazy<State>(
        new Func<State>(
          () => (State)Indeterminacy.Next(2)))));
```

Take full advantage of Dependency Injection everywhere and every time without any compromise.

## NuGet package

[![NuGet](https://buildstats.info/nuget/Pure.DI)](https://www.nuget.org/packages/Pure.DI)

## Simple and powerful API

```csharp
// Starts a configuration chain
// This method contains a single optional argument to specify generating type name
// By default it is a name of a class owner
DI.Setup("MyComposer")
  
  // You could select a base configuration
  .DependsOn(nameof(BasicComposer))
  
  // You could specify your own attribute to override an injection type
  .TypeAttribure<MyTypeAttribute>()
  
  // Or your own attribute to override an injection tag
  .TagAttribure<MyTagAttribute>()
  
  // Or your own attribute to override an injection order
  .OrderAttribure<MyOrderAttribute>()
  
  // This is basic binding format
  .Bind<IMyInterface>().To<MyImplementation>()
   
  // You could specify a lifetime for a binding
  .Bind<IMyInterface>().As(Lifetime.Singleton).To<MyImplementation>()
  
  // You could specify few tags for each binding
  .Bind<IMyInterface>().Tag("MyImpl").Tag(123).To<MyImplementation>()

  // Or a binding relating to any tag
  .Bind<IMyInterface>().AnyTag().To<MyImplementation>()

  // This advanced binding format 
  // which allows to create instance manually and inject all required dependenciesinvoke methods, initialize properties and etc
  .Bind<IMyInterface>().To(ctx => new MyImplementation(ctx.Resolve<ISomeDependency1>(), "Some value", ctx.Resolve<ISomeDependency2>()))
```

The list of life times:
- Transient - creates a new object of the requested type every time, it is default
- Singleton - creates a singleton object first time you and then returns the same object
- PerThread - creates a singleton object per thread. It returns different objects on different threads
- PerResolve - similar to the Transient, but it reuses the same object in the recursive object graph 
- Binding - this lifetime allows to apply a custom lifetime to a binding, just realize the interface ILifetime<T> and bind, for example `.Bind<ILifetime<IMyInterface>>().To<MyLifetime>()`
- ContainerSingleton - this lifetime is applicable for ASP.NET, specifies that a single instance of the service will be created
- Scoped - this lifetime is applicable for ASP.NET, specifies that a new instance of the service will be created for each scope

## Development environment requirements

- [.NET 5.0.102+](https://dotnet.microsoft.com/download/dotnet/5.0)
- [Visual Studio 16.8+](https://visualstudio.microsoft.com/vs)
- [C# 4.0+](https://docs.microsoft.com/en-us/dotnet/csharp/whats-new/csharp-version-history#c-version-40)

## Supported frameworks

- .NET 5.0+
- [.NET Core](https://docs.microsoft.com/en-us/dotnet/core/) 1.0+
- [.NET Standard](https://docs.microsoft.com/en-us/dotnet/standard/net-standard) 1.0+
- .NET Framework 3.5+
- [UWP](https://docs.microsoft.com/en-us/windows/uwp/index) 10+

## ASP.NET Support

When a targeting project is an ASP.NET project, a special extension method is generated automatically. This extension method could be used to integrate DI into a web application infrastructure. Pay attention to [this single statement](https://github.com/DevTeam/Pure.DI/blob/d1c4cdf3d6d7015f809cf7f9153d091a1d42dc34/Samples/BlazorServerApp/Startup.cs#L24)  that makes all magic. For more details, please take a look at this [sample](https://github.com/DevTeam/Pure.DI/tree/master/Samples/BlazorServerApp).

## WPF Support

[This sample](https://github.com/DevTeam/Pure.DI/tree/master/Samples/WpfAppNetCore) demonstrates how to apply DI for a WPF application. The crucial class is [DataProvider](Samples/WpfAppNetCore/DataProvider.cs), which connects view and view models. Besides that, it provides two sets of models for [design-time](Samples/WpfAppNetCore/ClockDomainDesignTime.cs) and [running](Samples/WpfAppNetCore/ClockDomain.cs) modes.

## Usage Scenarios

- Basics
  - [Autowiring](#autowiring-)
  - [Bindings](#bindings-)
  - [Constants](#constants-)
  - [Generics](#generics-)
  - [Tags](#tags-)
  - [Aspect-oriented DI](#aspect-oriented-di-)
  - [Several contracts](#several-contracts-)
  - [Autowiring with initialization](#autowiring-with-initialization-)
  - [Dependency tag](#dependency-tag-)
  - [Generic autowiring](#generic-autowiring-)
  - [Injection of default parameters](#injection-of-default-parameters-)
- Lifetimes
  - [Singleton lifetime](#singleton-lifetime-)
  - [Per thread singleton](#per-thread-singleton-)
- BCL types
  - [Arrays](#arrays-)
  - [Collections](#collections-)
  - [Enumerables](#enumerables-)
  - [Func](#func-)
  - [Lazy](#lazy-)
  - [Sets](#sets-)
  - [ThreadLocal](#threadlocal-)
  - [Tuples](#tuples-)
- Advanced
  - [Constructor choice](#constructor-choice-)

### Autowiring [![CSharp](https://img.shields.io/badge/C%23-code-blue.svg)](https://raw.githubusercontent.com/DevTeam/IoCContainer/master/IoC.Tests/UsageScenarios/Autowiring.cs)

Autowring is the most natural way to use containers. In the first step, we should create a container. At the second step, we bind interfaces to their implementations. After that, the container is ready to resolve dependencies.

``` CSharp
// Create the container and configure it, using full autowiring
DI.Setup()
    .Bind<IDependency>().To<Dependency>()
    .Bind<IService>().To<Service>();

// Resolve an instance of interface `IService`
var instance = AutowiringDI.Resolve<IService>();
```



### Bindings [![CSharp](https://img.shields.io/badge/C%23-code-blue.svg)](https://raw.githubusercontent.com/DevTeam/IoCContainer/master/IoC.Tests/UsageScenarios/Bindings.cs)

It is possible to bind any number of types.

``` CSharp
DI.Setup()
    .Bind<IDependency>().To<Dependency>()
    // Bind using few types
    .Bind<IService>().Bind<IAnotherService>().Tag("abc").To<Service>();

// Resolve instances using different types
var instance1 = BindingsDI.Resolve<IService>("abc");
var instance2 = BindingsDI.Resolve<IAnotherService>("abc");
```



### Constants [![CSharp](https://img.shields.io/badge/C%23-code-blue.svg)](https://raw.githubusercontent.com/DevTeam/IoCContainer/master/IoC.Tests/UsageScenarios/Constants.cs)

It's obvious here.

``` CSharp
DI.Setup()
    .Bind<int>().To(_ => 10);

// Resolve an integer
var val = ConstantsDI.Resolve<int>();
// Check the value
val.ShouldBe(10);
```



### Generics [![CSharp](https://img.shields.io/badge/C%23-code-blue.svg)](https://raw.githubusercontent.com/DevTeam/IoCContainer/master/IoC.Tests/UsageScenarios/Generics.cs)

Auto-wring of generic types via binding of open generic types or generic type markers are working the same way.

``` CSharp
DI.Setup()
    .Bind<IDependency>().To<Dependency>()
    // Bind open generic interface to open generic implementation
    .Bind<IService<TT>>().To<Service<TT>>()
    .Bind<CompositionRoot<IService<int>>>().To<CompositionRoot<IService<int>>>();

// Resolve a generic instance
var instance = GenericsDI.Resolve<CompositionRoot<IService<int>>>().Root;
```



### Tags [![CSharp](https://img.shields.io/badge/C%23-code-blue.svg)](https://raw.githubusercontent.com/DevTeam/IoCContainer/master/IoC.Tests/UsageScenarios/Tags.cs)

Tags are useful while binding to several implementations of the same abstract types.

``` CSharp
DI.Setup()
    .Bind<IDependency>().To<Dependency>()
    // Bind using several tags
    .Bind<IService>().Tag(10).Tag("abc").To<Service>()
    .Bind<IService>().To<Service>();

// Resolve instances using tags
var instance1 = TagsDI.Resolve<IService>("abc");
var instance2 = TagsDI.Resolve<IService>(10);

// Resolve the instance using the empty tag
var instance3 = TagsDI.Resolve<IService>();
```



### Aspect-oriented DI [![CSharp](https://img.shields.io/badge/C%23-code-blue.svg)](https://raw.githubusercontent.com/DevTeam/IoCContainer/master/IoC.Tests/UsageScenarios/AspectOriented.cs)

This framework has no special predefined attributes to support aspect-oriented auto wiring because a non-infrastructure code should not have references to this framework. But this code may contain these attributes by itself. And it is quite easy to use these attributes for aspect-oriented auto wiring, see the sample below.

``` CSharp
public void Run()
{
    DI.Setup()
        // Define attributes for aspect-oriented autowiring
        .TypeAttribute<TypeAttribute>()
        .OrderAttribute<OrderAttribute>()
        .TagAttribute<TagAttribute>()
        // Configure the container to use DI aspects
        .Bind<IConsole>().Tag("MyConsole").To(_ => AspectOriented.Console.Object)
        .Bind<string>().Tag("Prefix").To(_ => "info")
        .Bind<ILogger>().To<Logger>();

    // Create a logger
    var logger = AspectOrientedDI.Resolve<ILogger>();

    // Log the message
    logger.Log("Hello");

    // Check the output has the appropriate format
    Console.Verify(i => i.WriteLine(It.IsRegex(".+ - info: Hello")));
}

// Represents the dependency aspect attribute to specify a type for injection.
[AttributeUsage(AttributeTargets.Parameter | AttributeTargets.Method)]
public class TypeAttribute : Attribute
{
    public TypeAttribute(Type type) { }
}

// Represents the dependency aspect attribute to specify a tag for injection.
[AttributeUsage(AttributeTargets.Parameter | AttributeTargets.Method | AttributeTargets.Property)]
public class TagAttribute : Attribute
{
    public TagAttribute(object tag) { }
}

// Represents the dependency aspect attribute to specify an order for injection.
[AttributeUsage(AttributeTargets.Method | AttributeTargets.Property)]
public class OrderAttribute : Attribute
{
    public OrderAttribute(int order) { }
}

public interface IConsole { void WriteLine(string text); }

public interface IClock { DateTimeOffset Now { get; } }

public interface ILogger { void Log(string message); }

public class Logger : ILogger
{
    private readonly IConsole _console;
    private IClock? _clock;

    // Constructor injection using the tag "MyConsole"
    public Logger([Tag("MyConsole")] IConsole console) => _console = console;

    // Method injection after constructor using specified type _Clock_
    [Order(1)] public void Initialize([Type(typeof(Clock))] IClock clock) => _clock = clock;

    // Setter injection after the method injection above using the tag "Prefix"
    [Tag("Prefix"), Order(2)]
    public string Prefix { get; set; } = string.Empty;

    // Adds current time and prefix before a message and writes it to console
    public void Log(string message) => _console?.WriteLine($"{_clock?.Now} - {Prefix}: {message}");
}

public class Clock : IClock
{
    // "clockName" dependency is not resolved here but has default value
    public Clock([Type(typeof(string)), Tag("ClockName")] string clockName = "SPb") { }

    public DateTimeOffset Now => DateTimeOffset.Now;
}
```

You can also specify your own aspect-oriented autowiring by implementing the interface [_IAutowiringStrategy_](IoCContainer/blob/master/IoC/IAutowiringStrategy.cs).

### Several contracts [![CSharp](https://img.shields.io/badge/C%23-code-blue.svg)](https://raw.githubusercontent.com/DevTeam/IoCContainer/master/IoC.Tests/UsageScenarios/SeveralContracts.cs)

It is possible to bind several types to a single implementation.

``` CSharp
DI.Setup()
    .Bind<IDependency>().To<Dependency>()
    .Bind<Service>().Bind<IService>().Bind<IAnotherService>().To<Service>();

// Resolve instances
var instance1 = SeveralContractsDI.Resolve<IService>();
var instance2 = SeveralContractsDI.Resolve<IAnotherService>();
```



### Autowiring with initialization [![CSharp](https://img.shields.io/badge/C%23-code-blue.svg)](https://raw.githubusercontent.com/DevTeam/IoCContainer/master/IoC.Tests/UsageScenarios/AutowiringWithInitialization.cs)

Sometimes instances required some actions before you give them to use - some methods of initialization or fields which should be defined. You can solve these things easily.

``` CSharp
// Create a container and configure it using full autowiring
DI.Setup()
    .Bind<IDependency>().To<Dependency>()
    .Bind<INamedService>().To(
        ctx =>
        {
            var service = new InitializingNamedService(ctx.Resolve<IDependency>());
            // Configure the container to invoke method "Initialize" for every created instance of this type
            service.Initialize("Initialized!", ctx.Resolve<IDependency>());
            return service;
        });

// Resolve an instance of interface `IService`
var instance = AutowiringWithInitializationDI.Resolve<INamedService>();

// Check the instance
instance.ShouldBeOfType<InitializingNamedService>();

// Check that the initialization has took place
instance.Name.ShouldBe("Initialized!");
```

:warning: It is not recommended because it is a cause of hidden dependencies.

### Dependency tag [![CSharp](https://img.shields.io/badge/C%23-code-blue.svg)](https://raw.githubusercontent.com/DevTeam/IoCContainer/master/IoC.Tests/UsageScenarios/DependencyTag.cs)

Use a _tag_ to bind few dependencies for the same types.

``` CSharp
DI.Setup()
    .Bind<IDependency>().Tag("MyDep").To<Dependency>()
    // Configure autowiring and inject dependency tagged by "MyDep"
    .Bind<IService>().To(ctx => new Service(ctx.Resolve<IDependency>("MyDep")));

// Resolve an instance
var instance = DependencyTagDI.Resolve<IService>();
```



### Injection of default parameters [![CSharp](https://img.shields.io/badge/C%23-code-blue.svg)](https://raw.githubusercontent.com/DevTeam/IoCContainer/master/IoC.Tests/UsageScenarios/DefaultParamsInjection.cs)



``` CSharp
public void Run()
{
    DI.Setup()
        .Bind<IDependency>().To<Dependency>()
        .Bind<IService>().To<SomeService>();

    // Resolve an instance
    var instance = DefaultParamsInjectionDI.Resolve<IService>();

    // Check the optional dependency
    instance.State.ShouldBe("empty");
}

public class SomeService: IService
{
    // "state" dependency is not resolved here but it has the default value "empty"
    public SomeService(IDependency dependency, string state = "empty")
    {
        Dependency = dependency;
        State = state;
    }

    public IDependency Dependency { get; }

    public string State { get; }
}
```



### Generic autowiring [![CSharp](https://img.shields.io/badge/C%23-code-blue.svg)](https://raw.githubusercontent.com/DevTeam/IoCContainer/master/IoC.Tests/UsageScenarios/GenericAutowiring.cs)

Autowiring of generic types as simple as autowiring of other simple types. Just use a generic parameters markers like _TT_, _TT1_, _TT2_ and etc. or TTI, TTI1, TTI2 ... for interfaces or TTS, TTS1, TTS2 ... for value types or other special markers like TTDisposable, TTDisposable1 and etc. TTList<>, TTDictionary<> ... or create your own generic parameters markers or bind open generic types.

``` CSharp
public void Run()
{
    // Create and configure the container using autowiring
    DI.Setup()
        .Bind<IDependency>().To<Dependency>()
        // Bind using the predefined generic parameters marker TT (or TT1, TT2, TT3 ...)
        .Bind<IService<TT>>().To<Service<TT>>()
        // Bind using the predefined generic parameters marker TTList (or TTList1, TTList2 ...)
        // For other cases there are TTComparable, TTComparable<in T>, TTEquatable<T>, TTEnumerable<out T>, TTDictionary<TKey, TValue> and etc.
        .Bind<IListService<TTList<int>>>().To<ListService<TTList<int>>>()
        // Bind using the custom generic parameters marker TCustom
        .Bind<IService<TTMy>>().Tag("custom marker").To<Service<TTMy>>()
        .Bind<CompositionRoot<IListService<IList<int>>>>().To<CompositionRoot<IListService<IList<int>>>>()
        .Bind<CompositionRoot<ICollection<IService<int>>>>().To<CompositionRoot<ICollection<IService<int>>>>();

    // Resolve a generic instance
    var listService = GenericAutowiringDI.Resolve<CompositionRoot<IListService<IList<int>>>>().Root;
    var instances = GenericAutowiringDI.Resolve<CompositionRoot<ICollection<IService<int>>>>().Root;

    instances.Count.ShouldBe(2);
    // Check the instance's type
    foreach (var instance in instances)
    {
        instance.ShouldBeOfType<Service<int>>();
    }

    listService.ShouldBeOfType<ListService<IList<int>>>();
}

// Custom generic type marker using predefined attribute `GenericTypeArgument`
[GenericTypeArgument]
class TTMy { }
```



### Singleton lifetime [![CSharp](https://img.shields.io/badge/C%23-code-blue.svg)](https://raw.githubusercontent.com/DevTeam/IoCContainer/master/IoC.Tests/UsageScenarios/SingletonLifetime.cs)

[Singleton](https://en.wikipedia.org/wiki/Singleton_pattern) is a design pattern that supposes for having only one instance of some class during the whole application lifetime. The main complaint about Singleton is that it contradicts the Dependency Injection principle and thus hinders testability. It essentially acts as a global constant, and it is hard to substitute it with a test when needed. The _Singleton lifetime_ is indispensable in this case.

``` CSharp
DI.Setup()
    .Bind<IDependency>().To<Dependency>()
    // Use the Singleton lifetime
    .Bind<IService>().As(Singleton).To<Service>();

// Resolve the singleton twice
var instance1 = SingletonLifetimeDI.Resolve<IService>();
var instance2 = SingletonLifetimeDI.Resolve<IService>();

// Check that instances are equal
instance1.ShouldBe(instance2);
```



### Per thread singleton [![CSharp](https://img.shields.io/badge/C%23-code-blue.svg)](https://raw.githubusercontent.com/DevTeam/IoCContainer/master/IoC.Tests/UsageScenarios/ThreadSingletonLifetime.cs)



``` CSharp
public void Run()
{
    DI.Setup()
        .Bind<IDependency>().To<Dependency>()
        // Bind an interface to an implementation using the per thread singleton lifetime
        .Bind<IService>().As(Lifetime.PerThread).To<Service>();

    // Resolve the singleton twice
    var instance1 = ThreadSingletonLifetimeDI.Resolve<IService>();
    var instance2 = ThreadSingletonLifetimeDI.Resolve<IService>();
    IService? instance3 = null;
    IService? instance4 = null;

    var finish = new ManualResetEvent(false);
    var newThread = new Thread(() =>
    {
        instance3 = ThreadSingletonLifetimeDI.Resolve<IService>();
        instance4 = ThreadSingletonLifetimeDI.Resolve<IService>();
        finish.Set();
    });

    newThread.Start();
    finish.WaitOne();

    // Check that instances resolved in a main thread are equal
    instance1.ShouldBe(instance2);

    // Check that instance resolved in a new thread is not null
    instance3!.ShouldNotBeNull();

    // Check that instances resolved in different threads are not equal
    instance1.ShouldNotBe(instance3!);

    // Check that instances resolved in a new thread are equal
    instance4!.ShouldBe(instance3!);
}
```



### Arrays [![CSharp](https://img.shields.io/badge/C%23-code-blue.svg)](https://raw.githubusercontent.com/DevTeam/IoCContainer/master/IoC.Tests/UsageScenarios/Arrays.cs)

To resolve all possible instances of any tags of the specific type as an _array_ just use the injection _T[]_

``` CSharp
DI.Setup()
    .Bind<IDependency>().To<Dependency>()
    // Bind to the implementation #1
    .Bind<IService>().Tag(1).To<Service>()
    // Bind to the implementation #2
    .Bind<IService>().Tag(2).Tag("abc").To<Service>()
    // Bind to the implementation #3
    .Bind<IService>().Tag(3).To<Service>()
    .Bind<CompositionRoot<CompositionRoot<IService[]>>>().To<CompositionRoot<CompositionRoot<IService[]>>>();

// Resolve all appropriate instances
var composition = ArraysDI.Resolve<CompositionRoot<IService[]>>();
```



### Collections [![CSharp](https://img.shields.io/badge/C%23-code-blue.svg)](https://raw.githubusercontent.com/DevTeam/IoCContainer/master/IoC.Tests/UsageScenarios/Collections.cs)

To resolve all possible instances of any tags of the specific type as a _collection_ just use the injection _ICollection<T>_

``` CSharp
DI.Setup()
    .Bind<IDependency>().To<Dependency>()
    // Bind to the implementation #1
    .Bind<IService>().Tag(1).To<Service>()
    // Bind to the implementation #2
    .Bind<IService>().Tag(2).Tag("abc").To<Service>()
    // Bind to the implementation #3
    .Bind<IService>().Tag(3).To<Service>()
    .Bind<CompositionRoot<ICollection<IService>>>().To<CompositionRoot<ICollection<IService>>>();

// Resolve all appropriate instances
var composition = CollectionsDI.Resolve<CompositionRoot<ICollection<IService>>>();

// Check the number of resolved instances
composition.Root.Count.ShouldBe(3);
```



### Enumerables [![CSharp](https://img.shields.io/badge/C%23-code-blue.svg)](https://raw.githubusercontent.com/DevTeam/IoCContainer/master/IoC.Tests/UsageScenarios/Enumerables.cs)

To resolve all possible instances of any tags of the specific type as an _enumerable_ just use the injection _IEnumerable<T>_.

``` CSharp
DI.Setup()
    .Bind<IDependency>().To<Dependency>()
    // Bind to the implementation #1
    .Bind<IService>().Tag(1).To<Service>()
    // Bind to the implementation #2
    .Bind<IService>().Tag(2).Tag("abc").To<Service>()
    // Bind to the implementation #3
    .Bind<IService>().Tag(3).To<Service>()
    .Bind<CompositionRoot<IEnumerable<IService>>>().To<CompositionRoot<IEnumerable<IService>>>();

// Resolve all appropriate instances
var instances = EnumerablesDI.Resolve<CompositionRoot<IEnumerable<IService>>>().Root.ToList();

// Check the number of resolved instances
instances.Count.ShouldBe(3);
```



### Func [![CSharp](https://img.shields.io/badge/C%23-code-blue.svg)](https://raw.githubusercontent.com/DevTeam/IoCContainer/master/IoC.Tests/UsageScenarios/Func.cs)

_Func<>_ helps when a logic needs to inject some type of instances on-demand or solve circular dependency issues.

``` CSharp
DI.Setup()
    .Bind<IDependency>().To<Dependency>()
    .Bind<IService>().To<Service>()
    .Bind<CompositionRoot<Func<IService>>>().To<CompositionRoot<Func<IService>>>();

// Resolve function to create instances
var factory = FuncDI.Resolve<CompositionRoot<Func<IService>>>().Root;

// Resolve few instances
var instance1 = factory();
var instance2 = factory();
```



### Lazy [![CSharp](https://img.shields.io/badge/C%23-code-blue.svg)](https://raw.githubusercontent.com/DevTeam/IoCContainer/master/IoC.Tests/UsageScenarios/Lazy.cs)

_Lazy_ dependency helps when a logic needs to inject _Lazy<T>_ to get instance once on demand.

``` CSharp
DI.Setup()
    .Bind<IDependency>().To<Dependency>()
    .Bind<IService>().To<Service>()
    .Bind<CompositionRoot<Lazy<IService>>>().To<CompositionRoot<Lazy<IService>>>();

// Resolve the instance of Lazy<IService>
var lazy = LazyDI.Resolve<CompositionRoot<Lazy<IService>>>().Root;

// Get the instance via Lazy
var instance = lazy.Value;
```



### Sets [![CSharp](https://img.shields.io/badge/C%23-code-blue.svg)](https://raw.githubusercontent.com/DevTeam/IoCContainer/master/IoC.Tests/UsageScenarios/Sets.cs)

To resolve all possible instances of any tags of the specific type as a _ISet<>_ just use the injection _ISet<T>_.

``` CSharp
DI.Setup()
    .Bind<IDependency>().To<Dependency>()
    // Bind to the implementation #1
    .Bind<IService>().Tag(1).To<Service>()
    // Bind to the implementation #2
    .Bind<IService>().Tag(2).Tag("abc").To<Service>()
    // Bind to the implementation #3
    .Bind<IService>().Tag(3).To<Service>()
    .Bind<CompositionRoot<ISet<IService>>>().To<CompositionRoot<ISet<IService>>>();

// Resolve all appropriate instances
var instances = SetsDI.Resolve<CompositionRoot<ISet<IService>>>().Root;

// Check the number of resolved instances
instances.Count.ShouldBe(3);
```



### ThreadLocal [![CSharp](https://img.shields.io/badge/C%23-code-blue.svg)](https://raw.githubusercontent.com/DevTeam/IoCContainer/master/IoC.Tests/UsageScenarios/ThreadLocal.cs)



``` CSharp
DI.Setup()
    .Bind<IDependency>().To<Dependency>()
    .Bind<IService>().To<Service>()
    .Bind<CompositionRoot<ThreadLocal<IService>>>().To<CompositionRoot<ThreadLocal<IService>>>();

// Resolve the instance of ThreadLocal<IService>
var threadLocal = ThreadLocalDI.Resolve<CompositionRoot<ThreadLocal<IService>>>().Root;

// Get the instance via ThreadLocal
var instance = threadLocal.Value;
```



### Tuples [![CSharp](https://img.shields.io/badge/C%23-code-blue.svg)](https://raw.githubusercontent.com/DevTeam/IoCContainer/master/IoC.Tests/UsageScenarios/Tuples.cs)

[Tuple](https://docs.microsoft.com/en-us/dotnet/api/system.tuple) has a set of elements that should be resolved at the same time.

``` CSharp
DI.Setup()
    .Bind<IDependency>().To<Dependency>()
    .Bind<IService>().To<Service>()
    .Bind<INamedService>().To(ctx => new NamedService(ctx.Resolve<IDependency>(), "some name"))
    .Bind<CompositionRoot<Tuple<IService, INamedService>>>().To<CompositionRoot<Tuple<IService, INamedService>>>();

// Resolve an instance of type Tuple<IService, INamedService>
var (service, namedService) = TuplesDI.Resolve<CompositionRoot<Tuple<IService, INamedService>>>().Root;
```



### Constructor choice [![CSharp](https://img.shields.io/badge/C%23-code-blue.svg)](https://raw.githubusercontent.com/DevTeam/IoCContainer/master/IoC.Tests/UsageScenarios/ConstructorChoice.cs)

We can specify a constructor manually and all its arguments.

``` CSharp
DI.Setup()
    .Bind<IDependency>().To<Dependency>()
    .Bind<IService>().To(
        // Select the constructor and inject required dependencies
        ctx => new Service(ctx.Resolve<IDependency>(), "some state"));

var instance = ConstructorChoiceDI.Resolve<IService>();

// Check the injected constant
instance.State.ShouldBe("some state");
```



