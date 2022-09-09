
## Usage Scenarios

- Basics
  - [Composition Root](#composition-root)
  - [Constants](#constants)
  - [Generics](#generics)
  - [Manual binding](#manual-binding)
  - [Service collection](#service-collection)
  - [Tags](#tags)
  - [Aspect-oriented DI](#aspect-oriented-di)
  - [Service provider](#service-provider)
  - [Several contracts](#several-contracts)
  - [Aspect-oriented DI with custom attributes](#aspect-oriented-di-with-custom-attributes)
  - [Instance initialization](#instance-initialization)
  - [Record structs](#record-structs)
  - [Records](#records)
  - [Dependency tag](#dependency-tag)
  - [Injection of default parameters](#injection-of-default-parameters)
  - [Injection of nullable parameters](#injection-of-nullable-parameters)
  - [Init property](#init-property)
  - [Complex generics](#complex-generics)
  - [Complex generics with constraints](#complex-generics-with-constraints)
  - [Depends On](#depends-on)
  - [Default factory](#default-factory)
  - [Unbound instance resolving](#unbound-instance-resolving)
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
  - [Lazy with metadata](#lazy-with-metadata)
  - [Sets](#sets)
  - [Thread Local](#thread-local)
  - [Tuples](#tuples)
  - [Multi statement func](#multi-statement-func)
  - [Array binding override](#array-binding-override)
- Interception
  - [Decorator](#decorator)
  - [Intercept specific types](#intercept-specific-types)
  - [Intercept a set of types](#intercept-a-set-of-types)
  - [Intercept advanced](#intercept-advanced)
- Samples
  - [ASPNET](#aspnet)
  - [OS specific implementations](#os-specific-implementations)

### Composition Root

This sample demonstrates the most efficient way of getting a composition root object, free from any impact on memory consumption and performance.

``` CSharp
DI.Setup("Composer")
    .Bind<IDependency>().To<Dependency>()
    .Bind<IService>().To<Service>();

// Resolves an instance of interface `IService`
var instance = Composer.ResolveIService();
```

Actually, the method _ResolveIService_ looks like this:
```csharp
[MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
public static IService ResolveIService() => new Service(new Dependency());
```
and the compiler just inserts this set of constructor calls instead of ```Composer.ResolveIService()```:
```csharp
new Service(new Dependency())
```

### Constants

It's obvious here.

``` CSharp
DI.Setup()
    .Bind<int>().To(_ => 10);

// Resolve an integer
var val = ConstantsDI.ResolveInt();
// Check the value
val.ShouldBe(10);
```

The compiler replaces the statement:
```CSharp
var val = ConstantsDI.ResolveInt();
```
by the statement:
```CSharp
var val = 10;
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
    // Bind a generic type
    .Bind<IService<TT>>().To<Service<TT>>()
    .Bind<Consumer>().To<Consumer>();

var instance = GenericsDI.Resolve<Consumer>();
```

Open generic type instance, for instance, like IService&lt;TT&gt; here, cannot be a composition root instance. This sample references types from [this file](Pure.DI.UsageScenarios.Tests/Models.cs).
The actual composition for the example above looks like this:
```CSharp
new Consumer(new Service<int>(Dependency()));
```

### Manual binding

We can specify a constructor manually with all its arguments and even call some methods before an instance will be returned to consumers. Would also like to point out that invocations like *__ctx.Resolve<>()__* will be replaced by a related expression to create a required composition for the performance boost where possible, except when it might cause a circular dependency.

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

The actual composition for the example above looks like this:
```CSharp
new Service(new Dependency()), "some state");
```
... and no any additional method calls. This sample references types from [this file](Pure.DI.UsageScenarios.Tests/Models.cs).

### Service collection

In the cases when a project references the Microsoft Dependency Injection library, an extension method for ```IServiceCollection``` is generating automatically with a name like _Add..._ plus the name of a generated class, here it is ```AddMyComposer()``` for class ```public class MyComposer { }```.

``` CSharp
[Fact]
public void Run()
{
    DI.Setup("MyComposer")
        .Bind<IDependency>().As(Lifetime.Singleton).To<Dependency>()
        .Bind<IService>().To<Service>();
    
    var serviceProvider =
        // Creates some serviceCollection
        new ServiceCollection()
            // Adds some registrations with any lifetime
            .AddScoped<ServiceConsumer>()
        // Adds registrations produced by Pure DI above
        .AddMyComposer()
        // Builds a service provider
        .BuildServiceProvider();
    
    var consumer = serviceProvider.GetRequiredService<ServiceConsumer>();
    var instance = serviceProvider.GetRequiredService<IService>();
    consumer.Service.Dependency.ShouldBe(instance.Dependency);
    consumer.Service.ShouldNotBe(instance);

    // Creates a service provider directly
    var otherServiceProvider = MyComposer.Resolve<IServiceProvider>();
    var otherInstance = otherServiceProvider.GetRequiredService<IService>();
    otherInstance.Dependency.ShouldBe(consumer.Service.Dependency);
}

public class ServiceConsumer
{
    public ServiceConsumer(IService service) =>
        Service = service;

    public IService Service { get; }
}
```



### Tags

Tags are useful while binding to several implementations of the same abstract types.

``` CSharp
public void Run()
{
    DI.Setup()
        .Bind<IDependency>().To<Dependency>()
        // Adds some tags for a specific contract
        .Bind<IService>("Service 1").To<Service>()
        // Adds some tags for a binding
        .Bind<IService>().Tags("Service 2", 2).As(Lifetime.Singleton).To<ServiceRecord>()
        .Bind<Consumer>().To<Consumer>();

    var consumer = TagsDI.Resolve<Consumer>();
    consumer.Service1.ShouldBeOfType<Service>();
    consumer.Service2.ShouldBeOfType<ServiceRecord>();
    consumer.Service3.ShouldBe(consumer.Service2);
}

internal class Consumer
{
    public Consumer(
        [Tag("Service 1")] IService service1,
        [Tag("Service 2")] IService service2,
        [Tag(2)] IService service3)
    {
        Service1 = service1;
        Service2 = service2;
        Service3 = service3;
    }
    
    public IService Service1 { get; }

    public IService Service2 { get; }

    public IService Service3 { get; }
}
```

This sample references types from [this file](Pure.DI.UsageScenarios.Tests/Models.cs).

### Aspect-oriented DI



``` CSharp
public void Run()
{
    DI.Setup()
        .Bind<IConsole>().Tags("MyConsole").To(_ => AspectOriented.Console.Object)
        .Bind<string>().Tags("Prefix").To(_ => "info")
        .Bind<ILogger>().As(Singleton).To<Logger>();

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

    // Method injection using the specified type "Clock"
    // Starting C# 11 you can use a generic attribute like Type<Clock>
    // otherwise use an attribute like Type(typeof(Clock)) instead
    [Order(1)] public void Initialize([Type<Clock>] IClock clock) => _clock = clock;

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



### Service provider

It is easy to get an instance of the _IServiceProvider_ type at any time without any additional effort.

``` CSharp
DI.Setup()
    .Bind<IDependency>().To<Dependency>()
    .Bind<IService>().To<Service>();

// Resolve the instance of IServiceProvider
var serviceProvider = ServiceProviderDI.Resolve<IServiceProvider>();

// Get the instance via service provider
var instance = serviceProvider.GetService(typeof(IService));
```



### Several contracts

It is possible to bind several types to a single implementation.

``` CSharp
DI.Setup()
    .Bind<IDependency>().To<Dependency>()
    .Bind<IService>().Bind<IAnotherService>().To<Service>();

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
        // Starting C# 11 you can use a generic attributes
        .TypeAttribute<MyTypeAttribute<TT>>()

        .Bind<IConsole>().Tags("MyConsole").To(_ => AspectOrientedWithCustomAttributes.Console.Object)
        .Bind<string>().Tags("Prefix").To(_ => "info")
        .Bind<ILogger>().As(Singleton).To<Logger>();

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
    public MyTypeAttribute(Type type) { }
}

// Starting C# 11 you can use a generic attributes
[AttributeUsage(
    AttributeTargets.Parameter
    | AttributeTargets.Property
    | AttributeTargets.Field)]
public class MyTypeAttribute<T> : Attribute
{
}

// Represents the dependency aspect attribute to specify a tag for injection.
[AttributeUsage(
    AttributeTargets.Parameter
    | AttributeTargets.Property
    | AttributeTargets.Field)]
public class MyTagAttribute : Attribute
{
    public MyTagAttribute(object tag) { }
}

// Represents the dependency aspect attribute to specify an order for injection.
[AttributeUsage(
    AttributeTargets.Constructor
    | AttributeTargets.Method
    | AttributeTargets.Property
    | AttributeTargets.Field)]
public class MyOrderAttribute : Attribute
{
    public MyOrderAttribute(int order) { }
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
    [MyOrder(1)] public void Initialize([MyType<Clock>] IClock clock) => _clock = clock;

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



### Instance initialization

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
var instance = InstanceInitializationDI.Resolve<INamedService>();

// Check the instance
instance.ShouldBeOfType<InitializingNamedService>();

// Check that the initialization has took place
instance.Name.ShouldBe("Initialized!");
```

This sample references types from [this file](Pure.DI.UsageScenarios.Tests/Models.cs).

### Records



``` CSharp
public void Run()
{
    DI.Setup()
        .Bind<IDependency>().To<Dependency>()
        .Bind<IService>().To<RecordService>();
    
    var service = RecordsDI.Resolve<IService>();
    service.ShouldBeOfType<RecordService>();
}

public record RecordService(IDependency Dependency, string State = "") : IService;
```



### Record structs



``` CSharp
public void Run()
{
    DI.Setup()
        .Bind<IDependency>().To<Dependency>()
        .Bind<IService>().To<RecordStructService>();
    
    var service = RecordStructsDI.Resolve<IService>();
    service.ShouldBeOfType<RecordStructService>();
}

public readonly record struct RecordStructService(IDependency Dependency, string State = "") : IService;
```



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



### Injection of nullable parameters



``` CSharp
public void Run()
{
    DI.Setup()
        .Bind<IDependency>().To<Dependency>()
        .Bind<IService>().To<SomeService>();

    // Resolve an instance
    var instance = NullableParamsInjectionDI.Resolve<IService>();

    // Check the optional dependency
    instance.State.ShouldBe("my default value");
}

public class SomeService: IService
{
    // There is no registered dependency for parameter "state" of type "string",
    // but parameter "state" has a nullable annotation
    public SomeService(IDependency dependency, string? state)
    {
        Dependency = dependency;
        State = state ?? "my default value";
    }

    public IDependency Dependency { get; }

    public string State { get; }
}
```



### Init property



``` CSharp
public void Run()
{
    DI.Setup()
        .Bind<IService>().To<MyService>()
        .Bind<IDependency>().To<Dependency>();

    var service = InitPropertyDI.Resolve<IService>();
}

public class MyService: IService
{
    [Order(0)] public IDependency Dependency { get; init; }
    
    public string State => "Some state";
}        
```



### Complex generics

Autowiring generic types is as easy as autowiring other simple types. Just use generic parameter markers like _TT_, _TT1_, _TT2_ etc or TTI, TTI1, TTI2... for interfaces or _TTS_, _TTS1_, _TTS2_... for value types or other special markers like _TTDisposable_ , _TTDisposable1_ , etc. _TTList <>_, _TTDictionary<>_ ... or create your own generic markers like _TTMy_ in the example below. Your own generic markers must meet 2 conditions: the type name must start with _TT_ and the type must have the _[GenericTypeArgument]_ attribute.

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
    var consumer = ComplexGenericsDI.Resolve<Consumer>();
    
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

// Custom generic type marker:
// The type name should start from "TT"
// and this type should have the attribute "GenericTypeArgument"
[GenericTypeArgument]
interface TTMy { }
```

This sample references types from [this file](Pure.DI.UsageScenarios.Tests/Models.cs).

### Complex generics with constraints



``` CSharp
public class Program
{
    public Program(IConsumer<int> consumer)
    { }
}

public interface IConsumer<T>
{ }

public class Consumer<T>: IConsumer<T>
{
    public Consumer(IService<T, string, IDictionary<T, string[]>> service) { }
}

public interface IService<T1, T2, T3>
    where T3: IDictionary<T1, T2[]>
{ }

public class Service<T1, T2, T3> : IService<T1, T2, T3>
    where T3: IDictionary<T1, T2[]>
{ }

DI.Setup()
    .Bind<Program>().To<Program>()
    // Bind complex generic types
    .Bind<IService<TT1, TT2, IDictionary<TT1, TT2[]>>>().To<Service<TT1, TT2, IDictionary<TT1, TT2[]>>>()
    .Bind<IConsumer<TT>>().To<Consumer<TT>>();

// var instance = new Program(new Consumer<int>(new Service<int, string, System.Collections.Generic.IDictionary<int, string[]>>()));
var instance = ComplexGenericsWithConstraintsDI.Resolve<Program>();
```



### Depends On

Sometimes it becomes necessary to reuse a set of bindings in several composers. To do this, you can use the `DependsOn` function passing the name of the composer, where to get the set for reuse. It is important to note that this method works for composers within the same project.

``` CSharp
static partial class MyBaseComposer
{
    static void Setup() => DI.Setup()
        .Bind<IDependency>().To<Dependency>();
}

static partial class MyDependentComposer
{
    static void Setup() => DI.Setup()
        .DependsOn(nameof(MyBaseComposer))
        .Bind<IService>().To<Service>();
}

// Resolve an instance of interface `IService`
var instance = MyDependentComposer.Resolve<IService>();
```

This sample references types from [this file](Pure.DI.UsageScenarios.Tests/Models.cs).

### Default factory

Sometimes it is necessary to add custom dependency resolution logic for types that do not have any bindings defined. In this case, you can only use factory binding for the generic type marker and implement your own dependency resolution logic, as in the example below:

``` CSharp
public void Run()
{
    DI.Setup()
        .Bind<TT>().To(ctx =>
        {
            // Put any logic here to create an instance of the TT type
            // For example, some IoC container can be used to obtain an instance.
            if (typeof(TT) == typeof(int))
            {
                return (TT)(object)33;
            }
            
            if (typeof(TT) == typeof(string))
            {
                return (TT)(object)"Abc";
            }

            throw new Exception("Unknown type.");
        })
        .Bind<Consumer>().To<Consumer>();
    
    var instance = DefaultFactoryDI.Resolve<Consumer>();
    instance.Value.ShouldBe(33);
    instance.Text.ShouldBe("Abc");
}

public record Consumer(int Value, string Text);
```



### Unbound instance resolving

Autowiring automatically injects dependencies based on implementations even if it does not have an appropriate binding. :warning: This approach is not recommended. When you follow the dependency inversion principle you want to make sure that you do not depend on anything concrete.

``` CSharp
public void Run()
{
    DI.Setup()
        .Bind<IService>().To<Service>();
    
    var instance = UnboundInstanceResolvingDI.Resolve<IService>();
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
    public void Dispose() => GC.SuppressFinalize(this);
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
    var dependency = SingletonLifetimeDI.ResolveSingletonLifetimeIDependency();

    // Check that instances are equal
    instance.Dependency1.ShouldBe(instance.Dependency2);
    instance.Dependency1.ShouldBe(dependency);
    
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
    
    public void Dispose()
    {
        IsDisposed = true;
        GC.SuppressFinalize(this);
    }
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
    public void Dispose() => GC.SuppressFinalize(this);
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
        // Registers the custom lifetime for all implementations with a class name ending by word "Singleton"
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
    public T Create<T>(Func<T> factory, Type implementationType, object tag) =>
        (T)_instances.GetOrAdd(new Key(implementationType, tag), _ => factory()!);

    // Represents an instance key
    [SuppressMessage("ReSharper", "NotAccessedPositionalProperty.Local")]
    private record Key(Type Type, object? Tag);
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
    .Bind<IService>(1).As(Lifetime.PerResolve).To<Service>()
    // Bind to the implementation #2
    .Bind<IService>(99).Tags(2, "abc").To<Service>()
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
    .Bind<IService>(1).As(PerResolve).To<Service>()
    // Bind to the implementation #2
    .Bind<IService>(2).Tags("abc").To<Service>()
    // Bind to the implementation #3
    .Bind<IService>().As(Singleton).Tags(3).To<Service>()
    .Bind<CompositionRoot<ICollection<IService>>>().To<CompositionRoot<ICollection<IService>>>();

// Resolve all appropriate instances
var composition = CollectionsDI.ResolveCompositionRootICollectionIService();

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

_Func<>_ with the required type specified helps when a logic needs to inject some type of instances on-demand. Also, it is possible to solve circular dependency issues, but it is not the best way - better to reconsider the dependencies between classes.

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

_Lazy_ dependency helps when a logic needs to inject _Lazy<T>_ to get instance once on-demand.

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

### Lazy with metadata

_Lazy_ dependency helps when a logic needs to inject _Lazy<T, TMetadata>_ to get instance once on-demand and the metadata associated with the referenced object.

``` CSharp
DI.Setup()
    .Bind<IDependency>().To<Dependency>()
    .Bind<IService>().To<Service>()
    .Bind<IService<TT>>().To<Service<TT>>()
    .Bind<CompositionRoot<Lazy<IService, IService<int>>>>().To<CompositionRoot<Lazy<IService, IService<int>>>>();

// Resolve the instance of Lazy<IService> with some metadata, for instance of type IService<int>
var lazy = LazyWithMetadataDI.Resolve<CompositionRoot<Lazy<IService, IService<int>>>>().Root;

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
    .Bind<IService>().Tags(2, "abc").As(Singleton).To<Service>()
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

### Multi statement func



``` CSharp
public void Run()
{
    DI.Setup()
        .Bind<MultiStatementFunc.MyService>().To<MultiStatementFunc.MyService>()
        .Bind<Func<string, MultiStatementFunc.IMyService>>().To(ctx => new Func<string, MultiStatementFunc.IMyService>(name =>
        {
            var service = ctx.Resolve<MyService>();
            service.Name = name;
            return service;
        }))
        .Bind<Consumer>().To<Consumer>();

    // Resolve function to create instances
    var consumer = MultiStatementFuncDI.Resolve<Consumer>();

    consumer.Service.Name.ShouldBe("Abc");
}

public interface IMyService
{
    string Name { get; }
}

public class MyService: IMyService
{
    public string Name { get; set; } = "";
}

public class Consumer
{
    public Consumer(Func<string, IMyService> factory) => Service = factory("Abc");

    public IMyService Service { get; }
}
```



### Array binding override



``` CSharp
DI.Setup()
    .Bind<IDependency>().To<Dependency>()
    // Bind to the implementation #1
    .Bind<IService>(1).To<Service>()
    // Bind to the implementation #2
    .Bind<IService>(99).Tags(2, "abc").To<Service>()
    // Bind to the implementation #3
    .Bind<IService>().Tags(3).To<Service>()
    // Bind array
    .Bind<IService[]>().To(ctx => new[] {ctx.Resolve<IService>(1), ctx.Resolve<IService>("abc")})
    .Bind<CompositionRoot<IService[]>>()
        .To<CompositionRoot<IService[]>>();

var composition = ArrayBindingOverrideDI.Resolve<CompositionRoot<IService[]>>();
```



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
        .Default(Singleton)
        // Generates proxies
        .Bind<IProxyGenerator>().To<ProxyGenerator>()
        // Controls creating instances of type Dependency
        .Bind<IFactory<IDependency>>().To<MyInterceptor<IDependency>>()
        // Controls creating instances of type Service
        .Bind<IFactory<IService>>().To<MyInterceptor<IService>>()

        .Bind<IDependency>().To<Dependency>()
        .Bind<IService>().As(Transient).To<Service>();
    
    var instance = InterceptDI.Resolve<IService>();
    instance.Run();
    instance.Run();
    instance.Run();

    // Check number of invocations
    ((MyInterceptor<IService>)InterceptDI.Resolve<IFactory<IService>>()).InvocationCounter.ShouldBe(3);
    ((MyInterceptor<IDependency>)InterceptDI.Resolve<IFactory<IDependency>>()).InvocationCounter.ShouldBe(3);
}

public class MyInterceptor<T>: IFactory<T>, IInterceptor
    where T: class
{
    private readonly IProxyGenerator _proxyGenerator;

    public MyInterceptor(IProxyGenerator proxyGenerator) =>
        _proxyGenerator = proxyGenerator;

    public int InvocationCounter { get; private set; }

    public T Create(Func<T> factory, Type implementationType, object tag) =>
        _proxyGenerator.CreateInterfaceProxyWithTarget(factory(), this);

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

    public Service(IDependency dependency) { _dependency = dependency; }

    public void Run() { _dependency?.Run(); }
}
```



### Intercept advanced

This approach represents a fastest way of working with interceptors.

``` CSharp
public void Run()
{
    DI.Setup()
        .Default(Singleton)
        // Generates proxies
        .Bind<IProxyBuilder>().To<DefaultProxyBuilder>()
        // Controls creating instances of types Dependency and Service filtered by the [Include(...)] attribute
        .Bind<IFactory<TT>>().To<MyInterceptor<TT>>()
        
        .Bind<IDependency>().To<Dependency>()
        .Bind<IService>().As(Transient).To<Service>();
    
    var instance = InterceptAdvancedDI.Resolve<IService>();
    instance.Run();
    instance.Run();
    instance.Run();

    // Check number of invocations
    ((MyInterceptor<IService>)InterceptAdvancedDI.Resolve<IFactory<IService>>()).InvocationCounter.ShouldBe(3);
    ((MyInterceptor<IDependency>)InterceptAdvancedDI.Resolve<IFactory<IDependency>>()).InvocationCounter.ShouldBe(3);
}

// Filters for Service and for Dependency classes
[Include("(Service|Dependency)$")]
public class MyInterceptor<T>: IFactory<T>, IInterceptor
    where T: class
{
    private readonly Func<T, T> _proxyFactory;

    public MyInterceptor(IProxyBuilder proxyBuilder) =>
        _proxyFactory = CreateProxyFactory(proxyBuilder, this);

    public int InvocationCounter { get; private set; }

    public T Create(Func<T> factory, Type implementationType, object tag) =>
        // Creates a proxy for an instance
        _proxyFactory(factory());

    void IInterceptor.Intercept(IInvocation invocation)
    {
        InvocationCounter++;
        invocation.Proceed();
    }
    
    // Compiles a delegate to create a proxy for the performance boost
    private static Func<T, T> CreateProxyFactory(IProxyBuilder proxyBuilder, params IInterceptor[] interceptors)
    {
        var proxyType = proxyBuilder.CreateInterfaceProxyTypeWithTargetInterface(typeof(T), Type.EmptyTypes, ProxyGenerationOptions.Default);
        var ctor = proxyType.GetConstructors().Single(i => i.GetParameters().Length == 2);
        var instance = Expression.Parameter(typeof(T));
        var newProxyExpression = Expression.New(ctor, Expression.Constant(interceptors), instance);
        return Expression.Lambda<Func<T, T>>(newProxyExpression, instance).Compile();
    }
}

public interface IDependency { void Run();}

public class Dependency : IDependency { public void Run() {} }

public interface IService { void Run(); }

public class Service : IService
{
    private readonly IDependency? _dependency;

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

    public T Create<T>(Func<T> factory, Type implementationType, object tag) => 
        (T)_proxyGenerator.CreateInterfaceProxyWithTarget(typeof(T), factory(), this);

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
        // AddGreetingDomain(this IServiceCollection services) method was generated automatically
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



### OS specific implementations



``` CSharp
public void Run()
{
    DI.Setup()
        .Bind<IOsSpecific<TT>>().As(Lifetime.Singleton).To<OsSpecific<TT>>()

        // OS specific bindings
        .Bind<IDependency>(OSSpecificImplementations.OSPlatform.Windows).To<WindowsImpl>()
        .Bind<IDependency>(OSSpecificImplementations.OSPlatform.Linux).To<LinuxImpl>()
        .Bind<IDependency>(OSSpecificImplementations.OSPlatform.OSX).To<OSXImpl>()
        .Bind<IDependency>().To(ctx => ctx.Resolve<IOsSpecific<IDependency>>().Instance)

        // Other bindings
        .Bind<IService>().To<Service>();
    
    var service = OSSpecificImplementationsDI.Resolve<IService>();

    service.Run().Contains("Hello from").ShouldBeTrue();
}

public interface IOsSpecific<out T> {  T Instance { get; } }

public enum OSPlatform
{
    Windows,
    Linux,
    OSX
}

public class OsSpecific<T>: IOsSpecific<T>
{
    private readonly Func<T> _windowsFactory;
    private readonly Func<T> _linuxFactory;
    private readonly Func<T> _osxFactory;

    public OsSpecific(
        [Tag(OSSpecificImplementations.OSPlatform.Windows)] Func<T> windowsFactory,
        [Tag(OSSpecificImplementations.OSPlatform.Linux)] Func<T> linuxFactory,
        [Tag(OSSpecificImplementations.OSPlatform.OSX)] Func<T> osxFactory)
    {
        _windowsFactory = windowsFactory;
        _linuxFactory = linuxFactory;
        _osxFactory = osxFactory;
    }

    public T Instance =>
            Environment.OSVersion.Platform switch
            {
                PlatformID.Win32S => OSPlatform.Windows,
                PlatformID.Win32Windows => OSPlatform.Windows,
                PlatformID.Win32NT => OSPlatform.Windows,
                PlatformID.WinCE => OSPlatform.Windows,
                PlatformID.Xbox => OSPlatform.Windows,
                PlatformID.Unix => OSPlatform.Linux,
                PlatformID.MacOSX => OSPlatform.OSX,
                _ => throw new NotSupportedException()
            } switch
            {
                OSPlatform.Windows => _windowsFactory(),
                OSPlatform.Linux => _linuxFactory(),
                OSPlatform.OSX => _osxFactory(),
                _ => throw new NotSupportedException()
            };
}

public interface IDependency { string GetMessage(); }

public class WindowsImpl : IDependency { public string GetMessage() => "Hello from Windows"; }

public class LinuxImpl : IDependency { public string GetMessage() => "Hello from Linux"; }

public class OSXImpl : IDependency { public string GetMessage() => "Hello from OSX"; }

public interface IService { string Run(); }

public class Service : IService
{
    private readonly IDependency _dependency;
    
    public Service(IDependency dependency) => _dependency = dependency;

    public string Run() => _dependency.GetMessage();
}
```



