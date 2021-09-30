
## Usage Scenarios

- Basics
  - [Autowiring](#autowiring)
  - [Constants](#constants)
  - [Generics](#generics)
  - [Manual binding](#manual-binding)
  - [Tags](#tags)
  - [Aspect-oriented DI](#aspect-oriented-di)
  - [Several contracts](#several-contracts)
  - [Aspect-oriented DI with custom attributes](#aspect-oriented-di-with-custom-attributes)
  - [Autowiring with initialization](#autowiring-with-initialization)
  - [Dependency tag](#dependency-tag)
  - [Injection of default parameters](#injection-of-default-parameters)
  - [Advanced generic autowiring](#advanced-generic-autowiring)
  - [Depends On](#depends-on)
  - [Dependency Injection with referencing implementations](#dependency-injection-with-referencing-implementations)
- Lifetimes
  - [Default lifetime](#default-lifetime)
  - [Per resolve lifetime](#per-resolve-lifetime)
  - [Singleton lifetime](#singleton-lifetime)
  - [Transient lifetime](#transient-lifetime)
  - [Custom singleton lifetime](#custom-singleton-lifetime)
- BCL types
  - [Arrays](#arrays)
  - [Collections](#collections)
  - [Enumerables](#enumerables)
  - [Func](#func)
  - [Lazy](#lazy)
  - [Sets](#sets)
  - [Thread Local](#thread-local)
  - [Tuples](#tuples)
- Interception
  - [Decorator](#decorator)
  - [Intercept specific types](#intercept-specific-types)
  - [Intercept a set of types](#intercept-a-set-of-types)
- Advanced
  - [ASPNET](#aspnet)

### Autowiring



``` CSharp
DI.Setup()
    .Bind<IDependency>().To<Dependency>()
    .Bind<IService>().To<Service>();

// Resolve an instance of interface `IService`
var instance = AutowiringDI.Resolve<IService>();
```

This sample references types from [this file](Pure.DI.UsageScenarios.Tests/Models.cs).

### Constants

It's obvious here.

``` CSharp
DI.Setup()
    .Bind<int>().To(_ => 10);

// Resolve an integer
var val = ConstantsDI.Resolve<int>();
// Check the value
val.ShouldBe(10);
```



### Generics

Auto-wring of generic types via binding of open generic types or generic type markers are working the same way.

``` CSharp
public class Consumer
{
    public Consumer(IService<int> service) { }
}

DI.Setup()
    .Bind<IDependency>().To<Dependency>()
    // Bind open generic interface to open generic implementation
    .Bind<IService<TT>>().To<Service<TT>>()
    .Bind<Consumer>().To<Consumer>();

var instance = GenericsDI.Resolve<Consumer>();
```

Open generic type instance, for instance, like IService&lt;TT&gt; here, cannot be a composition root instance. This sample references types from [this file](Pure.DI.UsageScenarios.Tests/Models.cs).

### Manual binding

We can specify a constructor manually with all its arguments and even call some methods before an instance will be returned to consumers.

``` CSharp
DI.Setup()
    .Bind<IDependency>().To<Dependency>()
    .Bind<IService>().To(
        // Select the constructor and inject required dependencies manually
        ctx => new Service(ctx.Resolve<IDependency>(), "some state"));

var instance = ManualBindingDI.Resolve<IService>();

// Check the injected constant
instance.State.ShouldBe("some state");
```

This sample references types from [this file](Pure.DI.UsageScenarios.Tests/Models.cs).

### Tags

Tags are useful while binding to several implementations of the same abstract types.

``` CSharp
DI.Setup()
    .Bind<IDependency>().To<Dependency>()
    // Bind using several tags
    .Bind<IService>().Tags(10, "abc").To<Service>();

// Resolve instances using tags
var instance1 = TagsDI.Resolve<IService>("abc");
var instance2 = TagsDI.Resolve<IService>(10);
```

This sample references types from [this file](Pure.DI.UsageScenarios.Tests/Models.cs).

### Aspect-oriented DI



``` CSharp
public void Run()
{
    DI.Setup()
        .Bind<IConsole>().Tags("MyConsole").To(_ => AspectOriented.Console.Object)
        .Bind<string>().Tags("Prefix").To(_ => "info")
        .Bind<ILogger>().To<Logger>();

    // Create a logger
    var logger = AspectOrientedDI.Resolve<ILogger>();

    // Log the message
    logger.Log("Hello");

    // Check the output has the appropriate format
    Console.Verify(i => i.WriteLine(It.IsRegex(".+ - info: Hello")));
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
    public void Log(string message) => _console.WriteLine($"{_clock?.Now} - {Prefix}: {message}");
}

public class Clock : IClock
{
    // "clockName" dependency is not resolved here but has default value
    public Clock([Type(typeof(string)), Tag("ClockName")] string clockName = "SPb") { }

    public DateTimeOffset Now => DateTimeOffset.Now;
}
```



### Several contracts

It is possible to bind several types to a single implementation.

``` CSharp
DI.Setup()
    .Bind<IDependency>().To<Dependency>()
    .Bind<Service>().Bind<IService>().Bind<IAnotherService>().To<Service>();

// Resolve instances
var instance1 = SeveralContractsDI.Resolve<IService>();
var instance2 = SeveralContractsDI.Resolve<IAnotherService>();
```

This sample references types from [this file](Pure.DI.UsageScenarios.Tests/Models.cs).

### Aspect-oriented DI with custom attributes

There is already a set of predefined attributes to support aspect-oriented autowiring such as _TypeAttribute_. But in addition, you can use your own attributes, see the sample below.

``` CSharp
public void Run()
{
    DI.Setup()
        // Define custom attributes for aspect-oriented autowiring
        .TypeAttribute<MyTypeAttribute>()
        .OrderAttribute<MyOrderAttribute>()
        .TagAttribute<MyTagAttribute>()

        .Bind<IConsole>().Tags("MyConsole").To(_ => AspectOrientedWithCustomAttributes.Console.Object)
        .Bind<string>().Tags("Prefix").To(_ => "info")
        .Bind<ILogger>().To<Logger>();

    // Create a logger
    var logger = AspectOrientedWithCustomAttributesDI.Resolve<ILogger>();

    // Log the message
    logger.Log("Hello");

    // Check the output has the appropriate format
    Console.Verify(i => i.WriteLine(It.IsRegex(".+ - info: Hello")));
}

// Represents the dependency aspect attribute to specify a type for injection.
[AttributeUsage(
    AttributeTargets.Parameter
    | AttributeTargets.Property
    | AttributeTargets.Field)]
public class MyTypeAttribute : Attribute
{
    // A type, which will be used during an injection
    public readonly Type Type;

    public MyTypeAttribute(Type type) => Type = type;
}

// Represents the dependency aspect attribute to specify a tag for injection.
[AttributeUsage(
    AttributeTargets.Parameter
    | AttributeTargets.Property
    | AttributeTargets.Field)]
public class MyTagAttribute : Attribute
{
    // A tag, which will be used during an injection
    public readonly object Tag;

    public MyTagAttribute(object tag) => Tag = tag;
}

// Represents the dependency aspect attribute to specify an order for injection.
[AttributeUsage(
    AttributeTargets.Constructor
    | AttributeTargets.Method
    | AttributeTargets.Property
    | AttributeTargets.Field)]
public class MyOrderAttribute : Attribute
{
    // An order to be used to invoke a method
    public readonly int Order;

    public MyOrderAttribute(int order) => Order = order;
}

public interface IConsole { void WriteLine(string text); }

public interface IClock { DateTimeOffset Now { get; } }

public interface ILogger { void Log(string message); }

public class Logger : ILogger
{
    private readonly IConsole _console;
    private IClock? _clock;

    // Constructor injection using the tag "MyConsole"
    public Logger([MyTag("MyConsole")] IConsole console) => _console = console;

    // Method injection after constructor using specified type _Clock_
    [MyOrder(1)] public void Initialize([MyType(typeof(Clock))] IClock clock) => _clock = clock;

    // Setter injection after the method injection above using the tag "Prefix"
    [MyTag("Prefix"), MyOrder(2)]
    public string Prefix { get; set; } = string.Empty;

    // Adds current time and prefix before a message and writes it to console
    public void Log(string message) => _console.WriteLine($"{_clock?.Now} - {Prefix}: {message}");
}

public class Clock : IClock
{
    // "clockName" dependency is not resolved here but has default value
    public Clock([MyType(typeof(string)), MyTag("ClockName")] string clockName = "SPb") { }

    public DateTimeOffset Now => DateTimeOffset.Now;
}
```



### Autowiring with initialization

Sometimes instances required some actions before you give them to use - some methods of initialization or fields which should be defined. You can solve these things easily. :warning: But this approach is not recommended because it is a cause of hidden dependencies.

``` CSharp
DI.Setup()
    .Bind<IDependency>().To<Dependency>()
    .Bind<INamedService>().To(
        ctx =>
        {
            var service = new InitializingNamedService(ctx.Resolve<IDependency>());
            // Invokes method "Initialize" for every created instance of this type
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

This sample references types from [this file](Pure.DI.UsageScenarios.Tests/Models.cs).

### Dependency tag

Use a _tag_ to bind several dependencies for the same types.

``` CSharp
DI.Setup()
    .Bind<IDependency>().Tags("MyDep").To<Dependency>()
    // Configure autowiring and inject dependency tagged by "MyDep"
    .Bind<IService>().To(ctx => new Service(ctx.Resolve<IDependency>("MyDep")));

// Resolve an instance
var instance = DependencyTagDI.Resolve<IService>();
```

This sample references types from [this file](Pure.DI.UsageScenarios.Tests/Models.cs).

### Injection of default parameters



``` CSharp
public void Run()
{
    DI.Setup()
        .Bind<IDependency>().To<Dependency>()
        .Bind<IService>().To<SomeService>();

    // Resolve an instance
    var instance = DefaultParamsInjectionDI.Resolve<IService>();

    // Check the optional dependency
    instance.State.ShouldBe("my default value");
}

public class SomeService: IService
{
    // There is no registered dependency for parameter "state" of type "string",
    // but constructor has the default parameter value "my default value"
    public SomeService(IDependency dependency, string state = "my default value")
    {
        Dependency = dependency;
        State = state;
    }

    public IDependency Dependency { get; }

    public string State { get; }
}
```



### Depends On



``` CSharp
static partial class MyBaseComposer
{
    static MyBaseComposer() => DI.Setup()
        .Bind<IDependency>().To<Dependency>();
}

static partial class MyDependentComposer
{
    static MyDependentComposer() => DI.Setup()
        .DependsOn(nameof(MyBaseComposer))
        .Bind<IService>().To<Service>();
}

// Resolve an instance of interface `IService`
var instance = MyDependentComposer.Resolve<IService>();
```

This sample references types from [this file](Pure.DI.UsageScenarios.Tests/Models.cs).

### Advanced generic autowiring

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
        // For other cases there are TTComparable, TTComparable<in T>,
        // TTEquatable<T>, TTEnumerable<out T>, TTDictionary<TKey, TValue> and etc.
        .Bind<IListService<TTList<int>>>().To<ListService<TTList<int>>>()
        // Bind using the custom generic parameters marker TCustom
        .Bind<IService<TTMy>>().Tags("custom tag").To<Service<TTMy>>()
        .Bind<Consumer>().To<Consumer>();

    // Resolve a generic instance
    var consumer = GenericAutowiringAdvancedDI.Resolve<Consumer>();
    
    consumer.Services2.Count.ShouldBe(2);
    // Check the instance's type
    foreach (var instance in consumer.Services2)
    {
        instance.ShouldBeOfType<Service<int>>();
    }

    consumer.Services1.ShouldBeOfType<ListService<IList<int>>>();
}

public class Consumer
{
    public Consumer(IListService<IList<int>> services1, ICollection<IService<int>> services2)
    {
        Services1 = services1;
        Services2 = services2;
    }
    
    public IListService<IList<int>> Services1 { get; }

    public ICollection<IService<int>> Services2 { get; }
}

// Custom generic type marker using predefined attribute `GenericTypeArgument`
[GenericTypeArgument]
class TTMy { }
```

This sample references types from [this file](Pure.DI.UsageScenarios.Tests/Models.cs).

### Dependency Injection with referencing implementations

Autowiring automatically injects dependencies based on implementations even if it is not defined in the configuration chain. :warning: But this approach is not recommended. When you follow the dependency inversion principle you want to make sure that you do not depend on anything concrete.

``` CSharp
public void Run()
{
    DI.Setup()
        .Bind<IService>().To<Service>();
    
    var instance = DependencyInjectionWithImplementationsDI.Resolve<IService>();
}

public class Dependency { }

public interface IService { }

public class Service : IService
{
    public Service(Dependency dependency) { }
}
```



### Default lifetime



``` CSharp
public void Run()
{
    DI.Setup()
        // Makes Singleton as default lifetime
        .Default(Singleton)
            .Bind<IDependency>().To<Dependency>()
        // Makes Transient as default lifetime
        .Default(Transient)
            .Bind<IService>().To<Service>();
    
    // Resolve the singleton twice
    var instance = DefaultLifetimeDI.Resolve<IService>();

    // Check that instances are equal
    instance.Dependency1.ShouldBe(instance.Dependency2);
}

public interface IDependency { }

public class Dependency : IDependency { }

public interface IService
{
    IDependency Dependency1 { get; }
    
    IDependency Dependency2 { get; }
}

public class Service : IService
{
    public Service(IDependency dependency1, IDependency dependency2)
    {
        Dependency1 = dependency1;
        Dependency2 = dependency2;
    }

    public IDependency Dependency1 { get; }
    
    public IDependency Dependency2 { get; }
}
```



### Per resolve lifetime



``` CSharp
public void Run()
{
    DI.Setup()
        .Bind<IDependency>().As(PerResolve).To<Dependency>()
        .Bind<IService>().To<Service>();

    // Track disposables
    var disposables = new List<IDisposable>();
    PerResolveLifetimeDI.OnDisposable += e => disposables.Add(e.Disposable);

    var instance = PerResolveLifetimeDI.Resolve<IService>();

    // Check that dependencies are equal
    instance.Dependency1.ShouldBe(instance.Dependency2);
    
    // Check disposable instances created
    disposables.Count.ShouldBe(1);
}

public interface IDependency { }

public class Dependency : IDependency, IDisposable
{
    public void Dispose() { }
}

public interface IService
{
    IDependency Dependency1 { get; }
    
    IDependency Dependency2 { get; }
}

public class Service : IService
{
    public Service(IDependency dependency1, IDependency dependency2)
    {
        Dependency1 = dependency1;
        Dependency2 = dependency2;
    }

    public IDependency Dependency1 { get; }
    
    public IDependency Dependency2 { get; }
}
```



### Singleton lifetime

[Singleton](https://en.wikipedia.org/wiki/Singleton_pattern) is a design pattern that supposes for having only one instance of some class during the whole application lifetime. The main complaint about Singleton is that it contradicts the Dependency Injection principle and thus hinders testability. It essentially acts as a global constant, and it is hard to substitute it with a test when needed. The _Singleton lifetime_ is indispensable in this case.

``` CSharp
public void Run()
{
    DI.Setup()
        // Use the Singleton lifetime
        .Bind<IDependency>().As(Singleton).To<Dependency>()
        .Bind<IService>().To<Service>();
    
    // Resolve the singleton twice
    var instance = SingletonLifetimeDI.Resolve<IService>();

    // Check that instances are equal
    instance.Dependency1.ShouldBe(instance.Dependency2);
    
    // Dispose of singletons, this method should be invoked once
    SingletonLifetimeDI.FinalDispose();
    instance.Dependency1.IsDisposed.ShouldBeTrue();
}

public interface IDependency
{
    bool IsDisposed { get; }
}

public class Dependency : IDependency, IDisposable
{
    public bool IsDisposed { get; private set; }
    
    public void Dispose() => IsDisposed = true;
}

public interface IService
{
    IDependency Dependency1 { get; }
    
    IDependency Dependency2 { get; }
}

public class Service : IService
{
    public Service(IDependency dependency1, IDependency dependency2)
    {
        Dependency1 = dependency1;
        Dependency2 = dependency2;
    }

    public IDependency Dependency1 { get; }
    
    public IDependency Dependency2 { get; }
}
```



### Transient lifetime



``` CSharp
public void Run()
{
    DI.Setup()
        .Bind<IDependency>().To<Dependency>()
        .Bind<IService>().To<Service>();

    // Track disposables to dispose of instances manually
    var disposables = new List<IDisposable>();
    TransientLifetimeDI.OnDisposable += e =>
    {
        if (e.Lifetime == Lifetime.Transient) disposables.Add(e.Disposable);
    };

    var instance = TransientLifetimeDI.Resolve<IService>();

    // Check that dependencies are not equal
    instance.Dependency1.ShouldNotBe(instance.Dependency2);
    
    // Check the number of transient disposable instances
    disposables.Count.ShouldBe(2);
    
    // Dispose instances
    disposables.ForEach(disposable => disposable.Dispose());
    disposables.Clear();
}

public interface IDependency { }

public class Dependency : IDependency, IDisposable
{
    public void Dispose() { }
}

public interface IService
{
    IDependency Dependency1 { get; }
    
    IDependency Dependency2 { get; }
}

public class Service : IService
{
    public Service(IDependency dependency1, IDependency dependency2)
    {
        Dependency1 = dependency1;
        Dependency2 = dependency2;
    }

    public IDependency Dependency1 { get; }
    
    public IDependency Dependency2 { get; }
}
```



### Custom singleton lifetime

*__IFactory__* is a powerful tool that allows controlling most the aspects while resolving dependencies.

``` CSharp
public void Run()
{
    DI.Setup()
        // Registers custom lifetime for all implementations with a class name ending by word "Singleton"
        .Bind<IFactory>().As(Singleton).To<CustomSingletonLifetime>()

        .Bind<IDependency>().To<DependencySingleton>()
        .Bind<IService>().To<Service>();
    
    var instance1 = CustomSingletonDI.Resolve<IService>();
    var instance2 = CustomSingletonDI.Resolve<IService>();

    // Check that dependencies are singletons
    instance1.Dependency.ShouldBe(instance2.Dependency);

    instance1.ShouldNotBe(instance2);
}

// A pattern of the class name ending by word "Singleton"
[Include(".*Singleton$")]
public class CustomSingletonLifetime: IFactory
{
    // Stores singleton instances by key
    private readonly ConcurrentDictionary<Key, object> _instances = new();
    
    // Gets an existing instance or creates a new
    public T Create<T>(Func<T> factory, object tag) =>
        (T)_instances.GetOrAdd(new Key(typeof(T), tag), i => factory()!);

    // Represents an instance key
    private record Key(Type type, object? tag);
}

public interface IDependency { }

public class DependencySingleton : IDependency { }

public interface IService { IDependency Dependency { get; } }

public class Service : IService
{
    public Service(IDependency dependency) { Dependency = dependency; }

    public IDependency Dependency { get; }
}
```



### Arrays

To resolve all possible instances of any tags of the specific type as an _array_ just use the injection of _T[]_.

``` CSharp
DI.Setup()
    .Bind<IDependency>().To<Dependency>()
    // Bind to the implementation #1
    .Bind<IService>().Tags(1).To<Service>()
    // Bind to the implementation #2
    .Bind<IService>().Tags(2, "abc").To<Service>()
    // Bind to the implementation #3
    .Bind<IService>().Tags(3).To<Service>()
    .Bind<CompositionRoot<IService[]>>()
        .To<CompositionRoot<IService[]>>();

// Resolve all appropriate instances
var composition = ArraysDI.Resolve<CompositionRoot<IService[]>>();
```

This sample references types from [this file](Pure.DI.UsageScenarios.Tests/Models.cs).

### Collections

To resolve all possible instances of any tags of the specific type as a _collection_ just use the injection _ICollection<T>_

``` CSharp
DI.Setup()
    .Bind<IDependency>().To<Dependency>()
    // Bind to the implementation #1
    .Bind<IService>().Tags(1).To<Service>()
    // Bind to the implementation #2
    .Bind<IService>().Tags(2, "abc").To<Service>()
    // Bind to the implementation #3
    .Bind<IService>().As(Lifetime.Singleton).Tags(3).To<Service>()
    .Bind<CompositionRoot<ICollection<IService>>>().To<CompositionRoot<ICollection<IService>>>();

// Resolve all appropriate instances
var composition = CollectionsDI.Resolve<CompositionRoot<ICollection<IService>>>();

// Check the number of resolved instances
composition.Root.Count.ShouldBe(3);
```

This sample references types from [this file](Pure.DI.UsageScenarios.Tests/Models.cs).

### Enumerables

To resolve all possible instances of any tags of the specific type as an _enumerable_ just use the injection _IEnumerable<T>_.

``` CSharp
DI.Setup()
    .Bind<IDependency>().To<Dependency>()
    // Bind to the implementation #1
    .Bind<IService>().Tags(1).To<Service>()
    // Bind to the implementation #2
    .Bind<IService>().Tags(2, "abc").To<Service>()
    // Bind to the implementation #3
    .Bind<IService>().Tags(3).To<Service>()
    .Bind<CompositionRoot<IEnumerable<IService>>>().To<CompositionRoot<IEnumerable<IService>>>();

// Resolve all appropriate instances
var instances = EnumerablesDI.Resolve<CompositionRoot<IEnumerable<IService>>>().Root.ToList();

// Check the number of resolved instances
instances.Count.ShouldBe(3);
```

This sample references types from [this file](Pure.DI.UsageScenarios.Tests/Models.cs).

### Func

_Func<>_ helps when a logic needs to inject some type of instances on-demand. Also, it is possible to solve circular dependency issues, but it is not the best way - better to reconsider the dependencies between classes.

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

This sample references types from [this file](Pure.DI.UsageScenarios.Tests/Models.cs).

### Lazy

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

This sample references types from [this file](Pure.DI.UsageScenarios.Tests/Models.cs).

### Sets

To resolve all possible instances of any tags of the specific type as a _ISet<>_ just use the injection _ISet<T>_.

``` CSharp
DI.Setup()
    .Bind<IDependency>().To<Dependency>()
    // Bind to the implementation #1
    .Bind<IService>().Tags(1).To<Service>()
    // Bind to the implementation #2
    .Bind<IService>().Tags(2, "abc").To<Service>()
    // Bind to the implementation #3
    .Bind<IService>().Tags(3).To<Service>()
    .Bind<CompositionRoot<ISet<IService>>>().To<CompositionRoot<ISet<IService>>>();

// Resolve all appropriate instances
var instances = SetsDI.Resolve<CompositionRoot<ISet<IService>>>().Root;

// Check the number of resolved instances
instances.Count.ShouldBe(3);
```

This sample references types from [this file](Pure.DI.UsageScenarios.Tests/Models.cs).

### Thread Local



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

This sample references types from [this file](Pure.DI.UsageScenarios.Tests/Models.cs).

### Tuples

[Tuple](https://docs.microsoft.com/en-us/dotnet/api/system.tuple) has a set of elements that should be resolved at the same time.

``` CSharp
DI.Setup()
    .Bind<IDependency>().To<Dependency>()
    .Bind<IService>().To<Service>()
    .Bind<INamedService>().To(ctx => new NamedService(ctx.Resolve<IDependency>(), "some name"))
    .Bind<CompositionRoot<(IService, INamedService)>>().To<CompositionRoot<(IService, INamedService)>>();

// Resolve an instance of type Tuple<IService, INamedService>
var (service, namedService) = TuplesDI.Resolve<CompositionRoot<(IService, INamedService)>>().Root;
```

This sample references types from [this file](Pure.DI.UsageScenarios.Tests/Models.cs).

### Decorator



``` CSharp
public void Run()
{
    DI.Setup()
        .Bind<IService>().Tags("base").To<Service>()
        .Bind<IService>().To<DecoratorService>();
    
    var service = DecoratorDI.Resolve<IService>();

    service.GetMessage().ShouldBe("Hello World !!!");
}

public interface IService { string GetMessage(); }

public class Service : IService {
    public string GetMessage() => "Hello World";
}

public class DecoratorService : IService
{
    private readonly IService _baseService;

    public DecoratorService([Tag("base")] IService baseService) => _baseService = baseService;

    public string GetMessage() => $"{_baseService.GetMessage()} !!!";
}
```



### Intercept specific types



``` CSharp
public void Run()
{
    DI.Setup()
        // Generates proxies
        .Bind<IProxyGenerator>().As(Singleton).To<ProxyGenerator>()
        // Controls creating instances of type Dependency
        .Bind<IFactory<Dependency>>().As(Singleton).To<MyInterceptor<Dependency>>()
        // Controls creating instances of type Service
        .Bind<IFactory<Service>>().As(Singleton).To<MyInterceptor<Service>>()

        .Bind<IDependency>().As(Singleton).To<Dependency>()
        .Bind<IService>().To<Service>();
    
    var instance = InterceptDI.Resolve<IService>();
    instance.Run();
    instance.Run();
    instance.Run();

    // Check number of invocations
    InterceptDI.Resolve<MyInterceptor<Service>>().InvocationCounter.ShouldBe(3);
    InterceptDI.Resolve<MyInterceptor<Dependency>>().InvocationCounter.ShouldBe(3);
}

public class MyInterceptor<T>: IFactory<T>, IInterceptor
    where T: class
{
    private readonly IProxyGenerator _proxyGenerator;

    public MyInterceptor(IProxyGenerator proxyGenerator) =>
        _proxyGenerator = proxyGenerator;

    public int InvocationCounter { get; private set; }

    public T Create(Func<T> factory) => 
        (T)_proxyGenerator.CreateClassProxyWithTarget(
            typeof(T),
            typeof(T).GetInterfaces(),
            factory(),
            this);

    void IInterceptor.Intercept(IInvocation invocation)
    {
        InvocationCounter++;
        invocation.Proceed();
    }
}

public interface IDependency { void Run();}

public class Dependency : IDependency { public void Run() {} }

public interface IService { void Run(); }

public class Service : IService
{
    private readonly IDependency? _dependency;

    public Service() { }

    public Service(IDependency dependency) { _dependency = dependency; }

    public void Run() { _dependency?.Run(); }
}
```



### Intercept a set of types



``` CSharp
public void Run()
{
    DI.Setup()
        // Generates proxies
        .Bind<IProxyGenerator>().As(Singleton).To<ProxyGenerator>()
        // Controls creating instances
        .Bind<IFactory>().Bind<MyInterceptor>().As(Singleton).To<MyInterceptor>()

        .Bind<IDependency>().As(Singleton).To<Dependency>()
        .Bind<IService>().To<Service>();
    
    var instance = InterceptManyDI.Resolve<IService>();
    instance.Run();
    instance.Run();

    // Check number of invocations
    InterceptManyDI.Resolve<MyInterceptor>().InvocationCounter.ShouldBe(4);
}

[Exclude(nameof(ProxyGenerator))]
public class MyInterceptor: IFactory, IInterceptor
{
    private readonly IProxyGenerator _proxyGenerator;

    public MyInterceptor(IProxyGenerator proxyGenerator) =>
        _proxyGenerator = proxyGenerator;
    
    public int InvocationCounter { get; private set; }

    public T Create<T>(Func<T> factory, object tag) => 
        (T)_proxyGenerator.CreateClassProxyWithTarget(
            typeof(T),
            typeof(T).GetInterfaces(),
            factory(),
            this);

    void IInterceptor.Intercept(IInvocation invocation)
    {
        InvocationCounter++;
        invocation.Proceed();
    }
}

public interface IDependency { void Run();}

public class Dependency : IDependency { public void Run() {} }

public interface IService { void Run(); }

public class Service : IService
{
    private readonly IDependency? _dependency;

    public Service() { }

    public Service(IDependency dependency) { _dependency = dependency; }

    public void Run() { _dependency?.Run(); }
}
```



### ASPNET



``` CSharp
public class AspNetMvc
{
    public async Task Run()
    {
        var hostBuilder = new WebHostBuilder().UseStartup<Startup>();
        using var server = new TestServer(hostBuilder);
        using var client = server.CreateClient();
        
        var response = await client.GetStringAsync("/Greeting");
        response.ShouldBe("Hello!");
    }
}

public interface IGreeting
{
    string Hello { get; }
}

public class Greeting : IGreeting
{
    public string Hello => "Hello!";
}

[ApiController]
[Route("[controller]")]
public class GreetingController : ControllerBase
{
    private readonly IGreeting _greeting;

    public GreetingController(IGreeting greeting) => _greeting = greeting;

    [HttpGet] public string Get() => _greeting.Hello;
}

public static partial class GreetingDomain
{
    static GreetingDomain()
    {
        DI.Setup()
            .Bind<IGreeting>().As(Lifetime.ContainerSingleton).To<Greeting>()
            .Bind<GreetingController>().To<GreetingController>();
    }
}

public class Startup
{
    public Startup(Microsoft.Extensions.Configuration.IConfiguration configuration) =>
        Configuration = configuration;

    public Microsoft.Extensions.Configuration.IConfiguration Configuration { get; }

    public void ConfigureServices(IServiceCollection services)
    {
        services.AddControllers();
        services.AddGreetingDomain();
    }

    public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
    {
        app.UseRouting();
        app.UseEndpoints(endpoints =>
        {
            endpoints.MapControllers();
        });
    }
}
```



