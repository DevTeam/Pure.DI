This Markdown-formatted document contains information about working with Pure.DI

# Usage scenarios.

## Auto-bindings

Pure.DI can create non-abstract types without explicit bindings, which makes quick prototypes and small demos concise.
The generator still validates the graph at compile time and produces regular C# object creation code.

```c#
using Pure.DI;

// Specifies to create a partial class named "Composition"
DI.Setup("Composition")
    // with the root "Orders"
    .Root<OrderService>("Orders");

var composition = new Composition();

// service = new OrderService(new Database())
var orders = composition.Orders;

class Database;

class OrderService(Database database);
```

To run the above code, the following NuGet package must be added:
 - [Pure.DI](https://www.nuget.org/packages/Pure.DI)

Auto-bindings are convenient for utilities and sample code where dependency choices are obvious.
In larger applications they can hide architectural intent, because consumers start depending on concrete classes.
If you need interchangeable implementations and explicit lifetime control, prefer bindings of abstractions to implementations.
>[!WARNING]
>This approach is not recommended if you follow the dependency inversion principle or need precise lifetime control.

Prefer injecting abstractions (for example, interfaces) and map them to implementations as in most [other examples](injections-of-abstractions.md).
Limitations: auto-bindings scale poorly when several implementations, decorators, or strict lifetime rules are required.
Common pitfalls:
- Relying on concrete classes in domain code instead of abstractions.
- Losing explicit control over lifetime choices during refactoring.
See also: [Injections of abstractions](injections-of-abstractions.md), [Simplified binding](simplified-binding.md).

## Injections of abstractions

This is the recommended model for production code: depend on abstractions and bind them to implementations in composition.
It keeps business code independent from infrastructure details and makes replacements predictable.

```c#
using Pure.DI;

DI.Setup(nameof(Composition))
    // Binding abstractions to their implementations:
    // The interface IGpsSensor is bound to the implementation GpsSensor
    .Bind<IGpsSensor>().To<GpsSensor>()
    // The interface INavigationSystem is bound to the implementation NavigationSystem
    .Bind<INavigationSystem>().To<NavigationSystem>()

    // Specifies to create a composition root
    // of type "VehicleComputer" with the name "VehicleComputer"
    .Root<VehicleComputer>("VehicleComputer");

var composition = new Composition();

// Usage:
// var vehicleComputer = new VehicleComputer(new NavigationSystem(new GpsSensor()));
var vehicleComputer = composition.VehicleComputer;

vehicleComputer.StartTrip();

// The sensor abstraction
interface IGpsSensor;

// The sensor implementation
class GpsSensor : IGpsSensor;

// The service abstraction
interface INavigationSystem
{
    void Navigate();
}

// The service implementation
class NavigationSystem(IGpsSensor sensor) : INavigationSystem
{
    public void Navigate()
    {
        // Navigation logic using the sensor...
    }
}

// The consumer of the abstraction
partial class VehicleComputer(INavigationSystem navigationSystem)
{
    public void StartTrip() => navigationSystem.Navigate();
}
```

To run the above code, the following NuGet package must be added:
 - [Pure.DI](https://www.nuget.org/packages/Pure.DI)

The binding chain maps abstractions to concrete types so the generator can build a fully concrete object graph. This keeps consumers decoupled and allows swapping implementations. A single implementation can satisfy multiple abstractions.
>[!TIP]
>If a binding is missing, injection still works when the consumer requests a concrete type (not an abstraction).

Limitations: explicit bindings add configuration lines, but the trade-off is clearer architecture and safer evolution.
Common pitfalls:
- Mixing abstraction-first and concrete-only styles in one module without clear boundaries.
- Forgetting to bind alternate implementations for tagged use cases.
See also: [Auto-bindings](auto-bindings.md), [Tags](tags.md).

## Simplified binding

You can call `Bind()` without type parameters to infer contracts from the implementation type.
This reduces boilerplate while preserving compile-time graph validation.

```c#
using System.Collections;
using Pure.DI;

// Specifies to create a partial class "Composition"
DI.Setup(nameof(Composition))
    // Begins the binding definition for the implementation type itself,
    // and if the implementation is not an abstract class or structure,
    // for all abstract but NOT special types that are directly implemented.
    // Equivalent to:
    // .Bind<IOrderRepository, IOrderNotification, OrderManager>()
    //   .As(Lifetime.PerBlock)
    //   .To<OrderManager>()
    .Bind().As(Lifetime.PerBlock).To<OrderManager>()
    .Bind().To<Shop>()

    // Specifies to create a property "MyShop"
    .Root<IShop>("MyShop");

var composition = new Composition();
var shop = composition.MyShop;

interface IManager;

class ManagerBase : IManager;

interface IOrderRepository;

interface IOrderNotification;

class OrderManager :
    ManagerBase,
    IOrderRepository,
    IOrderNotification,
    IDisposable,
    IEnumerable<string>
{
    public void Dispose() {}

    public IEnumerator<string> GetEnumerator() =>
        new List<string> { "Order #1", "Order #2" }.GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}

interface IShop;

class Shop(
    OrderManager manager,
    IOrderRepository repository,
    IOrderNotification notification)
    : IShop;
```

To run the above code, the following NuGet package must be added:
 - [Pure.DI](https://www.nuget.org/packages/Pure.DI)

In practice, most abstraction types can be inferred. The parameterless `Bind()` binds:

- the implementation type itself
- and, if it is NOT abstract,
  - all abstract types it directly implements
  - except special types

Special types will not be added to bindings:

- `System.Object`
- `System.Enum`
- `System.MulticastDelegate`
- `System.Delegate`
- `System.Collections.IEnumerable`
- `System.Collections.Generic.IEnumerable<T>`
- `System.Collections.Generic.IList<T>`
- `System.Collections.Generic.ICollection<T>`
- `System.Collections.IEnumerator`
- `System.Collections.Generic.IEnumerator<T>`
- `System.Collections.Generic.IReadOnlyList<T>`
- `System.Collections.Generic.IReadOnlyCollection<T>`
- `System.IDisposable`
- `System.IAsyncResult`
- `System.AsyncCallback`

If you want to add your own special type, use the `SpecialType<T>()` call.

For class `OrderManager`, `Bind().To<OrderManager>()` is equivalent to `Bind<IOrderRepository, IOrderNotification, OrderManager>().To<OrderManager>()`. The types `IDisposable` and `IEnumerable<string>` are excluded because they are special. `ManagerBase` is excluded because it is not abstract. `IManager` is excluded because it is not implemented directly by `OrderManager`.

|    |                       |                                                   |
|----|-----------------------|---------------------------------------------------|
| ✅ | `OrderManager`        | implementation type itself                        |
| ✅ | `IOrderRepository`    | directly implements                               |
| ✅ | `IOrderNotification`  | directly implements                               |
| ❌ | `IDisposable`         | special type                                      |
| ❌ | `IEnumerable<string>` | special type                                      |
| ❌ | `ManagerBase`         | non-abstract                                      |
| ❌ | `IManager`            | is not directly implemented by class OrderManager |
Limitations: inferred bindings include only directly implemented abstractions and exclude special types.
Common pitfalls:
- Expecting inherited interfaces to be included automatically.
- Forgetting that special framework types are intentionally excluded.
See also: [Simplified lifetime-specific bindings](simplified-lifetime-specific-bindings.md), [Special types](simplified-lifetime-specific-bindings.md).

## Composition roots

This example shows several ways to define composition roots as explicit entry points into the graph.
>[!TIP]
>There is no hard limit on roots, but prefer a small number. Ideally, an application has a single composition root.

In classic DI containers, the composition is resolved dynamically via calls like `T Resolve<T>()` or `object GetService(Type type)`. In Pure.DI, each root generates a property or method at compile time, so roots are explicit and discoverable.

```c#
using Pure.DI;

DI.Setup(nameof(Composition))
    .Bind<IInvoiceGenerator>().To<PdfInvoiceGenerator>()
    .Bind<IInvoiceGenerator>("Online").To<HtmlInvoiceGenerator>()
    .Bind<ILogger>().To<FileLogger>()

    // Specifies to create a regular composition root
    // of type "IInvoiceGenerator" with the name "InvoiceGenerator".
    // This will be the main entry point for invoice generation.
    .Root<IInvoiceGenerator>("InvoiceGenerator")

    // Specifies to create an anonymous composition root
    // that is only accessible from "Resolve()" methods.
    // This is useful for auxiliary types or testing.
    .Root<ILogger>()

    // Specifies to create a regular composition root
    // of type "IInvoiceGenerator" with the name "OnlineInvoiceGenerator"
    // using the "Online" tag to differentiate implementations.
    .Root<IInvoiceGenerator>("OnlineInvoiceGenerator", "Online");

var composition = new Composition();

// Resolves the default invoice generator (PDF) with all its dependencies
// invoiceGenerator = new PdfInvoiceGenerator(new FileLogger());
var invoiceGenerator = composition.InvoiceGenerator;

// Resolves the online invoice generator (HTML)
// onlineInvoiceGenerator = new HtmlInvoiceGenerator();
var onlineInvoiceGenerator = composition.OnlineInvoiceGenerator;

// All and only the roots of the composition
// can be obtained by Resolve method.
// Here we resolve the private root 'ILogger'.
var logger = composition.Resolve<ILogger>();

// We can also resolve tagged roots dynamically if needed
var tagged = composition.Resolve<IInvoiceGenerator>("Online");

// Common logger interface used across the system
interface ILogger;

// Concrete implementation of a logger that writes to a file
class FileLogger : ILogger;

// Abstract definition of an invoice generator
interface IInvoiceGenerator;

// Implementation for generating PDF invoices, dependent on ILogger
class PdfInvoiceGenerator(ILogger logger) : IInvoiceGenerator;

// Implementation for generating HTML invoices for online viewing
class HtmlInvoiceGenerator : IInvoiceGenerator;
```

To run the above code, the following NuGet package must be added:
 - [Pure.DI](https://www.nuget.org/packages/Pure.DI)

The name of the composition root is arbitrarily chosen depending on its purpose but should be restricted by the property naming conventions in C# since it is the same name as a property in the composition class. In reality, the `Root` property has the form:
```c#
public IService Root
{
  get
  {
    return new Service(new Dependency());
  }
}
```
To avoid generating _Resolve_ methods just add a comment `// Resolve = Off` before a _Setup_ method:
```c#
// Resolve = Off
DI.Setup("Composition")
  .Bind<IDependency>().To<Dependency>()
  ...
```
This can be done if these methods are not needed, in case only certain composition roots are used. It's not significant then, but it will help save resources during compilation.
Limitations: too many public roots increase composition API surface and make architecture boundaries harder to track.
Common pitfalls:
- Exposing internal services as roots instead of keeping them private.
- Depending on `Resolve` everywhere instead of explicit root members.
See also: [Resolve methods](resolve-methods.md), [Root arguments](root-arguments.md).

## Transient

The `Transient` lifetime specifies to create a new dependency instance each time. It is the default lifetime and can be omitted.

```c#
using Shouldly;
using Pure.DI;
using static Pure.DI.Lifetime;

DI.Setup(nameof(Composition))
    .Bind().As(Transient).To<Buffer>()
    .Bind().To<BatchProcessor>()
    .Root<IBatchProcessor>("Processor");

var composition = new Composition();
var processor = composition.Processor;

// Verify that input and output buffers are different instances.
// This is critical for the batch processor to avoid data corruption
// during reading. The Transient lifetime ensures a new instance
// is created for each dependency injection.
processor.Input.ShouldNotBe(processor.Output);

// Represents a memory buffer that should be unique for each operation
interface IBuffer;

class Buffer : IBuffer;

interface IBatchProcessor
{
    public IBuffer Input { get; }

    public IBuffer Output { get; }
}

class BatchProcessor(
    IBuffer input,
    IBuffer output)
    : IBatchProcessor
{
    public IBuffer Input { get; } = input;

    public IBuffer Output { get; } = output;
}
```

To run the above code, the following NuGet packages must be added:
 - [Pure.DI](https://www.nuget.org/packages/Pure.DI)
 - [Shouldly](https://www.nuget.org/packages/Shouldly)

The `Transient` lifetime is the safest and is used by default. Yes, its widespread use can cause a lot of memory traffic, but if there are doubts about thread safety, the `Transient` lifetime is preferable because each consumer has its own instance of the dependency. The following nuances should be considered when choosing the `Transient` lifetime:

- There will be unnecessary memory overhead that could be avoided.

- Every object created must be disposed of, and this will waste CPU resources, at least when the GC does its memory-clearing job.

- Poorly designed constructors can run slowly, perform functions that are not their own, and greatly hinder the efficient creation of compositions of multiple objects.

>[!IMPORTANT]
>The following very important rule, in my opinion, will help in the last point. Now, when a constructor is used to implement dependencies, it should not be loaded with other tasks. Accordingly, constructors should be free of all logic except for checking arguments and saving them for later use. Following this rule, even the largest compositions of objects will be built quickly.

## Singleton

The `Singleton` lifetime ensures that there will be a single instance of the dependency for each composition.

```c#
using Shouldly;
using Pure.DI;
using System.Diagnostics.CodeAnalysis;
using static Pure.DI.Lifetime;

DI.Setup(nameof(Composition))
    // Bind the cache as Singleton to share it across all services
    .Bind().As(Singleton).To<Cache>()
    // Bind the order service as Transient (default) for per-request instances
    .Bind().To<OrderService>()
    .Root<IOrderService>("OrderService");

var composition = new Composition();
var orderService1 = composition.OrderService; // First order service instance
var orderService2 = composition.OrderService; // Second order service instance

// Verify that both services share the same cache instance (Singleton behavior)
orderService1.Cache.ShouldBe(orderService2.Cache);
// Simulate real-world usage: add data to cache via one service and check via another
orderService1.AddToCache("Order123", "Processed");
orderService2.GetFromCache("Order123").ShouldBe("Processed");

// Interface for a shared cache (e.g., for storing order statuses)
interface ICache
{
    void Add(string key, string value);

    bool TryGet(string key, [MaybeNullWhen(false)] out string value);
}

// Implementation of a simple in-memory cache (must be thread-safe in real apps)
class Cache : ICache
{
    private readonly Dictionary<string, string> _data = new();

    public void Add(string key, string value) =>
        _data[key] = value;

    public bool TryGet(string key, [MaybeNullWhen(false)] out string value) =>
        _data.TryGetValue(key, out value);
}

// Interface for order processing service
interface IOrderService
{
    ICache Cache { get; }

    void AddToCache(string orderId, string status);

    string GetFromCache(string orderId);
}

// Order service that uses the shared cache
class OrderService(ICache cache) : IOrderService
{
    // The cache is injected and shared (Singleton)
    public ICache Cache { get; } = cache;

    // Real-world method: add order status to cache
    public void AddToCache(string orderId, string status) =>
        Cache.Add(orderId, status);

    // Real-world method: retrieve order status from cache
    public string GetFromCache(string orderId) =>
        Cache.TryGet(orderId, out var status) ? status : "unknown";
}
```

To run the above code, the following NuGet packages must be added:
 - [Pure.DI](https://www.nuget.org/packages/Pure.DI)
 - [Shouldly](https://www.nuget.org/packages/Shouldly)

Some articles advise using objects with a `Singleton` lifetime as often as possible, but the following details must be considered:

- For .NET the default behavior is to create a new instance of the type each time it is needed, other behavior requires, additional logic that is not free and requires additional resources.

- The use of `Singleton` adds a requirement for thread-safety controls on their use, since singletons are more likely to share their state between different threads without even realizing it.

- The thread-safety control should be automatically extended to all dependencies that _Singleton_ uses, since their state is also now shared.

- Logic for thread-safety control can be resource-costly, error-prone, interlocking, and difficult to test.

- _Singleton_ can retain dependency references longer than their expected lifetime, this is especially significant for objects that hold "non-renewable" resources, such as the operating system Handler.

- Sometimes additional logic is required to dispose of _Singleton_.

## Scoped

The `Scoped` lifetime ensures that there will be a single instance of the dependency for each scope.

```c#
using Shouldly;
using Pure.DI;
using static Pure.DI.Lifetime;

var composition = new Composition();
var app = composition.AppRoot;

// Real-world analogy:
// each HTTP request (or message consumer handling) creates its own scope.
// Scoped services live exactly as long as the request is being processed.

// Request #1
var request1 = app.CreateRequestScope();
var checkout1 = request1.RequestRoot;

var ctx11 = checkout1.Context;
var ctx12 = checkout1.Context;

// Same request => same scoped instance
ctx11.ShouldBe(ctx12);

// Request #2
var request2 = app.CreateRequestScope();
var checkout2 = request2.RequestRoot;

var ctx2 = checkout2.Context;

// Different request => different scoped instance
ctx11.ShouldNotBe(ctx2);

// End of Request #1 => scoped instance is disposed
request1.Dispose();
ctx11.IsDisposed.ShouldBeTrue();

// End of Request #2 => scoped instance is disposed
request2.Dispose();
ctx2.IsDisposed.ShouldBeTrue();

interface IRequestContext
{
    Guid CorrelationId { get; }

    bool IsDisposed { get; }
}

// Typically: DbContext / UnitOfWork / RequestTelemetry / Activity, etc.
sealed class RequestContext : IRequestContext, IDisposable
{
    public Guid CorrelationId { get; } = Guid.NewGuid();

    public bool IsDisposed { get; private set; }

    public void Dispose() => IsDisposed = true;
}

interface ICheckoutService
{
    IRequestContext Context { get; }
}

// "Controller/service" that participates in request processing.
// It depends on a scoped context (per-request resource).
sealed class CheckoutService(IRequestContext context) : ICheckoutService
{
    public IRequestContext Context => context;
}

// Implements a request scope (per-request composition)
sealed class RequestScope(Composition parent) : Composition(parent);

partial class App(Func<RequestScope> requestScopeFactory)
{
    // In a web app this would roughly map to: "create scope for request"
    public RequestScope CreateRequestScope() => requestScopeFactory();
}

partial class Composition
{
    static void Setup() =>

        DI.Setup()
            // Per-request lifetime
            .Bind().As(Scoped).To<RequestContext>()

            // Regular service that consumes scoped context
            .Bind().To<CheckoutService>()

            // "Request root" (what your controller/handler resolves)
            .Root<ICheckoutService>("RequestRoot")

            // "Application root" (what creates request scopes)
            .Root<App>("AppRoot");
}
```

To run the above code, the following NuGet packages must be added:
 - [Pure.DI](https://www.nuget.org/packages/Pure.DI)
 - [Shouldly](https://www.nuget.org/packages/Shouldly)

>[!NOTE]
>`Scoped` lifetime is essential for request-based or session-based scenarios where instances should be shared within a scope but isolated between scopes.

## Tags

Tags let you control dependency selection when multiple implementations exist:
This is practical for scenarios like public/internal API clients, multiple payment providers, or environment-specific integrations.

```c#
using Shouldly;
using Pure.DI;

DI.Setup(nameof(Composition))
    // The `default` tag is used when the consumer does not specify a tag
    .Bind<IApiClient>("Public", default).To<RestApiClient>()
    .Bind<IApiClient>("Internal").As(Lifetime.Singleton).To<InternalApiClient>()
    .Bind<IApiFacade>().To<ApiFacade>()

    // "InternalRoot" is a root name, "Internal" is a tag
    .Root<IApiClient>("InternalRoot", "Internal")

    // Specifies to create the composition root named "Root"
    .Root<IApiFacade>("Api");

var composition = new Composition();
var api = composition.Api;
api.PublicClient.ShouldBeOfType<RestApiClient>();
api.InternalClient.ShouldBeOfType<InternalApiClient>();
api.InternalClient.ShouldBe(composition.InternalRoot);
api.DefaultClient.ShouldBeOfType<RestApiClient>();

interface IApiClient;

class RestApiClient : IApiClient;

class InternalApiClient : IApiClient;

interface IApiFacade
{
    IApiClient PublicClient { get; }

    IApiClient InternalClient { get; }

    IApiClient DefaultClient { get; }
}

class ApiFacade(
    [Tag("Public")] IApiClient publicClient,
    [Tag("Internal")] IApiClient internalClient,
    IApiClient defaultClient)
    : IApiFacade
{
    public IApiClient PublicClient { get; } = publicClient;

    public IApiClient InternalClient { get; } = internalClient;

    public IApiClient DefaultClient { get; } = defaultClient;
}
```

To run the above code, the following NuGet packages must be added:
 - [Pure.DI](https://www.nuget.org/packages/Pure.DI)
 - [Shouldly](https://www.nuget.org/packages/Shouldly)

The example shows how to:
- Define multiple bindings for the same interface
- Use tags to differentiate between implementations
- Control lifetime management
- Inject tagged dependencies into constructors

The tag can be a constant, a type, a [smart tag](smart-tags.md), or a value of an `Enum` type. The _default_ and _null_ tags are also supported.
Limitations: extensive tag usage can become hard to navigate if naming conventions are inconsistent.
Common pitfalls:
- Using many ad-hoc string tags without central conventions.
- Forgetting to define a `default` tag path for untagged consumers.
See also: [Smart tags](smart-tags.md), [Composition roots](composition-roots.md).

## Factory

Constructor injection covers most cases, but sometimes an instance needs extra work before it is ready to use — like the `Connect()` call here that opens a database connection.
A factory binding `To<T>(ctx => ...)` puts that creation logic under your control: call `ctx.Inject(out var dependency)` to have the container provide dependencies, run any setup code, then return the finished instance.

```c#
using Shouldly;
using Pure.DI;

DI.Setup(nameof(Composition))
    .Bind<IDatabaseService>().To<DatabaseService>(ctx => {
        // Some logic for creating an instance.
        // For example, we need to manually initialize the connection.
        ctx.Inject(out DatabaseService service);
        service.Connect();
        return service;
    })
    .Bind<IUserRegistry>().To<UserRegistry>()

    // Composition root
    .Root<IUserRegistry>("Registry");

var composition = new Composition();
var registry = composition.Registry;
registry.Database.IsConnected.ShouldBeTrue();

interface IDatabaseService
{
    bool IsConnected { get; }
}

class DatabaseService : IDatabaseService
{
    public bool IsConnected { get; private set; }

    // Simulates a connection establishment that must be called explicitly
    public void Connect() => IsConnected = true;
}

interface IUserRegistry
{
    IDatabaseService Database { get; }
}

class UserRegistry(IDatabaseService database) : IUserRegistry
{
    public IDatabaseService Database { get; } = database;
}
```

To run the above code, the following NuGet packages must be added:
 - [Pure.DI](https://www.nuget.org/packages/Pure.DI)
 - [Shouldly](https://www.nuget.org/packages/Shouldly)

There are scenarios where manual control over the creation process is required, such as
- When additional initialization logic is needed
- When complex construction steps are required
- When specific object states need to be set during creation

>[!IMPORTANT]
>The method `Inject()` cannot be used outside of the binding setup.
Limitations: factory bindings introduce custom construction logic that must be maintained and tested.
Common pitfalls:
- Moving business decisions into DI factory code.
- Overusing `Inject()` where normal constructor binding is enough.
See also: [Simplified factory](simplified-factory.md), [Injection on demand](injection-on-demand.md).

## Simplified factory

This example shows a simplified manual factory. Each lambda parameter represents an injected dependency, and starting with C# 10 you can add `Tag(...)` to specify a tagged dependency.

```c#
using Shouldly;
using Pure.DI;

DI.Setup(nameof(Composition))
    .Bind("today").To(() => DateTime.Today)
    // Injects FileLogger and DateTime
    // and applies additional initialization logic
    .Bind<IFileLogger>().To((
        FileLogger logger,
        [Tag("today")] DateTime date) => {
        logger.Init($"app-{date:yyyy-MM-dd}.log");
        return logger;
    })
    .Bind().To<OrderProcessingService>()

    // Composition root
    .Root<IOrderProcessingService>("OrderService");

var composition = new Composition();
var service = composition.OrderService;

service.Logger.FileName.ShouldBe($"app-{DateTime.Today:yyyy-MM-dd}.log");

interface IFileLogger
{
    string FileName { get; }

    void Log(string message);
}

class FileLogger : IFileLogger
{
    public string FileName { get; private set; } = "";

    public void Init(string fileName) => FileName = fileName;

    public void Log(string message)
    {
        // Write to file
    }
}

interface IOrderProcessingService
{
    IFileLogger Logger { get; }
}

class OrderProcessingService(IFileLogger logger) : IOrderProcessingService
{
    public IFileLogger Logger { get; } = logger;
}
```

To run the above code, the following NuGet packages must be added:
 - [Pure.DI](https://www.nuget.org/packages/Pure.DI)
 - [Shouldly](https://www.nuget.org/packages/Shouldly)

The example creates a service that depends on a logger initialized with a date-based file name.
This style keeps the setup concise while still allowing explicit initialization logic.
The `Tag` attribute enables named dependencies for more complex setups.
Limitations: compact lambda factories stay readable only while initialization logic remains small.
Common pitfalls:
- Putting heavy imperative setup code into short lambda factories.
- Forgetting explicit tags when several same-type dependencies exist.
See also: [Factory](factory.md), [Tags](tags.md).

## Injection on demand

This example creates dependencies on demand using a factory delegate. The service (`GameLevel`) needs multiple instances of `IEnemy`, so it receives a `Func<IEnemy>` that can create new instances when needed.
This approach is useful when instances are created lazily or repeatedly during business execution.

```c#
using Shouldly;
using Pure.DI;
using System.Collections.Generic;

DI.Setup(nameof(Composition))
    .Bind().To<Enemy>()
    .Bind().To<GameLevel>()

    // Composition root
    .Root<IGameLevel>("GameLevel");

var composition = new Composition();
var gameLevel = composition.GameLevel;

// Verifies that two distinct enemies have been spawned
gameLevel.Enemies.Count.ShouldBe(2);

// Represents a game entity that acts as an enemy
interface IEnemy;

class Enemy : IEnemy;

// Represents a game level that manages entities
interface IGameLevel
{
    IReadOnlyList<IEnemy> Enemies { get; }
}

class GameLevel(Func<IEnemy> enemySpawner) : IGameLevel
{
    // The factory spawns a fresh enemy instance on each call.
    public IReadOnlyList<IEnemy> Enemies { get; } =
    [
        enemySpawner(),
        enemySpawner()
    ];
}
```

To run the above code, the following NuGet packages must be added:
 - [Pure.DI](https://www.nuget.org/packages/Pure.DI)
 - [Shouldly](https://www.nuget.org/packages/Shouldly)

Key elements:
- `Enemy` is bound to the `IEnemy` interface, and `GameLevel` is bound to `IGameLevel`.
- The `GameLevel` constructor accepts `Func<IEnemy>`, enabling deferred creation of entities.
- The `GameLevel` calls the factory twice, resulting in two distinct `Enemy` instances stored in its `Enemies` collection.

This approach lets factories control lifetime and instantiation timing. Pure.DI resolves a new `IEnemy` each time the factory is invoked.
Limitations: factory delegate calls can create many objects, so lifetime choices still matter for performance and state.
Common pitfalls:
- Assuming `Func<T>` always returns new instances regardless of configured lifetime.
- Hiding expensive work behind repeated on-demand calls.
See also: [Injections on demand with arguments](injections-on-demand-with-arguments.md), [Func<T>](func.md).

## Composition arguments

Use composition arguments when you need to pass state into the composition. Define them with `Arg<T>(string argName)` (optionally with tags) and use them like any other dependency. Only arguments that are used in the object graph become constructor parameters.
This is a clean way to inject external runtime state without global static variables.
>[!NOTE]
>Actually, composition arguments work like normal bindings. The difference is that they bind to the values of the arguments. These values will be injected wherever they are required.


```c#
using Shouldly;
using Pure.DI;

DI.Setup(nameof(Composition))
    .Bind<IBankGateway>().To<BankGateway>()
    .Bind<IPaymentProcessor>().To<PaymentProcessor>()

    // Composition root "PaymentService"
    .Root<IPaymentProcessor>("PaymentService")

    // Composition argument: Connection timeout (e.g., from config)
    .Arg<int>("timeoutSeconds")

    // Composition argument: API Token (using a tag to distinguish from other strings)
    .Arg<string>("authToken", "api token")

    // Composition argument: Bank gateway address
    .Arg<string>("gatewayUrl");

// Create the composition, passing real settings from outside
var composition = new Composition(
    timeoutSeconds: 30,
    authToken: "secret_token_123",
    gatewayUrl: "https://api.bank.com/v1");

var paymentService = composition.PaymentService;

paymentService.Token.ShouldBe("secret_token_123");
paymentService.Gateway.Timeout.ShouldBe(30);
paymentService.Gateway.Url.ShouldBe("https://api.bank.com/v1");

interface IBankGateway
{
    int Timeout { get; }

    string Url { get; }
}

// Simulation of a bank gateway client
class BankGateway(int timeoutSeconds, string gatewayUrl) : IBankGateway
{
    public int Timeout { get; } = timeoutSeconds;

    public string Url { get; } = gatewayUrl;
}

interface IPaymentProcessor
{
    string Token { get; }

    IBankGateway Gateway { get; }
}

// Payment processing service
class PaymentProcessor(
    // The tag allows specifying exactly which string to inject here
    [Tag("api token")] string token,
    IBankGateway gateway) : IPaymentProcessor
{
    public string Token { get; } = token;

    public IBankGateway Gateway { get; } = gateway;
}
```

To run the above code, the following NuGet packages must be added:
 - [Pure.DI](https://www.nuget.org/packages/Pure.DI)
 - [Shouldly](https://www.nuget.org/packages/Shouldly)

>[!NOTE]
>Composition arguments provide a way to inject runtime values into the composition, making your DI configuration more flexible.
Limitations: too many composition arguments can bloat composition constructors and blur configuration boundaries.
Common pitfalls:
- Using untagged primitive arguments where several values of the same type exist.
- Treating composition arguments as mutable runtime state holders.
See also: [Root arguments](root-arguments.md), [Tags](tags.md).

## Root arguments

Use root arguments when you need to pass state into a specific root. Define them with `RootArg<T>(string argName)` (optionally with tags) and use them like any other dependency. A root that uses at least one root argument becomes a method, and only arguments used in that root's object graph appear in the method signature. Use unique argument names to avoid collisions.
Root arguments are useful when runtime values belong to one entry point, not to the whole composition.
>[!NOTE]
>Actually, root arguments work like normal bindings. The difference is that they bind to the values of the arguments. These values will be injected wherever they are required.


```c#
using Shouldly;
using Pure.DI;
using static Pure.DI.Tag;

DI.Setup(nameof(Composition))
    // Disable Resolve methods because root arguments are not compatible
    .Hint(Hint.Resolve, "Off")
    .Bind<IDatabaseService>().To<DatabaseService>()
    .Bind<IApplication>().To<Application>()

    // Root arguments serve as values passed
    // to the composition root method
    .RootArg<int>("port")
    .RootArg<string>("connectionString")

    // An argument can be tagged
    // to be injectable by type and this tag
    .RootArg<string>("appName", AppDetail)

    // Composition root
    .Root<IApplication>("CreateApplication");

var composition = new Composition();

// Creates an application with specific arguments
var app = composition.CreateApplication(
    appName: "MySuperApp",
    port: 8080,
    connectionString: "Server=.;Database=MyDb;");

app.Name.ShouldBe("MySuperApp");
app.Database.Port.ShouldBe(8080);
app.Database.ConnectionString.ShouldBe("Server=.;Database=MyDb;");

interface IDatabaseService
{
    int Port { get; }

    string ConnectionString { get; }
}

class DatabaseService(int port, string connectionString) : IDatabaseService
{
    public int Port { get; } = port;

    public string ConnectionString { get; } = connectionString;
}

interface IApplication
{
    string Name { get; }

    IDatabaseService Database { get; }
}

class Application(
    [Tag(AppDetail)] string name,
    IDatabaseService database)
    : IApplication
{
    public string Name { get; } = name;

    public IDatabaseService Database { get; } = database;
}
```

To run the above code, the following NuGet packages must be added:
 - [Pure.DI](https://www.nuget.org/packages/Pure.DI)
 - [Shouldly](https://www.nuget.org/packages/Shouldly)

When using root arguments, compilation warnings are emitted if `Resolve` methods are generated because these methods cannot create such roots. Disable `Resolve` via `Hint(Hint.Resolve, "Off")`, or ignore the warnings and accept the risks.
Limitations: roots with root arguments become methods and are incompatible with generated `Resolve` methods.
Common pitfalls:
- Reusing ambiguous argument names for different concepts.
- Forgetting to disable or avoid `Resolve` usage in these setups.
See also: [Composition arguments](composition-arguments.md), [Resolve hint](resolve-hint.md).

## Resolve methods

This example shows how to resolve dependencies via generated `Resolve` methods, i.e. through the _Service Locator_ style.
Use this style mainly for integration scenarios; explicit roots are usually cleaner and safer.

```c#
using Pure.DI;

DI.Setup(nameof(Composition))
    .Bind<IDevice>().To<Device>()
    .Bind<ISensor>().To<TemperatureSensor>()
    .Bind<ISensor>("Humidity").To<HumiditySensor>()

    // Specifies to create a private root
    // that is only accessible from _Resolve_ methods
    .Root<ISensor>()

    // Specifies to create a public root named _HumiditySensor_
    // using the "Humidity" tag
    .Root<ISensor>("HumiditySensor", "Humidity");

var composition = new Composition();

// The next 3 lines of code do the same thing:
var sensor1 = composition.Resolve<ISensor>();
var sensor2 = composition.Resolve(typeof(ISensor));
var sensor3 = composition.Resolve(typeof(ISensor), null);

// Resolve by "Humidity" tag
// The next 3 lines of code do the same thing too:
var humiditySensor1 = composition.Resolve<ISensor>("Humidity");
var humiditySensor2 = composition.Resolve(typeof(ISensor), "Humidity");
var humiditySensor3 = composition.HumiditySensor; // Resolve via the public root

interface IDevice;

class Device : IDevice;

interface ISensor;

class TemperatureSensor(IDevice device) : ISensor;

class HumiditySensor : ISensor;
```

To run the above code, the following NuGet package must be added:
 - [Pure.DI](https://www.nuget.org/packages/Pure.DI)

_Resolve_ methods are similar to calling composition roots, which are properties (or methods). Roots are efficient and do not throw, so they are preferred. In contrast, _Resolve_ methods have drawbacks:
- They provide access to an unlimited set of dependencies (_Service Locator_).
- Their use can potentially lead to runtime exceptions. For example, when the corresponding root has not been defined.
- They are awkward for some UI binding scenarios (e.g., MAUI/WPF/Avalonia).
Limitations: `Resolve` is dynamic access to the graph, so it weakens compile-time clarity compared to explicit roots.
Common pitfalls:
- Using `Resolve` as the default access pattern across the codebase.
- Assuming runtime resolve calls are always safe when no matching root exists.
See also: [Composition roots](composition-roots.md), [Resolve hint](resolve-hint.md).

## Injections on demand with arguments

This example uses a parameterized factory so dependencies can be created with runtime arguments. The service creates sensors with specific IDs at instantiation time.
It is a type-safe way to combine DI-managed creation with runtime data.

```c#
using Shouldly;
using Pure.DI;
using System.Collections.Generic;

DI.Setup(nameof(Composition))
    .Bind().To<Sensor>()
    .Bind().To<SmartHome>()

    // Composition root
    .Root<ISmartHome>("SmartHome");

var composition = new Composition();
var smartHome = composition.SmartHome;
var sensors = smartHome.Sensors;

sensors.Count.ShouldBe(2);
sensors[0].Id.ShouldBe(101);
sensors[1].Id.ShouldBe(102);

interface ISensor
{
    int Id { get; }
}

class Sensor(int id) : ISensor
{
    public int Id { get; } = id;
}

interface ISmartHome
{
    IReadOnlyList<ISensor> Sensors { get; }
}

class SmartHome(Func<int, ISensor> sensorFactory) : ISmartHome
{
    public IReadOnlyList<ISensor> Sensors { get; } =
    [
        // Use the injected factory to create a sensor with ID 101
        sensorFactory(101),

        // Create another sensor with ID 102
        sensorFactory(102)
    ];
}
```

To run the above code, the following NuGet packages must be added:
 - [Pure.DI](https://www.nuget.org/packages/Pure.DI)
 - [Shouldly](https://www.nuget.org/packages/Shouldly)

Delayed dependency instantiation:
- Injection of dependencies requiring runtime parameters
- Creation of distinct instances with different configurations
- Type-safe resolution of dependencies with constructor arguments
Limitations: runtime arguments improve flexibility but can increase coupling between call sites and construction signatures.
Common pitfalls:
- Passing infrastructure concerns as runtime arguments instead of normal dependencies.
- Duplicating argument validation logic across consumers.
See also: [Injection on demand](injection-on-demand.md), [Root arguments](root-arguments.md).

## Smart tags

Large object graphs often need many tags. String tags are error-prone and easy to mistype. Prefer `Enum` values as tags, and _Pure.DI_ helps make this safe.
Smart tags improve refactoring safety by moving tag usage into compiler-checked symbols.

When the compiler cannot determine a tag value, _Pure.DI_ generates a constant inside `Pure.DI.Tag`. For the example below, the generated constants would look like this:

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
This enables safe refactoring and compiler-checked tag usage, reducing errors.

![](smart_tags.gif)

The example below also uses the `using static Pure.DI.Tag;` directive to access tags in `Pure.DI.Tag` without specifying a type name:

```c#
using Shouldly;
using Pure.DI;

using static Pure.DI.Tag;
using static Pure.DI.Lifetime;

DI.Setup(nameof(Composition))
    // The `default` tag is used to resolve dependencies
    // when the tag was not specified by the consumer
    .Bind<IMessageSender>(Email, default).To<EmailSender>()
    .Bind<IMessageSender>(Sms).As(Singleton).To<SmsSender>()
    .Bind<IMessagingService>().To<MessagingService>()

    // "SmsSenderRoot" is root name, Sms is tag
    .Root<IMessageSender>("SmsSenderRoot", Sms)

    // Specifies to create the composition root named "Root"
    .Root<IMessagingService>("MessagingService");

var composition = new Composition();
var messagingService = composition.MessagingService;
messagingService.EmailSender.ShouldBeOfType<EmailSender>();
messagingService.SmsSender.ShouldBeOfType<SmsSender>();
messagingService.SmsSender.ShouldBe(composition.SmsSenderRoot);
messagingService.DefaultSender.ShouldBeOfType<EmailSender>();

interface IMessageSender;

class EmailSender : IMessageSender;

class SmsSender : IMessageSender;

interface IMessagingService
{
    IMessageSender EmailSender { get; }

    IMessageSender SmsSender { get; }

    IMessageSender DefaultSender { get; }
}

class MessagingService(
    [Tag(Email)] IMessageSender emailSender,
    [Tag(Sms)] IMessageSender smsSender,
    IMessageSender defaultSender)
    : IMessagingService
{
    public IMessageSender EmailSender { get; } = emailSender;

    public IMessageSender SmsSender { get; } = smsSender;

    public IMessageSender DefaultSender { get; } = defaultSender;
}
```

To run the above code, the following NuGet packages must be added:
 - [Pure.DI](https://www.nuget.org/packages/Pure.DI)
 - [Shouldly](https://www.nuget.org/packages/Shouldly)

>[!NOTE]
>Smart tags provide compile-time safety for tag values, reducing runtime errors and improving code maintainability.
Limitations: smart tags reduce typo risk, but tag policy still needs clear naming and ownership conventions.
Common pitfalls:
- Mixing string literals and smart tags in the same area without a migration plan.
- Treating generated tag constants as domain concepts instead of DI composition details.
See also: [Tags](tags.md), [Generics](generics.md).

## Simplified lifetime-specific bindings

You can use the `Transient<>()`, `Singleton<>()`, `PerResolve<>()`, etc. methods. In this case binding will be performed for the implementation type itself, and if the implementation is not an abstract type or structure, for all abstract but NOT special types that are directly implemented.
This keeps lifetime configuration concise while preserving explicit lifetime semantics.

```c#
using System.Collections;
using Pure.DI;

// Specifies to create a partial class "Composition"
DI.Setup(nameof(Composition))
    // The equivalent of the following:
    // .Bind<IOrderRepository, IOrderNotification, OrderManager>()
    //   .As(Lifetime.PerBlock)
    //   .To<OrderManager>()
    .PerBlock<OrderManager>()
    // The equivalent of the following:
    // .Bind<IShop, Shop>()
    //   .As(Lifetime.Transient)
    //   .To<Shop>()
    // .Bind<IOrderNameFormatter, OrderNameFormatter>()
    //   .As(Lifetime.Transient)
    //   .To<OrderNameFormatter>()
    .Transient<Shop, OrderNameFormatter>()

    // Specifies to create a property "MyShop"
    .Root<IShop>("MyShop");

var composition = new Composition();
var shop = composition.MyShop;

interface IManager;

class ManagerBase : IManager;

interface IOrderRepository;

interface IOrderNotification;

class OrderManager(IOrderNameFormatter orderNameFormatter) :
    ManagerBase,
    IOrderRepository,
    IOrderNotification,
    IDisposable,
    IEnumerable<string>
{
    public void Dispose() {}

    public IEnumerator<string> GetEnumerator() =>
        new List<string>
        {
            orderNameFormatter.Format(1),
            orderNameFormatter.Format(2)
        }.GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}

interface IOrderNameFormatter
{
    string Format(int orderId);
}

class OrderNameFormatter : IOrderNameFormatter
{
    public string Format(int orderId) => $"Order #{orderId}";
}

interface IShop;

class Shop(
    OrderManager manager,
    IOrderRepository repository,
    IOrderNotification notification)
    : IShop;
```

To run the above code, the following NuGet package must be added:
 - [Pure.DI](https://www.nuget.org/packages/Pure.DI)

These methods perform the binding with appropriate lifetime:

- with the implementation type itself
- and if it is NOT an abstract type or structure
  - with all abstract types that it directly implements
  - exceptions are special types

Special types will not be added to bindings:

- `System.Object`
- `System.Enum`
- `System.MulticastDelegate`
- `System.Delegate`
- `System.Collections.IEnumerable`
- `System.Collections.Generic.IEnumerable<T>`
- `System.Collections.Generic.IList<T>`
- `System.Collections.Generic.ICollection<T>`
- `System.Collections.IEnumerator`
- `System.Collections.Generic.IEnumerator<T>`
- `System.Collections.Generic.IReadOnlyList<T>`
- `System.Collections.Generic.IReadOnlyCollection<T>`
- `System.IDisposable`
- `System.IAsyncResult`
- `System.AsyncCallback`

If you want to add your own special type, use the `SpecialType<T>()` call.

For class `OrderManager`, the `PerBlock<OrderManager>()` binding will be equivalent to the `Bind<IOrderRepository, IOrderNotification, OrderManager>().As(Lifetime.PerBlock).To<OrderManager>()` binding. The types `IDisposable`, `IEnumerable<string>` did not get into the binding because they are special from the list above. `ManagerBase` did not get into the binding because it is not abstract. `IManager` is not included because it is not implemented directly by class `OrderManager`.

|    |                       |                                                   |
|----|-----------------------|---------------------------------------------------|
| yes | `OrderManager`        | implementation type itself                        |
| yes | `IOrderRepository`    | directly implements                               |
| yes | `IOrderNotification`  | directly implements                               |
| no  | `IDisposable`         | special type                                      |
| no  | `IEnumerable<string>` | special type                                      |
| no  | `ManagerBase`         | non-abstract                                      |
| no  | `IManager`            | is not directly implemented by class OrderManager |
Limitations: lifetime-specific shortcuts still rely on inferred contracts, so review inferred bindings carefully.
Common pitfalls:
- Applying singleton shortcuts to stateful services without thread-safety guarantees.
- Assuming shortcut APIs bypass special-type exclusion rules.
See also: [Transient](transient.md), [Simplified binding](simplified-binding.md).

## Simplified lifetime-specific factory

Lifetime-named shortcuts such as `Transient(...)` and `Singleton(...)` register a factory and its lifetime in a single call, replacing the longer `Bind().As(...).To(...)` chain.
Overloads accept a plain lambda (optionally with a tag, like `Transient(() => DateTime.Today, "today")`) or a lambda whose parameters are injected dependencies — parameters may carry attributes such as `[Tag]` — so you can initialize the instance before returning it, as `Singleton<FileLogger, DateTime, IFileLogger>` does when setting up the log file name.

```c#
using Shouldly;
using Pure.DI;

DI.Setup(nameof(Composition))
    .Transient(Guid.NewGuid)
    .Transient(() => DateTime.Today, "today")
    // Injects FileLogger and DateTime instances
    // and performs further initialization logic
    // defined in the lambda function to set up the log file name
    .Singleton<FileLogger, DateTime, IFileLogger>((
        logger,
        [Tag("today")] date) => {
        logger.Init($"app-{date:yyyy-MM-dd}.log");
        return logger;
    })
    .Transient<OrderProcessingService>()

    // Composition root
    .Root<IOrderProcessingService>("OrderService");

var composition = new Composition();
var service = composition.OrderService;

service.Logger.FileName.ShouldBe($"app-{DateTime.Today:yyyy-MM-dd}.log");

interface IFileLogger
{
    string FileName { get; }

    void Log(string message);
}

class FileLogger(Func<Guid> idFactory) : IFileLogger
{
    public string FileName { get; private set; } = "";

    public void Init(string fileName) => FileName = fileName;

    public void Log(string message)
    {
        var id = idFactory();
        // Write to file
    }
}

interface IOrderProcessingService
{
    IFileLogger Logger { get; }
}

class OrderProcessingService(IFileLogger logger) : IOrderProcessingService
{
    public IFileLogger Logger { get; } = logger;
}
```

To run the above code, the following NuGet packages must be added:
 - [Pure.DI](https://www.nuget.org/packages/Pure.DI)
 - [Shouldly](https://www.nuget.org/packages/Shouldly)

>[!NOTE]
>Lifetime-specific factories combine the convenience of simplified syntax with explicit lifetime control for optimal performance and correctness.

## Method injection

To use dependency injection for a method, simply add the _Dependency_ (or _Ordinal_) attribute to that method, specifying the sequence number that will be used to define the call to that method:

```c#
using Shouldly;
using Pure.DI;

DI.Setup(nameof(Composition))
    .Bind<IMap>().To<Map>()
    .Bind<INavigator>().To<Navigator>()

    // Composition root
    .Root<INavigator>("Navigator");

var composition = new Composition();
var navigator = composition.Navigator;
navigator.CurrentMap.ShouldBeOfType<Map>();

interface IMap;

class Map : IMap;

interface INavigator
{
    IMap? CurrentMap { get; }
}

class Navigator : INavigator
{
    // The Dependency (or Ordinal) attribute indicates that the method
    // should be called to inject the dependency.
    [Dependency(ordinal: 0)]
    public void LoadMap(IMap map) =>
        CurrentMap = map;

    public IMap? CurrentMap { get; private set; }
}
```

To run the above code, the following NuGet packages must be added:
 - [Pure.DI](https://www.nuget.org/packages/Pure.DI)
 - [Shouldly](https://www.nuget.org/packages/Shouldly)

The key points are:
- The method must be available to be called from a composition class
- The `Dependency` (or `Ordinal`) attribute is used to mark the method for injection
- The DI automatically calls the method to inject dependencies

## Property injection

To use dependency injection on a property, make sure the property is writable and simply add the _Ordinal_ attribute to that property, specifying the ordinal that will be used to determine the injection order:

```c#
using Shouldly;
using Pure.DI;

DI.Setup(nameof(Composition))
    .Bind<ILogger>().To<ConsoleLogger>()
    .Bind<IService>().To<Service>()

    // Composition root
    .Root<IService>("MyService");

var composition = new Composition();
var service = composition.MyService;
service.Logger.ShouldBeOfType<ConsoleLogger>();

interface ILogger;

class ConsoleLogger : ILogger;

interface IService
{
    ILogger? Logger { get; }
}

class Service : IService
{
    // The Dependency attribute specifies to perform an injection,
    // the integer value in the argument specifies
    // the ordinal of injection.
    // Usually, property injection is used for optional dependencies.
    [Dependency] public ILogger? Logger { get; set; }
}
```

To run the above code, the following NuGet packages must be added:
 - [Pure.DI](https://www.nuget.org/packages/Pure.DI)
 - [Shouldly](https://www.nuget.org/packages/Shouldly)

The key points are:
- The property must be writable
- The `Dependency` (or `Ordinal`) attribute is used to mark the property for injection
- The DI automatically injects the dependency when resolving the object graph

## Field injection

To use dependency injection for a field, make sure the field is writable and simply add the _Ordinal_ attribute to that field, specifying an ordinal that will be used to determine the injection order:

```c#
using Shouldly;
using Pure.DI;

DI.Setup(nameof(Composition))
    .Bind<ICoffeeMachine>().To<CoffeeMachine>()
    .Bind<ISmartKitchen>().To<SmartKitchen>()

    // Composition root
    .Root<ISmartKitchen>("Kitchen");

var composition = new Composition();
var kitchen = composition.Kitchen;
kitchen.CoffeeMachine.ShouldBeOfType<CoffeeMachine>();

interface ICoffeeMachine;

class CoffeeMachine : ICoffeeMachine;

interface ISmartKitchen
{
    ICoffeeMachine? CoffeeMachine { get; }
}

class SmartKitchen : ISmartKitchen
{
    // The Dependency attribute specifies to perform an injection.
    // The DI will automatically assign a value to this field
    // when creating the SmartKitchen instance.
    [Dependency]
    public ICoffeeMachine? CoffeeMachineImpl;

    // Expose the injected dependency through a public property
    public ICoffeeMachine? CoffeeMachine => CoffeeMachineImpl;
}
```

To run the above code, the following NuGet packages must be added:
 - [Pure.DI](https://www.nuget.org/packages/Pure.DI)
 - [Shouldly](https://www.nuget.org/packages/Shouldly)

The key points are:
- The field must be writable
- The `Dependency` (or `Ordinal`) attribute is used to mark the field for injection
- The DI automatically injects the dependency when resolving the object graph

## Default values

This example shows how to use default values in dependency injection when explicit injection is not possible.

```c#
using Shouldly;
using Pure.DI;

DI.Setup(nameof(Composition))
    .Bind<ISensor>().To<MotionSensor>()
    .Bind<ISecuritySystem>().To<SecuritySystem>()

    // Composition root
    .Root<ISecuritySystem>("SecuritySystem");

var composition = new Composition();
var securitySystem = composition.SecuritySystem;
securitySystem.Sensor.ShouldBeOfType<MotionSensor>();
securitySystem.SystemName.ShouldBe("Home Guard");

interface ISensor;

class MotionSensor : ISensor;

interface ISecuritySystem
{
    string SystemName { get; }

    ISensor Sensor { get; }
}

// If injection cannot be performed explicitly,
// the default value will be used
class SecuritySystem(string systemName = "Home Guard") : ISecuritySystem
{
    public string SystemName { get; } = systemName;

    // The 'required' modifier ensures that the property is set during initialization.
    // The default value 'new MotionSensor()' provides a fallback
    // if no dependency is injected.
    public required ISensor Sensor { get; init; } = new MotionSensor();
}
```

To run the above code, the following NuGet packages must be added:
 - [Pure.DI](https://www.nuget.org/packages/Pure.DI)
 - [Shouldly](https://www.nuget.org/packages/Shouldly)

The key points are:
- Default constructor arguments can be used for simple values
- The DI will use these defaults if no explicit bindings are provided

This example shows how to handle default values in a dependency injection scenario:
- **Constructor Default Argument**: The `SecuritySystem` class has a constructor with a default value for the name parameter. If no value is provided, "Home Guard" will be used.
- **Required Property with Default**: The `Sensor` property is marked as required but has a default instantiation. This ensures that:
  - The property must be set
  - If no explicit injection occurs, a default value will be used

## Required properties or fields

This example shows how the `required` modifier can be used to automatically inject dependencies into properties and fields. When a property or field is marked with `required`, the DI will automatically inject the dependency without additional effort.

```c#
using Shouldly;
using Pure.DI;

DI.Setup(nameof(Composition))
    .Arg<string>("connectionString")
    .Bind<IDatabase>().To<SqlDatabase>()
    .Bind<IUserRepository>().To<UserRepository>()

    // Composition root
    .Root<IUserRepository>("Repository");

var composition = new Composition(connectionString: "Server=.;Database=MyDb;");
var repository = composition.Repository;

repository.Database.ShouldBeOfType<SqlDatabase>();
repository.ConnectionString.ShouldBe("Server=.;Database=MyDb;");

interface IDatabase;

class SqlDatabase : IDatabase;

interface IUserRepository
{
    string ConnectionString { get; }

    IDatabase Database { get; }
}

class UserRepository : IUserRepository
{
    // The required field will be injected automatically.
    // In this case, it gets the value from the composition argument
    // of type 'string'.
    public required string ConnectionStringField;

    public string ConnectionString => ConnectionStringField;

    // The required property will be injected automatically
    // without additional effort.
    public required IDatabase Database { get; init; }
}
```

To run the above code, the following NuGet packages must be added:
 - [Pure.DI](https://www.nuget.org/packages/Pure.DI)
 - [Shouldly](https://www.nuget.org/packages/Shouldly)

This approach simplifies dependency injection by eliminating the need to manually configure bindings for required dependencies, making the code more concise and easier to maintain.

## Build up of an existing object

This example shows the Build-Up pattern in dependency injection, where an existing object is injected with necessary dependencies through its properties, methods, or fields.

```c#
using Shouldly;
using Pure.DI;

DI.Setup(nameof(Composition))
    .RootArg<string>("name")
    .Bind().To(Guid.NewGuid)
    .Bind().To(ctx => {
        var person = new Person();
        // Injects dependencies into an existing object
        ctx.BuildUp(person);
        return person;
    })
    .Bind().To<Greeter>()

    // Composition root
    .Root<IGreeter>("GetGreeter");

var composition = new Composition();
var greeter = composition.GetGreeter("Nik");

greeter.Person.Name.ShouldBe("Nik");
greeter.Person.Id.ShouldNotBe(Guid.Empty);

interface IPerson
{
    string Name { get; }

    Guid Id { get; }
}

class Person : IPerson
{
    // The Dependency attribute specifies to perform an injection and its order
    [Dependency] public string Name { get; set; } = "";

    public Guid Id { get; private set; } = Guid.Empty;

    // The Dependency attribute specifies to perform an injection and its order
    [Dependency] public void SetId(Guid id) => Id = id;
}

interface IGreeter
{
    IPerson Person { get; }
}

record Greeter(IPerson Person) : IGreeter;
```

To run the above code, the following NuGet packages must be added:
 - [Pure.DI](https://www.nuget.org/packages/Pure.DI)
 - [Shouldly](https://www.nuget.org/packages/Shouldly)

Key Concepts:
**Build-Up** - injecting dependencies into an already created object
**Dependency Attribute** - marker for identifying injectable members

## Builder

Sometimes you need to build up an existing composition root and inject all of its dependencies, in which case the `Builder` method will be useful, as in the example below:

```c#
using Shouldly;
using Pure.DI;

DI.Setup(nameof(Composition))
    .Bind().To(Guid.NewGuid)
    .Bind().To<PhotonBlaster>()
    .Builder<Player>("Equip");

var composition = new Composition();

// The Game Engine instantiates the Player entity,
// so we need to inject dependencies into the existing instance.
var player = composition.Equip(new Player());

player.Id.ShouldNotBe(Guid.Empty);
player.Weapon.ShouldBeOfType<PhotonBlaster>();

interface IWeapon;

class PhotonBlaster : IWeapon;

interface IGameEntity
{
    Guid Id { get; }

    IWeapon? Weapon { get; }
}

record Player : IGameEntity
{
    public Guid Id { get; private set; } = Guid.Empty;

    // The Dependency attribute specifies to perform an injection
    [Dependency]
    public IWeapon? Weapon { get; set; }

    [Dependency]
    public void SetId(Guid id) => Id = id;
}
```

To run the above code, the following NuGet packages must be added:
 - [Pure.DI](https://www.nuget.org/packages/Pure.DI)
 - [Shouldly](https://www.nuget.org/packages/Shouldly)

Important Notes:
- The default builder method name is `BuildUp`
- The first argument to the builder method is always the instance to be built

Advantages:
- Allows working with pre-existing objects
- Provides flexibility in dependency injection
- Supports partial injection of dependencies
- Can be used with legacy code

Use Cases:
- When objects are created outside the DI
- For working with third-party libraries
- When migrating existing code to DI
- For complex object graphs where full construction is not feasible

## Builder with arguments

This example shows how to use builders with custom arguments in dependency injection. It shows how to pass additional parameters during the build-up process.

```c#
using Shouldly;
using Pure.DI;

DI.Setup(nameof(Composition))
    .RootArg<Guid>("id")
    .Bind().To<TelemetrySystem>()
    .Builder<Satellite>("Initialize");

var composition = new Composition();

var id = Guid.NewGuid();
var satellite = composition.Initialize(new Satellite(), id);
satellite.Id.ShouldBe(id);
satellite.Telemetry.ShouldBeOfType<TelemetrySystem>();

interface ITelemetrySystem;

class TelemetrySystem : ITelemetrySystem;

interface ISatellite
{
    Guid Id { get; }

    ITelemetrySystem? Telemetry { get; }
}

record Satellite : ISatellite
{
    public Guid Id { get; private set; } = Guid.Empty;

    // The Dependency attribute specifies to perform an injection
    [Dependency]
    public ITelemetrySystem? Telemetry { get; set; }

    [Dependency]
    public void SetId(Guid id) => Id = id;
}
```

To run the above code, the following NuGet packages must be added:
 - [Pure.DI](https://www.nuget.org/packages/Pure.DI)
 - [Shouldly](https://www.nuget.org/packages/Shouldly)

Important Notes:
- The default builder method name is `BuildUp`
- The first argument to the builder method is always the instance to be built
- Additional arguments are passed in the order they are defined in the setup
- Root arguments can be used to provide custom values during build-up

Use Cases:
- When additional parameters are required during object construction
- For scenarios where dependencies depend on runtime values
- When specific initialization data is needed
- For conditional injection based on provided arguments

Best Practices
- Keep the number of builder arguments minimal
- Use meaningful names for root arguments

## Builders

Sometimes you need builders for all types derived from `T` that are known at compile time.

```c#
using Shouldly;
using Pure.DI;

DI.Setup(nameof(Composition))
    .Bind().To(Guid.NewGuid)
    .Bind().To<PlutoniumBattery>()
    // Creates a builder for each type inherited from IRobot.
    // These types must be available at this point in the code.
    .Builders<IRobot>("BuildUp", filter: "*Bot");

var composition = new Composition();

var cleaner = composition.BuildUp(new CleanerBot());
cleaner.Token.ShouldNotBe(Guid.Empty);
cleaner.Battery.ShouldBeOfType<PlutoniumBattery>();

var guard = composition.BuildUp(new GuardBot());
guard.Token.ShouldBe(Guid.Empty);
guard.Battery.ShouldBeOfType<PlutoniumBattery>();

// Uses a common method to build an instance
IRobot robot = new CleanerBot();
robot = composition.BuildUp(robot);
robot.ShouldBeOfType<CleanerBot>();
robot.Token.ShouldNotBe(Guid.Empty);
robot.Battery.ShouldBeOfType<PlutoniumBattery>();

// Uses a safe common method when the runtime subtype may be unknown.
var externalRobot = new ExternalRobot();
composition.TryBuildUp(externalRobot).ShouldBeFalse();
externalRobot.Battery.ShouldBeNull();

// The strict builder still throws for unknown runtime subtypes.
Should.Throw<ArgumentException>(() => composition.BuildUp(externalRobot));

interface IBattery;

class PlutoniumBattery : IBattery;

interface IRobot
{
    Guid Token { get; }

    IBattery? Battery { get; }
}

record CleanerBot : IRobot
{
    public Guid Token { get; private set; } = Guid.Empty;

    // The Dependency attribute specifies to perform an injection
    [Dependency]
    public IBattery? Battery { get; set; }

    [Dependency]
    public void SetToken(Guid token) => Token = token;
}

record GuardBot : IRobot
{
    public Guid Token => Guid.Empty;

    [Dependency]
    public IBattery? Battery { get; set; }
}

record ExternalRobot : IRobot
{
    public Guid Token => Guid.Empty;

    public IBattery? Battery => null;
}
```

To run the above code, the following NuGet packages must be added:
 - [Pure.DI](https://www.nuget.org/packages/Pure.DI)
 - [Shouldly](https://www.nuget.org/packages/Shouldly)

Important Notes:
- The default builder method name is `BuildUp`
- The first argument to the builder method is always the instance to be built
- `Builders<T>` also generates `TryBuildUp` for safe build-up when the runtime subtype may be unknown

## Builders with a name template

Sometimes you need to build up an existing composition root and inject all of its dependencies, in which case the `Builder` method will be useful, as in the example below:

```c#
using Shouldly;
using Pure.DI;

DI.Setup(nameof(Composition))
    .Bind().To(Guid.NewGuid)
    .Bind().To<WiFi>()
    // Creates a builder based on the name template
    // for each type inherited from IDevice.
    // These types must be available at this point in the code.
    .Builders<IDevice>("Install{type}");

var composition = new Composition();

var webcam = composition.InstallWebcam(new Webcam());
webcam.Id.ShouldNotBe(Guid.Empty);
webcam.Network.ShouldBeOfType<WiFi>();

var thermostat = composition.InstallThermostat(new Thermostat());
thermostat.Id.ShouldBe(Guid.Empty);
thermostat.Network.ShouldBeOfType<WiFi>();

// Uses a common method to build an instance
IDevice device = new Webcam();
device = composition.InstallIDevice(device);
device.ShouldBeOfType<Webcam>();
device.Id.ShouldNotBe(Guid.Empty);
device.Network.ShouldBeOfType<WiFi>();

interface INetwork;

class WiFi : INetwork;

interface IDevice
{
    Guid Id { get; }

    INetwork? Network { get; }
}

record Webcam : IDevice
{
    public Guid Id { get; private set; } = Guid.Empty;

    // The Dependency attribute specifies to perform an injection
    [Dependency]
    public INetwork? Network { get; set; }

    [Dependency]
    public void SetId(Guid id) => Id = id;
}

record Thermostat : IDevice
{
    public Guid Id => Guid.Empty;

    // The Dependency attribute specifies to perform an injection
    [Dependency]
    public INetwork? Network { get; set; }
}
```

To run the above code, the following NuGet packages must be added:
 - [Pure.DI](https://www.nuget.org/packages/Pure.DI)
 - [Shouldly](https://www.nuget.org/packages/Shouldly)

The default builder method name is `BuildUp`. The first argument to this method will always be the instance to be built.

## Overrides

This example shows advanced dependency injection techniques using Pure.DI's override mechanism to customize dependency instantiation with runtime arguments and tagged parameters. The implementation creates multiple `IDependency` instances with values manipulated through explicit overrides.

```c#
using Shouldly;
using Pure.DI;
using System.Collections.Immutable;
using System.Drawing;

DI.Setup(nameof(Composition))
    .Bind(Tag.Red).To(() => Color.Red)
    .Bind().As(Lifetime.Singleton).To<Clock>()
    // The factory accepts the widget ID and the layer index
    .Bind().To<Func<int, int, IWidget>>(ctx =>
        (widgetId, layerIndex) => {
            // Overrides the 'id' argument of the constructor with the first lambda argument
            ctx.Override(widgetId);

            // Overrides the 'layer' tagged argument of the constructor with the second lambda argument
            ctx.Override(layerIndex, "layer");

            // Overrides the 'name' argument with a formatted string
            ctx.Override($"Widget {widgetId} on layer {layerIndex}");

            // Resolves the 'Color' dependency tagged with 'Red'
            ctx.Inject(Tag.Red, out Color color);
            // Overrides the 'color' argument with the resolved value
            ctx.Override(color);

            // Creates the instance using the overridden values
            ctx.Inject<Widget>(out var widget);
            return widget;
        })
    .Bind().To<Dashboard>()

    // Composition root
    .Root<IDashboard>("Dashboard");

var composition = new Composition();
var dashboard = composition.Dashboard;
dashboard.Widgets.Length.ShouldBe(3);

dashboard.Widgets[0].Id.ShouldBe(0);
dashboard.Widgets[0].Layer.ShouldBe(99);
dashboard.Widgets[0].Name.ShouldBe("Widget 0 on layer 99");

dashboard.Widgets[1].Id.ShouldBe(1);
dashboard.Widgets[1].Name.ShouldBe("Widget 1 on layer 99");

dashboard.Widgets[2].Id.ShouldBe(2);
dashboard.Widgets[2].Name.ShouldBe("Widget 2 on layer 99");

interface IClock
{
    DateTimeOffset Now { get; }
}

class Clock : IClock
{
    public DateTimeOffset Now => DateTimeOffset.Now;
}

interface IWidget
{
    string Name { get; }

    int Id { get; }

    int Layer { get; }
}

class Widget(
    string name,
    IClock clock,
    int id,
    [Tag("layer")] int layer,
    Color color)
    : IWidget
{
    public string Name => name;

    public int Id => id;

    public int Layer => layer;
}

interface IDashboard
{
    ImmutableArray<IWidget> Widgets { get; }
}

class Dashboard(Func<int, int, IWidget> widgetFactory) : IDashboard
{
    public ImmutableArray<IWidget> Widgets { get; } =
    [
        widgetFactory(0, 99),
        widgetFactory(1, 99),
        widgetFactory(2, 99)
    ];
}
```

To run the above code, the following NuGet packages must be added:
 - [Pure.DI](https://www.nuget.org/packages/Pure.DI)
 - [Shouldly](https://www.nuget.org/packages/Shouldly)

>[!NOTE]
>Overrides provide fine-grained control over dependency resolution, allowing you to customize bindings at runtime or for specific scenarios.

## Nullable reference types

Pure.DI preserves nullable reference type annotations when it reads dependency contracts, builds the graph, and generates composition members.
Use nullable dependencies for values that are allowed to be absent. A nullable root or composition argument does not get a generated null check, while a non-null reference argument still does.
A non-null binding can satisfy a nullable dependency request. This is useful for optional constructor parameters, nullable factory results, and nullable collection elements.
>[!TIP]
>`T?` means that the consumer can handle `null`; it does not mean that a missing binding is ignored. If no binding or auto-binding can provide the type, Pure.DI still reports the graph error.
>[!NOTE]
>When a nullable reference type is used as a generic argument, the generic type must allow nullable arguments. For example, prefer `where T : class?` over `where T : class` for contracts such as `IBox<string?>`; otherwise the C# compiler reports a nullable constraint warning before Pure.DI analyzes the graph.

```c#
using Shouldly;
using Pure.DI;
using System.Collections.Generic;
using System.Linq;

DI.Setup(nameof(Composition))
    .Hint(Hint.Resolve, "Off")
    .Bind<IDatabase>().To<Database>()
    .Bind<IReportService>().To<ReportService>()

    // Nullable composition argument: no generated null check
    .Arg<string?>("defaultTitle", "title")

    // Nullable root argument: no generated null check
    .RootArg<string?>("connectionString", "connection")

    // Composition root
    .Root<IReportService>("CreateReportService");

var composition = new Composition(defaultTitle: null);
var reportService = composition.CreateReportService(connectionString: null);

reportService.DefaultTitle.ShouldBeNull();
reportService.ConnectionString.ShouldBeNull();
reportService.OptionalDatabase.ShouldNotBeNull();
reportService.Databases.Count.ShouldBe(1);

interface IDatabase;

class Database : IDatabase;

interface IReportService
{
    string? DefaultTitle { get; }

    string? ConnectionString { get; }

    IDatabase? OptionalDatabase { get; }

    IReadOnlyList<IDatabase?> Databases { get; }
}

class ReportService(
    [Tag("title")] string? defaultTitle,
    [Tag("connection")] string? connectionString,
    IDatabase? optionalDatabase,
    IEnumerable<IDatabase?> databases)
    : IReportService
{
    public string? DefaultTitle { get; } = defaultTitle;

    public string? ConnectionString { get; } = connectionString;

    public IDatabase? OptionalDatabase { get; } = optionalDatabase;

    public IReadOnlyList<IDatabase?> Databases { get; } = databases.ToList();
}
```

To run the above code, the following NuGet packages must be added:
 - [Pure.DI](https://www.nuget.org/packages/Pure.DI)
 - [Shouldly](https://www.nuget.org/packages/Shouldly)

Limitations: nullable annotations describe compile-time contracts. They are not runtime validation rules and do not replace explicit domain validation.
Common pitfalls:
- Using `T?` to hide a missing binding instead of modelling an optional value.
- Forgetting tags for nullable primitive values when several values of the same type exist.
- Assuming `IEnumerable<T?>` changes the lifetime of elements; lifetime still comes from the matched bindings.
- Declaring generic contracts with `where T : class` and then consuming them as `T?`; use a nullable-aware constraint such as `where T : class?` when nullable generic arguments are valid.
See also: [Composition arguments](composition-arguments.md), [Root arguments](root-arguments.md), [Injection on demand](injection-on-demand.md).

## Root binding

In general, it is recommended to define one composition root for the entire application. But Sometimes you need to have multiple roots. To simplify the definition of composition roots, a "hybrid" API method `RootBind<T>(string rootName)` was added. It lets you define a binding and at the same time the root of the composition. You can it in order to reduce repetitions. The registration `composition.RootBind<IDependency>().To<Dependency>()` is an equivalent to `composition.Bind<IDependency>().To<Dependency>().Root<IDependency>()`.

```c#
using Shouldly;
using Pure.DI;

DI.Setup(nameof(Composition))
    .Bind().As(Lifetime.Singleton).To<DbConnection>()
    // RootBind lets you define a binding and a composition root
    // simultaneously. This is useful for creating public entry points
    // for your application components while keeping the configuration concise.
    .RootBind<IOrderService>("OrderService").To<OrderService>();

// The line above is functionally equivalent to:
//  .Bind<IOrderService>().To<OrderService>()
//  .Root<IOrderService>("OrderService")

var composition = new Composition();
var orderService = composition.OrderService;
orderService.ShouldBeOfType<OrderService>();

interface IDbConnection;

class DbConnection : IDbConnection;

interface IOrderService;

class OrderService(IDbConnection connection) : IOrderService;
```

To run the above code, the following NuGet packages must be added:
 - [Pure.DI](https://www.nuget.org/packages/Pure.DI)
 - [Shouldly](https://www.nuget.org/packages/Shouldly)

>[!NOTE]
>`RootBind` reduces boilerplate when you need both a binding and a root for the same type.

## Static root

Passing `kind: RootKinds.Static` to `Root<T>(...)` makes the generated root a static member, so an instance can be obtained directly from the composition type — `Composition.GlobalConfiguration` — without creating a composition object.
This is useful for stateless entry-point services such as static configuration readers, validators, or one-shot command helpers where the composition itself does not carry state.

```c#
using Shouldly;
using Pure.DI;

DI.Setup(nameof(Composition))
    .Bind().As(Lifetime.PerResolve).To<FileSystem>()
    .Bind().To<Configuration>()
    .Root<IConfiguration>("GlobalConfiguration", kind: RootKinds.Static);

var configuration = Composition.GlobalConfiguration;
configuration.ShouldBeOfType<Configuration>();

interface IFileSystem;

class FileSystem : IFileSystem;

interface IConfiguration;

class Configuration(IFileSystem fileSystem) : IConfiguration;
```

To run the above code, the following NuGet packages must be added:
 - [Pure.DI](https://www.nuget.org/packages/Pure.DI)
 - [Shouldly](https://www.nuget.org/packages/Shouldly)

>[!NOTE]
>Static roots keep the call site compact and avoid allocating a composition instance for graphs that do not need composition-level state.
Avoid static roots for graphs that depend on scoped state, per-composition caches, or externally supplied constructor arguments. In those cases, an instance composition keeps ownership and lifetime boundaries clearer.

## Async Root

A composition root can be asynchronous: declare it as `Root<Task<IService>>(...)` and _Pure.DI_ generates a root method you can `await`.
This is useful when building the object graph is costly and you don't want to block the caller.
Add `RootArg<CancellationToken>("cancellationToken")` to pass a cancellation token that is used when resolving the root.

```c#
using Shouldly;
using Pure.DI;

DI.Setup(nameof(Composition))
    .Bind<IFileStore>().To<FileStore>()
    .Bind<IBackupService>().To<BackupService>()

    // Specifies to use CancellationToken from the argument
    // when resolving a composition root
    .RootArg<CancellationToken>("cancellationToken")

    // Composition root
    .Root<Task<IBackupService>>("GetBackupServiceAsync");

var composition = new Composition();

// Resolves composition roots asynchronously
var service = await composition.GetBackupServiceAsync(CancellationToken.None);

interface IFileStore;

class FileStore : IFileStore;

interface IBackupService;

class BackupService(IFileStore fileStore) : IBackupService;
```

To run the above code, the following NuGet packages must be added:
 - [Pure.DI](https://www.nuget.org/packages/Pure.DI)
 - [Shouldly](https://www.nuget.org/packages/Shouldly)

>[!NOTE]
>Async roots are useful when you need to perform asynchronous initialization or when your services require async creation.

## Roots

Sometimes you need roots for all types inherited from <see cref="T"/> available at compile time at the point where the method is called.

```c#
using Shouldly;
using Pure.DI;

DI.Setup(nameof(Composition))
    .Bind().As(Lifetime.Singleton).To<Preferences>()
    // Roots can be used to register all descendants of a type as roots.
    .Roots<IWindow>("{type}");

var composition = new Composition();
composition.MainWindow.ShouldBeOfType<MainWindow>();
composition.SettingsWindow.ShouldBeOfType<SettingsWindow>();

interface IPreferences;

class Preferences : IPreferences;

interface IWindow;

class MainWindow(IPreferences preferences) : IWindow;

class SettingsWindow(IPreferences preferences) : IWindow;
```

To run the above code, the following NuGet packages must be added:
 - [Pure.DI](https://www.nuget.org/packages/Pure.DI)
 - [Shouldly](https://www.nuget.org/packages/Shouldly)

>[!NOTE]
>This feature is useful for plugin-style architectures where you need to expose all implementations of a base type or interface.

## Roots with filter

`Roots<T>(name, filter)` creates a composition root for every implementation of `T` whose type name matches a wildcard filter, with `{type}` in the name template replaced by each type's name.
Filtering matters when some implementations should not be exposed: here `filter: "*Email*"` picks up `EmailService` but skips `SmsService`, whose `string apiKey` dependency has no binding and therefore cannot be resolved.

```c#
using Shouldly;
using Pure.DI;

DI.Setup(nameof(Composition))
    .Bind().As(Lifetime.Singleton).To<Configuration>()
    .Roots<INotificationService>("My{type}", filter: "*Email*");

var composition = new Composition();
composition.MyEmailService.ShouldBeOfType<EmailService>();

interface IConfiguration;

class Configuration : IConfiguration;

interface INotificationService;

// This service requires an API key which is not bound,
// so it cannot be resolved and should be filtered out.
class SmsService(string apiKey) : INotificationService;

class EmailService(IConfiguration config) : INotificationService;
```

To run the above code, the following NuGet packages must be added:
 - [Pure.DI](https://www.nuget.org/packages/Pure.DI)
 - [Shouldly](https://www.nuget.org/packages/Shouldly)

>[!NOTE]
>Filtering roots provides fine-grained control over which implementations are exposed, useful for conditional feature activation.

## Consumer type

`ConsumerType` is used to get the consumer type of the given dependency. The use of `ConsumerType` is demonstrated on the example of [Serilog library](https://serilog.net/):

```c#
using Shouldly;
using Serilog.Core;
using Serilog.Events;
using Pure.DI;
using Serilog.Core;

Serilog.ILogger serilogLogger = new Serilog.LoggerConfiguration().CreateLogger();
var composition = new Composition(logger: serilogLogger);
var service = composition.Root;

interface IDependency;

class Dependency : IDependency
{
    public Dependency(Serilog.ILogger log)
    {
        log.Information("Dependency created");
    }
}

interface IService
{
    IDependency Dependency { get; }
}

class Service : IService
{
    public Service(
        Serilog.ILogger log,
        IDependency dependency)
    {
        Dependency = dependency;
        log.Information("Service initialized");
    }

    public IDependency Dependency { get; }
}

partial class Composition
{
    private void Setup() =>

        DI.Setup(nameof(Composition))
            .Arg<Serilog.ILogger>("logger", "from arg")
            .Bind().To(ctx => {
                ctx.Inject<Serilog.ILogger>("from arg", out var logger);

                // Enriches the logger with the specific context of the consumer type.
                // ctx.ConsumerType represents the type into which the dependency is being injected.
                // This allows logs to be tagged with the name of the class that created them.
                return logger.ForContext(ctx.ConsumerType);
            })
            .Bind().To<Dependency>()
            .Bind().To<Service>()
            .Root<IService>(nameof(Root));
}
```

To run the above code, the following NuGet packages must be added:
 - [Pure.DI](https://www.nuget.org/packages/Pure.DI)
 - [Shouldly](https://www.nuget.org/packages/Shouldly)
 - [Serilog.Core](https://www.nuget.org/packages/Serilog.Core)
 - [Serilog.Events](https://www.nuget.org/packages/Serilog.Events)

>[!NOTE]
>ConsumerType is useful for creating context-aware loggers or when you need to know which type is consuming a dependency.

## Ref dependencies

High-performance code often relies on `ref struct` types such as `Span<T>`, which cannot be stored in fields — so ordinary constructor or property injection is off the table.
Instead, inject them by `ref` through a method marked with `[Ordinal]`: here a `ref struct Data` wrapping the bound `int[]` is passed into `Initialize(ref Data data)` and consumed without extra allocations.

```c#
using Shouldly;
using Pure.DI;

DI.Setup("Composition")
    // Represents a large data set or buffer
    .Bind().To<int[]>(() => [10, 20, 30])
    .Root<Service>("MyService");

var composition = new Composition();
var service = composition.MyService;
service.Sum.ShouldBe(60);

class Service
{
    public int Sum { get; private set; }

    // Ref structs cannot be fields, so they are injected via a method
    // with the [Ordinal] attribute. This allows working with
    // high-performance types like Span<T> or other ref structs.
    [Ordinal]
    public void Initialize(ref Data data) =>
        Sum = data.Sum();
}

// A ref struct that holds a reference to the data
// to process it without additional memory allocations
readonly ref struct Data(ref int[] data)
{
    private readonly ref int[] _dep = ref data;

    public int Sum() => _dep.Sum();
}
```

To run the above code, the following NuGet packages must be added:
 - [Pure.DI](https://www.nuget.org/packages/Pure.DI)
 - [Shouldly](https://www.nuget.org/packages/Shouldly)

>[!NOTE]
>`ref` injection through an `[Ordinal]` method lets dependencies use stack-only types like `Span<T>` and avoids copying large structs.

## Union types

A union types C# feature allows declaring a union type such as `union PaymentGateway(StripeGateway, BankGateway);`. Each case type is implicitly convertible to the union, and Pure.DI uses this conversion to treat a case implementation as an implementation of the union DI contract. Unlike an interface contract, the case types do not have to share any common abstraction — they may even be third-party SDK types you cannot change. The checkout service below depends only on the `PaymentGateway` union and pattern matches over its cases, while the composition decides which gateway is used and builds the whole dependency graph of the selected case.

```c#
using Shouldly;
using Pure.DI;

DI.Setup(nameof(Composition))
    .Bind<IReceiptService>().To<ReceiptService>()

    // The composition decides which payment gateway
    // stands behind the union contract
    .Bind<PaymentGateway>().To<StripeGateway>()

    // Composition root
    .Root<CheckoutService>("Checkout");

var composition = new Composition();
var checkout = composition.Checkout;
checkout.Pay(4999).ShouldBe("Receipt: stripe/api charge 4999");

// The case types are independent classes, possibly from
// different vendor SDKs, and share no common interface.
// Each of them has its own dependencies that the composition
// builds behind the union contract.
class StripeApiClient
{
    public string Post(string request) => $"stripe/api {request}";
}

class StripeGateway(StripeApiClient api)
{
    public string Charge(int amountInCents) =>
        api.Post($"charge {amountInCents}");
}

class BankAccount
{
    public string Iban => "DE00 1234 5678";
}

class BankGateway(BankAccount account)
{
    public string Transfer(int amountInCents) =>
        $"bank transfer {amountInCents} from {account.Iban}";
}

// A union type: its case types are implicitly convertible
// to the union, and Pure.DI uses this conversion
// to satisfy the union contract
union PaymentGateway(StripeGateway, BankGateway);

interface IReceiptService
{
    string Print(string confirmation);
}

class ReceiptService : IReceiptService
{
    public string Print(string confirmation) => $"Receipt: {confirmation}";
}

// The service depends on the union contract instead of
// a specific gateway and handles each case explicitly
class CheckoutService(PaymentGateway gateway, IReceiptService receipts)
{
    public string Pay(int amountInCents)
    {
        var confirmation = gateway switch
        {
            StripeGateway stripe => stripe.Charge(amountInCents),
            BankGateway bank => bank.Transfer(amountInCents),
            _ => throw new InvalidOperationException("Unknown payment gateway.")
        };

        return receipts.Print(confirmation);
    }
}
```

To run the above code, the following NuGet packages must be added:
 - [Pure.DI](https://www.nuget.org/packages/Pure.DI)
 - [Shouldly](https://www.nuget.org/packages/Shouldly)

Swapping the payment provider is a one-line change in the setup: `.Bind<PaymentGateway>().To<BankGateway>()` makes the same checkout root transfer money through the bank gateway together with its own dependencies.
If there is no explicit union binding and exactly one registered binding can be implicitly converted to the requested union, Pure.DI resolves the union through that single case automatically:
```c#
DI.Setup(nameof(Composition))
    .Bind<StripeGateway>().To<StripeGateway>()
    .Root<PaymentGateway>("Gateway");
```
When several case bindings are applicable to the same union contract and tag, Pure.DI reports error `DIE050` instead of picking a case arbitrarily. Bind the union contract explicitly, use distinct tags, or remove one of the candidate bindings.
Composition arguments and root arguments can also provide a case value. For example, `.Arg<StripeGateway>("gateway")` can satisfy a `PaymentGateway` dependency, while `.RootArg<StripeGateway>("gateway")` produces a root method that converts the supplied gateway on each call.
Generic unions are supported in bindings, factories, and both closed and generic roots. Pure.DI substitutes the requested type arguments and asks Roslyn whether the resulting case type converts to the closed union. Generic constraints are preserved on generated roots.
On-demand factories work for `Func<PaymentGateway>`, `Func<StripeGateway, PaymentGateway>`, and custom delegates. Delegate arguments are normal Pure.DI overrides: the case value is converted inside the delegate invocation and is not captured by the composition.
The case binding remains the owner of its lifetime and disposal behavior. `Singleton`, `Scoped`, and `PerResolve` are not replaced by the transient conversion node. A disposable singleton case is disposed exactly once by the composition, including when it is reached through a delegate or a collection. A disposable case supplied as a delegate argument is an external override and remains owned by the caller.
Direct nested unions are supported when an inner union is the actual source contract, for example `Bind<CardPayment>().To<Visa>()` followed by a `Payment` root whose case is `CardPayment`. Pure.DI materializes `CardPayment` before converting it to `Payment`. C# deliberately forbids chaining `Visa -> CardPayment -> Payment` when only a `Visa` binding exists, and Pure.DI reports the unresolved outer contract.
Collections deliberately keep the normal Pure.DI multi-binding rules. Register case bindings with `Tag.Unique` and request `IEnumerable<PaymentGateway>`, `PaymentGateway[]`, `List<PaymentGateway>`, `IReadOnlyList<PaymentGateway>`, immutable collections, `ReadOnlySpan<PaymentGateway>`, or `IAsyncEnumerable<PaymentGateway>` to receive every registered case converted to the union. Generic union elements and their tags are handled in the same way. A tag on the collection injection selects that collection dependency but does not filter its elements, matching the established Pure.DI collection semantics. A single `PaymentGateway` request with several matching cases remains ambiguous and reports `DIE050`.
Pure.DI follows Roslyn's standard-conversion rules before a case-to-union conversion, including derived-to-base, interface, boxing, and nullable conversions. User-defined conversion operators still shadow union conversions, while union conversions are never chained.
The preview specification also defines custom union member providers through a nested public `IUnionMembers` interface with static `Create` methods. Pure.DI keeps provider handling compiler-driven: Roslyn 5.6 does not yet classify these provider conversions as union conversions, so Pure.DI does not emulate them. The integration suite contains pending positive scenarios for generic and `in`-parameter providers, plus an active compiler-level guard that will signal when Roslyn support becomes available.
For `DIE050`, every candidate is shown with its case type, effective lifetime, and binding expression; related locations point to the ambiguous root or injection and to each candidate binding.
The generated code stays statically typed: the case instance is converted to the union by the C# compiler at the injection site, so lifetimes of case bindings remain visible to lifetime validation.
This scenario requires the preview language version and .NET 11 Preview 5 or later, where the union runtime types are available.

## PerResolve

The `PerResolve` lifetime ensures that there will be one instance of the dependency for each composition root instance.

```c#
using Shouldly;
using Pure.DI;
using static Pure.DI.Lifetime;

DI.Setup(nameof(Composition))
    // PerResolve = one "planning session" per root access.
    // Imagine: each time you ask for a plan, you get a fresh context.
    .Bind().As(PerResolve).To<RoutePlanningSession>()

    // Singleton = created once per Composition instance.
    // Here it intentionally captures session when it's created the first time
    // (this is a realistic pitfall: singleton accidentally holds request-scoped state).
    .Bind().As(Singleton).To<(IRoutePlanningSession s3, IRoutePlanningSession s4)>()

    // Composition root
    .Root<TrainTripPlanner>("Planner");

var composition = new Composition();

// First "user request": plan a trip now
var plan1 = composition.Planner;

// In the same request, PerResolve dependencies are the same instance:
plan1.SessionForOutbound.ShouldBe(plan1.SessionForReturn);

// Tuple is Singleton, so both entries are the same captured instance:
plan1.CapturedSessionA.ShouldBe(plan1.CapturedSessionB);

// Because the singleton tuple was created during the first request,
// it captured THAT request's PerResolve session:
plan1.SessionForOutbound.ShouldBe(plan1.CapturedSessionA);

// Second "user request": plan another trip (new root access)
var plan2 = composition.Planner;

// New request => new PerResolve session:
plan2.SessionForOutbound.ShouldNotBe(plan1.SessionForOutbound);

// But the singleton still holds the old captured session from the first request:
plan2.CapturedSessionA.ShouldBe(plan1.CapturedSessionA);
plan2.SessionForOutbound.ShouldNotBe(plan2.CapturedSessionA);

// A request-scoped context: e.g., contains "now", locale, pricing rules version,
// feature flags, etc. You typically want a new one per route planning request.
interface IRoutePlanningSession;

class RoutePlanningSession : IRoutePlanningSession;

// A service that plans a train trip.
// It asks for two session instances to demonstrate PerResolve:
// both should be the same within a single request.
class TrainTripPlanner(
    IRoutePlanningSession sessionForOutbound,
    IRoutePlanningSession sessionForReturn,
    (IRoutePlanningSession capturedA, IRoutePlanningSession capturedB) capturedSessions)
{
    public IRoutePlanningSession SessionForOutbound { get; } = sessionForOutbound;

    public IRoutePlanningSession SessionForReturn { get; } = sessionForReturn;

    // These come from a singleton tuple - effectively "global cached" instances.
    public IRoutePlanningSession CapturedSessionA { get; } = capturedSessions.capturedA;

    public IRoutePlanningSession CapturedSessionB { get; } = capturedSessions.capturedB;
}
```

To run the above code, the following NuGet packages must be added:
 - [Pure.DI](https://www.nuget.org/packages/Pure.DI)
 - [Shouldly](https://www.nuget.org/packages/Shouldly)

>[!NOTE]
>`PerResolve` lifetime is useful when you want to share a dependency instance within a single composition root resolution.

## PerBlock

The `PerBlock` lifetime reuses an instance inside a generated construction block. It is useful when several constructor parameters in the same object graph need the same expensive helper, but keeping that helper for the whole root (`PerResolve`) or composition (`Singleton`) would be too broad.
The order repository below receives the same database connection for both primary and secondary constructor paths within one block, then receives a fresh connection for the next root call. This reduces duplicate construction while keeping request-like operations isolated.

```c#
using Shouldly;
using Pure.DI;
using static Pure.DI.Lifetime;

DI.Setup(nameof(Composition))
    // Bind DatabaseConnection with PerBlock lifetime:
    // Ensures a single connection per composition root (e.g., per user request),
    // but a new one for each new root - useful for batch operations without full singleton overhead.
    .Bind().As(PerBlock).To<DatabaseConnection>()
    // Bind a tuple of two connections as Singleton:
    // This shares the same connection globally, simulating a cached or shared resource.
    .Bind().As(Singleton).To<(IDatabaseConnection conn3, IDatabaseConnection conn4)>()

    // Composition root - represents the main service entry point.
    .Root<OrderRepository>("Repository");

var composition = new Composition();

// Simulate the first user request or batch operation
var repository1 = composition.Repository;
repository1.ProcessOrder("ORD-2025-54546");

// Check that within one repository (one block), connections are shared for consistency
repository1.PrimaryConnection.ShouldBe(repository1.SecondaryConnection);
repository1.OtherConnection.ShouldBe(repository1.FallbackConnection);

repository1.PrimaryConnection.ShouldNotBe(repository1.OtherConnection);

// Simulate the second user request or batch - should have a new PerBlock connection
var repository2 = composition.Repository;
repository2.PrimaryConnection.ShouldNotBe(repository1.PrimaryConnection);

// Interface for database connection - in a real world, this could handle SQL queries
interface IDatabaseConnection;

// Implementation of database connection - transient-like but controlled by lifetime
class DatabaseConnection : IDatabaseConnection;

// Repository for handling orders, injecting multiple connections for demonstration
// In real-world, this could process orders in a batch, sharing connection within the batch
class OrderRepository(
    IDatabaseConnection primaryConnection,
    IDatabaseConnection secondaryConnection,
    (IDatabaseConnection otherConnection, IDatabaseConnection fallbackConnection) additionalConnections)
{
    // Public properties for connections - in practice, these would be private and used in methods
    public IDatabaseConnection PrimaryConnection { get; } = primaryConnection;

    public IDatabaseConnection SecondaryConnection { get; } = secondaryConnection;

    public IDatabaseConnection OtherConnection { get; } = additionalConnections.otherConnection;

    public IDatabaseConnection FallbackConnection { get; } = additionalConnections.fallbackConnection;

    // Example real-world method: Process an order using the shared connection
    public void ProcessOrder(string orderId)
    {
        // Use PrimaryConnection to query database, e.g.,
        // "SELECT * FROM Orders WHERE Id = @orderId"
    }
}
```

To run the above code, the following NuGet packages must be added:
 - [Pure.DI](https://www.nuget.org/packages/Pure.DI)
 - [Shouldly](https://www.nuget.org/packages/Shouldly)

>[!NOTE]
>`PerBlock` provides a balance between `Transient` and `PerResolve`: fewer allocations inside a local block without turning the dependency into long-lived shared state.
Use it for short-lived helpers, local adapters, and operation-level collaborators. Prefer `Scoped` or `PerResolve` when the reuse boundary must be visible at the application level.

## Scope

The `Scoped` lifetime ensures a single instance of a dependency within a scope — a typical example is a single `DbContext`, unit of work, or request context per web request. This example wraps scope creation in a `Scope` class: each scope gets its own `RequestContext`, all services resolved within that scope share it, and disposing the scope disposes all scoped instances it created.

```c#
using Shouldly;
using Pure.DI;
using static Pure.DI.Lifetime;

var composition = new Composition();
IRequestContext ctx1;
IRequestContext ctx2;

// Scope #1
using (var scope1 = composition.NewScope)
{
    var checkout11 = scope1.Checkout;
    var checkout12 = scope1.Checkout;
    ctx1 = checkout11.Context;

    // Same request => same scoped instance
    ctx1.ShouldBe(checkout12.Context);
    ctx1.IsDisposed.ShouldBeFalse();
}

// End of request #1 => scoped instance is disposed
ctx1.IsDisposed.ShouldBeTrue();

// Request #2
using (var scope1 = composition.NewScope)
{
    var checkout2 = scope1.Checkout;
    ctx2 = checkout2.Context;
}

// Different request => different scoped instance
ctx1.ShouldNotBe(ctx2);

// End of request #2 => scoped instance is disposed
ctx2.IsDisposed.ShouldBeTrue();

interface IIdGenerator
{
    Guid Generate();
}

class IdGenerator : IIdGenerator
{
    public Guid Generate() => Guid.NewGuid();
}

interface IRequestContext
{
    Guid CorrelationId { get; }

    bool IsDisposed { get; }
}

// Typically: DbContext / UnitOfWork / RequestTelemetry / Activity, etc.
sealed class RequestContext(IIdGenerator idGenerator)
    : IRequestContext, IDisposable
{
    public Guid CorrelationId { get; } = idGenerator.Generate();

    public bool IsDisposed { get; private set; }

    public void Dispose() => IsDisposed = true;
}

interface ICheckoutService
{
    IRequestContext Context { get; }
}

// "Controller/service" that participates in request processing.
// It depends on a scoped context (per-request resource).
sealed class CheckoutService(IRequestContext context)
    : ICheckoutService
{
    public IRequestContext Context => context;
}

// Represents a scope
class Scope(Composition composition): IDisposable
{
    private readonly Composition _scope = Composition.SetupScope(composition, new Composition());

    public ICheckoutService Checkout => _scope.RequestRoot;

    public void Dispose() => _scope.Dispose();
}

partial class Composition
{
    static void Setup() =>

        DI.Setup()
            .Hint(Hint.ScopeMethodName, "SetupScope")
            // Per-request lifetime
            .Bind().As(Scoped).To<RequestContext>()

            .Bind().As(Singleton).To<IdGenerator>()

            // Regular service that consumes scoped context
            .Bind().To<CheckoutService>()

            // "Request root" (what your controller/handler resolves)
            .Root<ICheckoutService>("RequestRoot")
            .Root<Scope>("NewScope");
}
```

To run the above code, the following NuGet packages must be added:
 - [Pure.DI](https://www.nuget.org/packages/Pure.DI)
 - [Shouldly](https://www.nuget.org/packages/Shouldly)

>[!NOTE]
>A scope is just another composition instance bound to its parent, so singletons remain shared across scopes while scoped instances are unique per scope.

## Scope setup method

The `ScopeMethodName` hint sets the name of a generated static method that binds a new composition instance to a parent scope. This example calls the generated `Composition.SetupScope(...)` method directly to create per-request scopes without defining a wrapper class: scoped instances are unique per scope and are disposed together with it, while singletons remain shared with the parent composition.

```c#
using Shouldly;
using Pure.DI;
using static Pure.DI.Lifetime;

var composition = new Composition();
IRequestContext ctx1;
IRequestContext ctx2;

// Request #1
using (var request1 = Composition.SetupScope(composition, new Composition()))
{
    var checkout11 = request1.RequestRoot;
    var checkout12 = request1.RequestRoot;
    ctx1 = checkout11.Context;

    // Same request => same scoped instance
    ctx1.ShouldBe(checkout12.Context);
    ctx1.IsDisposed.ShouldBeFalse();
}

// End of request #1 => scoped instance is disposed
ctx1.IsDisposed.ShouldBeTrue();

// Request #2
using (var request2 = Composition.SetupScope(composition, new Composition()))
{
    var checkout2 = request2.RequestRoot;
    ctx2 = checkout2.Context;
}

// Different request => different scoped instance
ctx1.ShouldNotBe(ctx2);

// End of request #2 => scoped instance is disposed
ctx2.IsDisposed.ShouldBeTrue();

interface IIdGenerator
{
    Guid Generate();
}

class IdGenerator : IIdGenerator
{
    public Guid Generate() => Guid.NewGuid();
}

interface IRequestContext
{
    Guid CorrelationId { get; }

    bool IsDisposed { get; }
}

// Typically: DbContext / UnitOfWork / RequestTelemetry / Activity, etc.
sealed class RequestContext(IIdGenerator idGenerator)
    : IRequestContext, IDisposable
{
    public Guid CorrelationId { get; } = idGenerator.Generate();

    public bool IsDisposed { get; private set; }

    public void Dispose() => IsDisposed = true;
}

interface ICheckoutService
{
    IRequestContext Context { get; }
}

// "Controller/service" that participates in request processing.
// It depends on a scoped context (per-request resource).
sealed class CheckoutService(IRequestContext context)
    : ICheckoutService
{
    public IRequestContext Context => context;
}

partial class Composition
{
    static void Setup() =>

        DI.Setup()
            .Hint(Hint.ScopeMethodName, "SetupScope")
            // Per-request lifetime
            .Bind().As(Scoped).To<RequestContext>()

            .Bind().As(Singleton).To<IdGenerator>()

            // Regular service that consumes scoped context
            .Bind().To<CheckoutService>()

            // "Request root" (what your controller/handler resolves)
            .Root<ICheckoutService>("RequestRoot");
}
```

To run the above code, the following NuGet packages must be added:
 - [Pure.DI](https://www.nuget.org/packages/Pure.DI)
 - [Shouldly](https://www.nuget.org/packages/Shouldly)


## Auto scoped

You can use the following example to automatically create a session when creating instances of a particular type:

```c#
using Shouldly;
using Pure.DI;
using static Pure.DI.Lifetime;

var composition = new Composition();
var musicApp = composition.MusicAppRoot;

// Session #1: user starts listening on "Living Room Speaker"
var session1 = musicApp.StartListeningSession();
session1.Enqueue("Daft Punk - One More Time");
session1.Enqueue("Massive Attack - Teardrop");

// Session #2: user starts listening on "Headphones"
var session2 = musicApp.StartListeningSession();
session2.Enqueue("Radiohead - Weird Fishes/Arpeggi");

// Different sessions -> different scoped queue instances
session1.Queue.ShouldNotBe(session2.Queue);

// But inside one session, the same queue is used everywhere within that scope
session1.Queue.Items.Count.ShouldBe(2);
session2.Queue.Items.Count.ShouldBe(1);

// Domain abstractions

interface IPlaybackQueue
{
    IReadOnlyList<string> Items { get; }
    void Add(string trackTitle);
}

sealed class PlaybackQueue : IPlaybackQueue
{
    private readonly List<string> _items = [];

    public IReadOnlyList<string> Items => _items;

    public void Add(string trackTitle) => _items.Add(trackTitle);
}

interface IListeningSession
{
    IPlaybackQueue Queue { get; }

    void Enqueue(string trackTitle);
}

sealed class ListeningSession(IPlaybackQueue queue) : IListeningSession
{
    public IPlaybackQueue Queue => queue;

    public void Enqueue(string trackTitle) => queue.Add(trackTitle);
}

// Implements a "session boundary" for listening
class MusicApp(Func<IListeningSession> sessionFactory)
{
    // Each call creates a new DI scope under the hood (new "listening session").
    public IListeningSession StartListeningSession() => sessionFactory();
}

partial class Composition
{
    static void Setup() =>

        DI.Setup()
            // Scoped: one queue per listening session
            .Bind().As(Scoped).To<PlaybackQueue>()

            // Session composition root (private root used only to build sessions)
            .Root<ListeningSession>("Session", kind: RootKinds.Private)

            // Auto scoped factory: creates a new scope for each listening session
            .Bind().To(IListeningSession (Composition parentScope) => {
                // Create a child scope so scoped services (PlaybackQueue) are unique per session.
                var scope = new Composition(parentScope);
                return scope.Session;
            })

            // App-level root
            .Root<MusicApp>("MusicAppRoot");
}
```

To run the above code, the following NuGet packages must be added:
 - [Pure.DI](https://www.nuget.org/packages/Pure.DI)
 - [Shouldly](https://www.nuget.org/packages/Shouldly)

>[!IMPORTANT]
>The method `Inject()`cannot be used outside of the binding setup.

## Default lifetime

When most bindings share the same lifetime, repeating `.As(...)` on each of them is noisy.
`DefaultLifetime(...)` sets the lifetime applied to every subsequent binding in the setup chain that doesn't specify one — until the chain ends or `DefaultLifetime(...)` is called again.
Here `DefaultLifetime(Singleton)` makes both the gateway and the assistant singletons, so the two gateway references inside the assistant resolve to the same instance.

```c#
using Shouldly;
using Pure.DI;
using static Pure.DI.Lifetime;

DI.Setup(nameof(Composition))
    // In real AI apps, the "client" (HTTP handler, connection pool, retries, telemetry)
    // is typically expensive and should be shared.
    //
    // DefaultLifetime(Singleton) makes *all* bindings in this chain singletons,
    // until the chain ends or DefaultLifetime(...) is called again.
    .DefaultLifetime(Singleton)
    .Bind().To<LlmGateway>()
    .Bind().To<RagChatAssistant>()
    .Root<IChatAssistant>("Assistant");

var composition = new Composition();

// Think of these as two independent "requests" to resolve the assistant.
// With singleton lifetime, you get the same assistant instance each time.
var assistant1 = composition.Assistant;
var assistant2 = composition.Assistant;

assistant1.ShouldBe(assistant2);

// The assistant depends on the same gateway in two places (e.g., chat + embeddings).
// Because the gateway is singleton, both references are the *same instance*.
assistant1.ChatGateway.ShouldBe(assistant1.EmbeddingsGateway);

// And because the assistant itself is singleton, it reuses the same gateway across resolutions.
assistant1.ChatGateway.ShouldBe(assistant2.ChatGateway);

// Represents an "LLM provider gateway": HTTP client, auth, retries, rate limiting, etc.
// NOTE: No secrets here; in real projects you'd configure credentials via secure configuration.
interface ILlmGateway;

// Concrete gateway implementation (placeholder for "OpenAI/Anthropic/Azure/etc. client").
class LlmGateway : ILlmGateway;

// A chat assistant that does RAG (Retrieval-Augmented Generation).
// It needs the gateway for:
// - Chat completions (answer generation)
// - Embeddings (vectorization of question/documents)
interface IChatAssistant
{
    ILlmGateway ChatGateway { get; }

    ILlmGateway EmbeddingsGateway { get; }
}

class RagChatAssistant(
    ILlmGateway chatGateway,
    ILlmGateway embeddingsGateway)
    : IChatAssistant
{
    public ILlmGateway ChatGateway { get; } = chatGateway;

    public ILlmGateway EmbeddingsGateway { get; } = embeddingsGateway;
}
```

To run the above code, the following NuGet packages must be added:
 - [Pure.DI](https://www.nuget.org/packages/Pure.DI)
 - [Shouldly](https://www.nuget.org/packages/Shouldly)

>[!NOTE]
>Default lifetime reduces configuration verbosity when a particular lifetime is predominant in your composition.

## Default lifetime for a type

For example, if a certain lifetime is used more often than others, you can make it the default lifetime for a certain type:

```c#
using Shouldly;
using Pure.DI;
using static Pure.DI.Lifetime;

DI.Setup(nameof(Composition))
    // In a real base station, the time source (PTP/GNSS disciplined clock)
    // is a shared infrastructure component:
    // it should be created once per station and reused everywhere.
    .DefaultLifetime<ITimeSource>(Singleton)

    // Time source used by multiple subsystems
    .Bind().To<GnssTimeSource>()

    // Upper-level station components (usually transient by default)
    .Bind().To<BaseStationController>()
    .Bind().To<RadioScheduler>()

    // Composition root (represents "get me a controller instance")
    .Root<IBaseStationController>("Controller");

var composition = new Composition();

// Two independent controller instances (e.g., two independent operations)
var controller1 = composition.Controller;
var controller2 = composition.Controller;

controller1.ShouldNotBe(controller2);

// Inside one controller we request ITimeSource twice:
// the same singleton instance should be injected both times.
controller1.SyncTimeSource.ShouldBe(controller1.SchedulerTimeSource);

// Across different controllers the same station-wide time source is reused.
controller1.SyncTimeSource.ShouldBe(controller2.SyncTimeSource);

// A shared station-wide dependency
interface ITimeSource
{
    long UnixTimeMilliseconds { get; }
}

// Represents a GNSS-disciplined clock (or PTP grandmaster input).
// In real deployments you'd talk to a driver / NIC / daemon here.
class GnssTimeSource : ITimeSource
{
    public long UnixTimeMilliseconds => DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
}

interface IBaseStationController
{
    ITimeSource SyncTimeSource { get; }
    ITimeSource SchedulerTimeSource { get; }
}

// A "top-level" controller of the base station.
// It depends on the time source for synchronization and for scheduling decisions.
class BaseStationController(
    ITimeSource syncTimeSource,
    RadioScheduler scheduler)
    : IBaseStationController
{
    // Used for time synchronization / frame timing
    public ITimeSource SyncTimeSource { get; } = syncTimeSource;

    // Demonstrates that scheduler also uses the same singleton time source
    public ITimeSource SchedulerTimeSource { get; } = scheduler.TimeSource;
}

// A subsystem (e.g., MAC scheduler) that also needs precise time.
class RadioScheduler(ITimeSource timeSource)
{
    public ITimeSource TimeSource { get; } = timeSource;
}
```

To run the above code, the following NuGet packages must be added:
 - [Pure.DI](https://www.nuget.org/packages/Pure.DI)
 - [Shouldly](https://www.nuget.org/packages/Shouldly)

>[!NOTE]
>Setting default lifetime for types simplifies configuration when the same lifetime is consistently applied.

## Default lifetime for a type and a tag

For example, if a certain lifetime is used more often than others, you can make it the default lifetime for a certain type:

```c#
using Shouldly;
using Pure.DI;
using static Pure.DI.Lifetime;

DI.Setup(nameof(Composition))
    // Real-world idea:
    // "Live" audio capture device should be shared (singleton),
    // while a regular (untagged) audio source can be created per session (transient).
    .DefaultLifetime<IAudioSource>(Singleton, "Live")

    // Tagged binding: "Live" audio capture (shared)
    .Bind("Live").To<LiveAudioSource>()

    // Untagged binding: some other source (new instance each time)
    .Bind().To<BufferedAudioSource>()

    // A playback session uses two sources:
    // - Live (shared, tagged)
    // - Buffered (transient, untagged)
    .Bind().To<PlaybackSession>()

    // Composition root
    .Root<IPlaybackSession>("PlaybackSession");

var composition = new Composition();

// Two independent sessions (transient root)
var session1 = composition.PlaybackSession;
var session2 = composition.PlaybackSession;

session1.ShouldNotBe(session2);

// Within a single session:
// - Live source is tagged => default lifetime forces it to be shared (singleton)
// - Buffered source is untagged => transient => always a new instance
session1.LiveSource.ShouldNotBe(session1.BufferedSource);

// Between sessions:
// - Live source is a shared singleton (same instance)
// - Buffered source is transient (different instances)
session1.LiveSource.ShouldBe(session2.LiveSource);

interface IAudioSource;

// "Live" device: e.g., microphone/line-in capture.
class LiveAudioSource : IAudioSource;

// "Buffered" source: e.g., decoded audio chunks, per-session pipeline buffer.
class BufferedAudioSource : IAudioSource;

interface IPlaybackSession
{
    IAudioSource LiveSource { get; }

    IAudioSource BufferedSource { get; }
}

class PlaybackSession(
    // Tagged dependency: should be singleton because of DefaultLifetime<IAudioSource>(..., "Live")
    [Tag("Live")] IAudioSource liveSource,

    // Untagged dependency: transient by default
    IAudioSource bufferedSource)
    : IPlaybackSession
{
    public IAudioSource LiveSource { get; } = liveSource;

    public IAudioSource BufferedSource { get; } = bufferedSource;
}
```

To run the above code, the following NuGet packages must be added:
 - [Pure.DI](https://www.nuget.org/packages/Pure.DI)
 - [Shouldly](https://www.nuget.org/packages/Shouldly)

>[!NOTE]
>Default lifetime configuration reduces boilerplate when the same lifetime is consistently used for specific types.

## Disposable singleton

To dispose all created singleton instances, simply dispose the composition instance:

```c#
using Shouldly;
using Pure.DI;
using static Pure.DI.Lifetime;

DI.Setup(nameof(Composition))
    // Disable Resolve methods to keep the public API minimal
    .Hint(Hint.Resolve, "Off")

    // A realistic example:
    // a submarine has a shared hardware bus to onboard sensors.
    // It should be created once and disposed when the "mission scope"
    // (the composition instance) ends.
    .Bind().As(Singleton).To<AcousticSensorBus>()
    .Bind().To<SubmarineCombatSystem>()
    .Root<ICombatSystem>("CombatSystem");

IAcousticSensorBus bus;
using (var composition = new Composition())
{
    var combatSystem = composition.CombatSystem;

    // Store the singleton instance to verify that it gets disposed
    // when composition is disposed.
    bus = combatSystem.SensorBus;

    // In real usage you would call methods like:
    // combatSystem.ScanForContacts();
}

// When the mission scope ends, all disposable singletons created by it
// must be disposed.
bus.IsDisposed.ShouldBeTrue();

interface IAcousticSensorBus
{
    bool IsDisposed { get; }
}

// Represents a shared connection to submarine sensors (sonar, hydrophones, etc.).
// This is a singleton because the hardware bus is typically a single shared resource,
// and it must be cleaned up properly.
class AcousticSensorBus : IAcousticSensorBus, IDisposable
{
    public bool IsDisposed { get; private set; }

    public void Dispose() => IsDisposed = true;
}

interface ICombatSystem
{
    IAcousticSensorBus SensorBus { get; }
}

// A "combat system" is a typical high-level service that uses shared hardware resources.
class SubmarineCombatSystem(IAcousticSensorBus sensorBus) : ICombatSystem
{
    public IAcousticSensorBus SensorBus { get; } = sensorBus;
}
```

To run the above code, the following NuGet packages must be added:
 - [Pure.DI](https://www.nuget.org/packages/Pure.DI)
 - [Shouldly](https://www.nuget.org/packages/Shouldly)

A composition class becomes disposable if it creates at least one disposable singleton instance.

## Async disposable singleton

If at least one of these objects implements the `IAsyncDisposable` interface, then the composition implements `IAsyncDisposable` as well. To dispose of all created singleton instances in an asynchronous manner, simply dispose of the composition instance in an asynchronous manner:

```c#
using Shouldly;
using Pure.DI;
using static Pure.DI.Lifetime;
using System.Threading.Tasks;

DI.Setup(nameof(Composition))
    // A singleton resource that needs async cleanup (e.g., flushing buffers, closing connections)
    .Bind().As(Singleton).To<AuditLogWriter>()
    .Bind().To<CheckoutService>()
    .Root<ICheckoutService>("CheckoutService");

AuditLogWriter writer;

await using (var composition = new Composition())
{
    var service = composition.CheckoutService;

    // A "live" usage: do some work that writes to an audit log
    await service.CheckoutAsync(orderId: "ORD-2025-00042");

    // Keep a reference so we can assert disposal after the composition is disposed
    writer = service.Writer;
    writer.IsDisposed.ShouldBeFalse();
}

// Composition disposal triggers async disposal of singleton(s)
writer.IsDisposed.ShouldBeTrue();

interface ICheckoutService
{
    AuditLogWriter Writer { get; }

    ValueTask CheckoutAsync(string orderId);
}

/// <summary>
/// Represents a singleton infrastructure component.
/// Think: audit log writer, message producer, telemetry pipeline, DB connection, etc.
/// It is owned by the DI composition and must be disposed asynchronously.
/// </summary>
sealed class AuditLogWriter : IAsyncDisposable
{
    public bool IsDisposed { get; private set; }

    public async ValueTask WriteAsync(string message)
    {
        ObjectDisposedException.ThrowIf(IsDisposed, nameof(AuditLogWriter));
        // Simulate I/O (writing to file / network / remote log)
        await Task.Delay(5);
    }

    public async ValueTask DisposeAsync()
    {
        // Simulate async cleanup: flush buffers / send remaining events / gracefully close connection
        await Task.Delay(5);
        IsDisposed = true;
    }
}

sealed class CheckoutService(AuditLogWriter writer) : ICheckoutService
{
    public AuditLogWriter Writer { get; } = writer;

    public ValueTask CheckoutAsync(string orderId)
    {
        // Real-world-ish side effect: record a business event
        return Writer.WriteAsync($"Checkout completed: {orderId}");
    }
}
```

To run the above code, the following NuGet packages must be added:
 - [Pure.DI](https://www.nuget.org/packages/Pure.DI)
 - [Shouldly](https://www.nuget.org/packages/Shouldly)

>[!NOTE]
>Async disposable singleton ensures proper async cleanup of all singleton resources when the composition is disposed.

## Async disposable scope

When scoped services hold resources that need asynchronous cleanup — network connections, streams, database sessions — implement `IAsyncDisposable` on them and dispose of the scope with `await scope.DisposeAsync()`.
A scope is a class derived from the composition (here `Session`): each session gets its own `Scoped` instances, and disposing the session asynchronously disposes everything created within it.

```c#
using Shouldly;
using Pure.DI;
using static Pure.DI.Lifetime;

var composition = new Composition();
var program = composition.ProgramRoot;

// Creates session #1
var session1 = program.CreateSession();
var dependency1 = session1.SessionRoot.Dependency;
var dependency12 = session1.SessionRoot.Dependency;

// Checks the identity of scoped instances in the same session
dependency1.ShouldBe(dependency12);

// Creates session #2
var session2 = program.CreateSession();
var dependency2 = session2.SessionRoot.Dependency;

// Checks that the scoped instances are not identical in different sessions
dependency1.ShouldNotBe(dependency2);

// Disposes of session #1
await session1.DisposeAsync();
// Checks that the scoped instance is finalized
dependency1.IsDisposed.ShouldBeTrue();

// Disposes of session #2
await session2.DisposeAsync();
// Checks that the scoped instance is finalized
dependency2.IsDisposed.ShouldBeTrue();

interface IDependency
{
    bool IsDisposed { get; }
}

class Dependency : IDependency, IAsyncDisposable
{
    public bool IsDisposed { get; private set; }

    public ValueTask DisposeAsync()
    {
        IsDisposed = true;
        return ValueTask.CompletedTask;
    }
}

interface IService
{
    IDependency Dependency { get; }
}

class Service(IDependency dependency) : IService
{
    public IDependency Dependency => dependency;
}

// Implements a session
class Session(Composition composition) : Composition(composition);

partial class Program(Func<Session> sessionFactory)
{
    public Session CreateSession() => sessionFactory();
}

partial class Composition
{
    static void Setup() =>

        DI.Setup()
            .Bind().As(Scoped).To<Dependency>()
            .Bind().To<Service>()

            // Session composition root
            .Root<IService>("SessionRoot")

            // Program composition root
            .Root<Program>("ProgramRoot");
}
```

To run the above code, the following NuGet packages must be added:
 - [Pure.DI](https://www.nuget.org/packages/Pure.DI)
 - [Shouldly](https://www.nuget.org/packages/Shouldly)

>[!NOTE]
>Async disposable scope is essential for scenarios requiring proper async cleanup of scoped resources.

## Func

_Func<T>_ helps when the logic must enter instances of some type on demand or more than once. This is a very handy mechanism for instance replication. For example it is used when implementing the `Lazy<T>` injection.

```c#
using Shouldly;
using Pure.DI;
using System.Collections.Immutable;

DI.Setup(nameof(Composition))
    .Bind().As(Lifetime.Singleton).To<TicketIdGenerator>()
    .Bind().To<Ticket>()
    .Bind().To<QueueTerminal>()

    // Composition root
    .Root<IQueueTerminal>("Terminal");

var composition = new Composition();
var terminal = composition.Terminal;

terminal.Tickets.Length.ShouldBe(3);

terminal.Tickets[0].Id.ShouldBe(1);
terminal.Tickets[1].Id.ShouldBe(2);
terminal.Tickets[2].Id.ShouldBe(3);

interface ITicketIdGenerator
{
    int NextId { get; }
}

class TicketIdGenerator : ITicketIdGenerator
{
    public int NextId => ++field;
}

interface ITicket
{
    int Id { get; }
}

class Ticket(ITicketIdGenerator idGenerator) : ITicket
{
    public int Id { get; } = idGenerator.NextId;
}

interface IQueueTerminal
{
    ImmutableArray<ITicket> Tickets { get; }
}

class QueueTerminal(Func<ITicket> ticketFactory) : IQueueTerminal
{
    public ImmutableArray<ITicket> Tickets { get; } =
    [
        // The factory creates a new instance of the ticket each time it is called
        ticketFactory(),
        ticketFactory(),
        ticketFactory()
    ];
}
```

To run the above code, the following NuGet packages must be added:
 - [Pure.DI](https://www.nuget.org/packages/Pure.DI)
 - [Shouldly](https://www.nuget.org/packages/Shouldly)

Be careful, replication takes into account the lifetime of the object.

## Func with arguments

Sometimes an instance can only be created with values known at runtime, such as an id or a name. Injecting a `Func<..., T>` with arguments gives you a factory: values passed at call time are matched by type to the constructor parameters (here `int id` and `string name` of `Person`), while the remaining dependencies, like `IClock`, are resolved from the composition as usual.

```c#
using Shouldly;
using Pure.DI;
using System.Collections.Immutable;

DI.Setup(nameof(Composition))
    .Bind().As(Lifetime.Singleton).To<Clock>()
    .Bind().To<Person>()
    .Bind().To<Team>()

    // Composition root
    .Root<ITeam>("Team");

var composition = new Composition();
var team = composition.Team;

team.Members.Length.ShouldBe(3);

team.Members[0].Id.ShouldBe(10);
team.Members[0].Name.ShouldBe("Nik");

team.Members[1].Id.ShouldBe(20);
team.Members[1].Name.ShouldBe("Mike");

team.Members[2].Id.ShouldBe(30);
team.Members[2].Name.ShouldBe("Jake");

interface IClock
{
    DateTimeOffset Now { get; }
}

class Clock : IClock
{
    public DateTimeOffset Now => DateTimeOffset.Now;
}

interface IPerson
{
    int Id { get; }

    string Name { get; }
}

class Person(string name, IClock clock, int id)
    : IPerson
{
    public int Id => id;

    public string Name => name;
}

interface ITeam
{
    ImmutableArray<IPerson> Members { get; }
}

class Team(Func<int, string, IPerson> personFactory) : ITeam
{
    public ImmutableArray<IPerson> Members { get; } =
    [
        personFactory(10, "Nik"),
        personFactory(20, "Mike"),
        personFactory(30, "Jake")
    ];
}
```

To run the above code, the following NuGet packages must be added:
 - [Pure.DI](https://www.nuget.org/packages/Pure.DI)
 - [Shouldly](https://www.nuget.org/packages/Shouldly)

>[!NOTE]
>Func with arguments provides flexibility for scenarios where you need to pass runtime parameters during instance creation.

## Func with tag

A tag applied to a `Func<T>` dependency carries over to the instances it creates. Here `[Tag("postgres")] Func<IDbConnection>` resolves the binding registered with the `"postgres"` tag, and each call returns a new `NpgsqlConnection`, letting the pool create as many distinct connections as it needs.

```c#
using Shouldly;
using Pure.DI;
using System.Collections.Immutable;

DI.Setup(nameof(Composition))
    .Bind<IDbConnection>("postgres").To<NpgsqlConnection>()
    .Bind<IConnectionPool>().To<ConnectionPool>()

    // Composition root
    .Root<IConnectionPool>("ConnectionPool");

var composition = new Composition();
var pool = composition.ConnectionPool;

// Check that the pool has created 3 connections
pool.Connections.Length.ShouldBe(3);
pool.Connections[0].ShouldBeOfType<NpgsqlConnection>();

interface IDbConnection;

// Specific implementation for PostgreSQL
class NpgsqlConnection : IDbConnection;

interface IConnectionPool
{
    ImmutableArray<IDbConnection> Connections { get; }
}

class ConnectionPool([Tag("postgres")] Func<IDbConnection> connectionFactory) : IConnectionPool
{
    public ImmutableArray<IDbConnection> Connections { get; } =
    [
        // Use the factory to create distinct connection instances
        connectionFactory(),
        connectionFactory(),
        connectionFactory()
    ];
}
```

To run the above code, the following NuGet packages must be added:
 - [Pure.DI](https://www.nuget.org/packages/Pure.DI)
 - [Shouldly](https://www.nuget.org/packages/Shouldly)

>[!NOTE]
>Func with tags allows you to create instances with specific tags dynamically, useful for factory patterns with multiple implementations.

## Enumerable

Specifying `IEnumerable<T>` as the injection type lets you inject instances of all bindings that implement type `T` in a lazy fashion - the instances will be provided one by one, in order corresponding to the sequence of bindings.

```c#
using Shouldly;
using Pure.DI;
using System.Collections.Immutable;

DI.Setup(nameof(Composition))
    .Bind<IMessageSender>().To<EmailSender>()
    .Bind<IMessageSender>("sms").To<SmsSender>()
    .Bind<INotificationService>().To<NotificationService>()

    // Composition root
    .Root<INotificationService>("NotificationService");

var composition = new Composition();
var notificationService = composition.NotificationService;
notificationService.Senders.Length.ShouldBe(2);
notificationService.Senders[0].ShouldBeOfType<EmailSender>();
notificationService.Senders[1].ShouldBeOfType<SmsSender>();

notificationService.Notify("Hello World");

interface IMessageSender
{
    void Send(string message);
}

class EmailSender : IMessageSender
{
    public void Send(string message)
    {
        // Sending email...
    }
}

class SmsSender : IMessageSender
{
    public void Send(string message)
    {
        // Sending SMS...
    }
}

interface INotificationService
{
    ImmutableArray<IMessageSender> Senders { get; }

    void Notify(string message);
}

class NotificationService(IEnumerable<IMessageSender> senders) : INotificationService
{
    public ImmutableArray<IMessageSender> Senders { get; }
        = [..senders];

    public void Notify(string message)
    {
        foreach (var sender in Senders)
        {
            sender.Send(message);
        }
    }
}
```

To run the above code, the following NuGet packages must be added:
 - [Pure.DI](https://www.nuget.org/packages/Pure.DI)
 - [Shouldly](https://www.nuget.org/packages/Shouldly)

>[!NOTE]
>IEnumerable<T> provides lazy evaluation, making it efficient for scenarios where you may not need to enumerate all instances.

## Enumerable generics

Shows how generic middleware pipelines collect all matching implementations.

```c#
using Shouldly;
using Pure.DI;
using System.Collections.Immutable;

DI.Setup(nameof(Composition))
    // Register generic middleware components.
    // LoggingMiddleware<T> is registered as the default implementation.
    .Bind<IMiddleware<TT>>().To<LoggingMiddleware<TT>>()
    // MetricsMiddleware<T> is registered with the "Metrics" tag.
    .Bind<IMiddleware<TT>>("Metrics").To<MetricsMiddleware<TT>>()

    // Register the pipeline that takes the collection of all middleware.
    .Bind<IPipeline<TT>>().To<Pipeline<TT>>()

    // Composition roots for different data types (int and string)
    .Root<IPipeline<int>>("IntPipeline")
    .Root<IPipeline<string>>("StringPipeline");

var composition = new Composition();

// Validate the pipeline for int
var intPipeline = composition.IntPipeline;
intPipeline.Middlewares.Length.ShouldBe(2);
intPipeline.Middlewares[0].ShouldBeOfType<LoggingMiddleware<int>>();
intPipeline.Middlewares[1].ShouldBeOfType<MetricsMiddleware<int>>();

// Validate the pipeline for string
var stringPipeline = composition.StringPipeline;
stringPipeline.Middlewares.Length.ShouldBe(2);
stringPipeline.Middlewares[0].ShouldBeOfType<LoggingMiddleware<string>>();
stringPipeline.Middlewares[1].ShouldBeOfType<MetricsMiddleware<string>>();

// Middleware interface
interface IMiddleware<T>;

// Logging implementation
class LoggingMiddleware<T> : IMiddleware<T>;

// Metrics implementation
class MetricsMiddleware<T> : IMiddleware<T>;

// Pipeline interface
interface IPipeline<T>
{
    ImmutableArray<IMiddleware<T>> Middlewares { get; }
}

// Pipeline implementation that aggregates all available middleware
class Pipeline<T>(IEnumerable<IMiddleware<T>> middlewares) : IPipeline<T>
{
    public ImmutableArray<IMiddleware<T>> Middlewares { get; }
        = [..middlewares];
}
```

To run the above code, the following NuGet packages must be added:
 - [Pure.DI](https://www.nuget.org/packages/Pure.DI)
 - [Shouldly](https://www.nuget.org/packages/Shouldly)

>[!NOTE]
>Generic enumerable injections are useful for implementing middleware patterns where multiple handlers need to be invoked in sequence.

## Array

Specifying `T[]` as the injection type allows instances from all bindings that implement the `T` type to be injected.

```c#
using Shouldly;
using Pure.DI;

DI.Setup(nameof(Composition))
    .Bind<ISensor>().To<TemperatureSensor>()
    .Bind<ISensor>("External").To<WindSensor>()
    .Bind<ISensorService>().To<SensorService>()

    // Composition root
    .Root<ISensorService>("Sensor");

var composition = new Composition();
var sensor = composition.Sensor;

// Checks that all bindings for the ISensor interface are injected,
// regardless of whether they are tagged or not.
sensor.Sensors.Length.ShouldBe(2);
sensor.Sensors[0].ShouldBeOfType<TemperatureSensor>();
sensor.Sensors[1].ShouldBeOfType<WindSensor>();

interface ISensor;

class TemperatureSensor : ISensor;

class WindSensor : ISensor;

interface ISensorService
{
    ISensor[] Sensors { get; }
}

class SensorService(ISensor[] sensors) : ISensorService
{
    public ISensor[] Sensors { get; } = sensors;
}
```

To run the above code, the following NuGet packages must be added:
 - [Pure.DI](https://www.nuget.org/packages/Pure.DI)
 - [Shouldly](https://www.nuget.org/packages/Shouldly)

In addition to arrays, other collection types are also supported, such as:
- System.Memory<T>
- System.ReadOnlyMemory<T>
- System.Span<T>
- System.ReadOnlySpan<T>
- System.Collections.Generic.ICollection<T>
- System.Collections.Generic.IList<T>
- System.Collections.Generic.List<T>
- System.Collections.Generic.IReadOnlyCollection<T>
- System.Collections.Generic.IReadOnlyList<T>
- System.Collections.Generic.ISet<T>
- System.Collections.Generic.HashSet<T>
- System.Collections.Generic.SortedSet<T>
- System.Collections.Generic.Queue<T>
- System.Collections.Generic.Stack<T>
- System.Collections.Immutable.ImmutableArray<T>
- System.Collections.Immutable.IImmutableList<T>
- System.Collections.Immutable.ImmutableList<T>
- System.Collections.Immutable.IImmutableSet<T>
- System.Collections.Immutable.ImmutableHashSet<T>
- System.Collections.Immutable.ImmutableSortedSet<T>
- System.Collections.Immutable.IImmutableQueue<T>
- System.Collections.Immutable.ImmutableQueue<T>
- System.Collections.Immutable.IImmutableStack<T>
And of course this list can easily be supplemented on its own.

## Span and ReadOnlySpan

Specifying `Span<T>` and `ReadOnlySpan<T>` work the same as with the array `T[]` for immediate constructor or method use.

```c#
using Shouldly;
using Pure.DI;

DI.Setup(nameof(Composition))
    .Bind<Point>('a').To(() => new Point(1, 1))
    .Bind<Point>('b').To(() => new Point(2, 2))
    .Bind<Point>('c').To(() => new Point(3, 3))
    .Bind<IPath>().To<Path>()

    // Composition root
    .Root<IPath>("Path");

var composition = new Composition();
var path = composition.Path;
path.PointCount.ShouldBe(3);

readonly struct Point(int x, int y)
{
    public int X { get; } = x;

    public int Y { get; } = y;
}

interface IPath
{
    int PointCount { get; }
}

class Path(ReadOnlySpan<Point> points) : IPath
{
    // The 'points' span is allocated on the stack, so it's very efficient.
    // However, we cannot store it in a field because it's a ref struct.
    // We can process it here in the constructor.
    public int PointCount { get; } = points.Length;
}
```

To run the above code, the following NuGet packages must be added:
 - [Pure.DI](https://www.nuget.org/packages/Pure.DI)
 - [Shouldly](https://www.nuget.org/packages/Shouldly)

This scenario is even more efficient in the case of `Span<T>` or `ReadOnlySpan<T>` when `T` is a value type. In this case, there is no heap allocation, and the composition root `IPath` looks like this:
```c#
public IPath Path
{
  get
  {
    ReadOnlySpan<Point> points = stackalloc Point[3] { new Point(1, 1), new Point(2, 2), new Point(3, 3) };
    return new Path(points);
  }
}
```
Constructor injection into a heap type is available for compatibility and reports warning `DIW012`. Prefer method injection for new code when the stack-only value is only needed during initialization.
Generic root arguments with `where T : allows ref struct` follow the same rules. Pure.DI treats such `T` as maybe stack-only and emits `scoped T` in generated root signatures.
When a root has several arguments, `scoped` is applied only to arguments that are stack-only or maybe stack-only themselves. Heap-safe wrappers such as `Wrapper<T>` remain regular parameters even when `T` allows ref structs.
Factory bodies may resolve and consume stack-only values immediately via `ctx.Inject<T>(...)` when the API target supports `allows ref struct`. Pure.DI reports `DIE049` when such values are captured behind a generated delegate or deferred factory.
Factory overrides may pass stack-only values with `ctx.Override<T>(...)` or `ctx.Let<T>(...)` only inside the current synchronous factory frame or the current delegate invocation. Delegate arguments such as `T text` can be consumed immediately by method injection, but Pure.DI still reports `DIE049` if an outer stack-only value is captured by the delegate or if `text` is passed into a nested/returned delegate.
When manual `ctx.Override<T>(...)` or `ctx.Let<T>(...)` calls are used in factory delegates, the usual thread-safety rule still applies: wrap the override and the following injection in `lock (ctx.Lock)` or disable thread safety for known single-threaded compositions. Otherwise Pure.DI reports `DIW013`.
The generated default `Func<ReadOnlySpan<char>, T>` binding does not use shared override state and does not require a manual `lock`; use it when the standard `Func` shape is enough.
Generic interfaces are allowed when the implementation is heap-safe, for example `class Parser<T> : IParser<T>`. Pure.DI reports `DIE048` only when the implementation itself is stack-only, such as `ref struct Parser<T> : IParser<T>`.
Pure.DI reports errors when `Span<T>`, `ReadOnlySpan<T>`, or custom `ref struct` values are injected into fields, properties, stored lifetimes, delegate captures, or stack-only interface conversions.

## Dictionary

When a service needs to pick a dependency by key at runtime — for example, choosing a notification channel — inject an `IReadOnlyDictionary<TKey, TValue>`. Bind each entry as a `KeyValuePair<TKey, TValue>` with `Tag.Unique`, and Pure.DI collects all such pairs into the dictionary automatically.

```c#
using Shouldly;
using Pure.DI;

DI.Setup(nameof(Composition))
    .Bind(Tag.Unique).To((EmailChannel chanel) => new KeyValuePair<Channel, INotificationChannel>(Channel.Email, chanel))
    .Bind(Tag.Unique).To((SmsChannel chanel) => new KeyValuePair<Channel, INotificationChannel>(Channel.Sms, chanel))
    .Bind(Tag.Unique).To((PushChannel chanel) => new KeyValuePair<Channel, INotificationChannel>(Channel.Push, chanel))
    .Bind<INotificationService>().To<NotificationService>()

    // Composition root
    .Root<INotificationService>("NotificationService");

var composition = new Composition();
var notificationService = composition.NotificationService;

// Verify that all notification channels are injected into the dictionary
notificationService.Channels.Count.ShouldBe(3);
notificationService.Channels[Channel.Email].ShouldBeOfType<EmailChannel>();
notificationService.Channels[Channel.Sms].ShouldBeOfType<SmsChannel>();
notificationService.Channels[Channel.Push].ShouldBeOfType<PushChannel>();

interface INotificationChannel
{
    void Send(string message);
}

class EmailChannel : INotificationChannel
{
    public void Send(string message) => Console.WriteLine($"Email: {message}");
}

class SmsChannel : INotificationChannel
{
    public void Send(string message) => Console.WriteLine($"SMS: {message}");
}

class PushChannel : INotificationChannel
{
    public void Send(string message) => Console.WriteLine($"Push: {message}");
}

enum Channel { Email, Sms, Push }

interface INotificationService
{
    IReadOnlyDictionary<Channel, INotificationChannel> Channels { get; }
}

class NotificationService(IReadOnlyDictionary<Channel, INotificationChannel> channels) : INotificationService
{
    public IReadOnlyDictionary<Channel, INotificationChannel> Channels { get; } = channels;
}
```

To run the above code, the following NuGet packages must be added:
 - [Pure.DI](https://www.nuget.org/packages/Pure.DI)
 - [Shouldly](https://www.nuget.org/packages/Shouldly)

>[!NOTE]
>Dictionary injection is useful when you need to access dependencies by keys, such as named or tagged implementations like notification channels.

## Lazy

Injecting `Lazy<T>` defers creation of a dependency until its `Value` property is first accessed, after which the same instance is returned every time. No extra setup is needed: bind the underlying type as usual and request `Lazy<T>` in the constructor.

```c#
using Shouldly;
using Pure.DI;

DI.Setup(nameof(Composition))
    .Bind<IGraphicsEngine>().To<GraphicsEngine>()
    .Bind<IWindow>().To<Window>()

    // Composition root
    .Root<IWindow>("Window");

var composition = new Composition();
var window = composition.Window;

// The graphics engine is created only when it is first accessed
window.Engine.ShouldBe(window.Engine);

interface IGraphicsEngine;

class GraphicsEngine : IGraphicsEngine;

interface IWindow
{
    IGraphicsEngine Engine { get; }
}

class Window(Lazy<IGraphicsEngine> engine) : IWindow
{
    public IGraphicsEngine Engine => engine.Value;
}
```

To run the above code, the following NuGet packages must be added:
 - [Pure.DI](https://www.nuget.org/packages/Pure.DI)
 - [Shouldly](https://www.nuget.org/packages/Shouldly)

>[!NOTE]
>Lazy<T> is useful for expensive-to-create objects or when the instance may never be needed, improving application startup performance.

## Task

By default, tasks are started automatically when they are injected. It is recommended to use an argument of type <c>CancellationToken</c> to the composition root to be able to cancel the execution of a task. In this case, the composition root property is automatically converted to a method with a parameter of type <c>CancellationToken</c>. To start a task, an instance of type <c>TaskFactory<T></c> is used, with default settings:

- CancellationToken.None
- TaskScheduler.Default
- TaskCreationOptions.None
- TaskContinuationOptions.None

But you can always override them, as in the example below for <c>TaskScheduler.Current</c>.

```c#
using Shouldly;
using Pure.DI;

DI.Setup(nameof(Composition))
    .Hint(Hint.Resolve, "Off")
    // Overrides TaskScheduler.Default if necessary
    .Bind<TaskScheduler>().To(() => TaskScheduler.Current)
    // Specifies to use CancellationToken from the composition root argument,
    // if not specified, then CancellationToken.None will be used
    .RootArg<CancellationToken>("cancellationToken")
    .Bind<IDataService>().To<DataService>()
    .Bind<ICommand>().To<LoadDataCommand>()

    // Composition root
    .Root<ICommand>("GetCommand");

var composition = new Composition();
using var cancellationTokenSource = new CancellationTokenSource();

// Creates a composition root with the CancellationToken passed to it
var command = composition.GetCommand(cancellationTokenSource.Token);
await command.ExecuteAsync(cancellationTokenSource.Token);

interface IDataService
{
    ValueTask<string[]> GetItemsAsync(CancellationToken cancellationToken);
}

class DataService : IDataService
{
    public ValueTask<string[]> GetItemsAsync(CancellationToken cancellationToken) =>
        new(["Item1", "Item2"]);
}

interface ICommand
{
    Task ExecuteAsync(CancellationToken cancellationToken);
}

class LoadDataCommand(Task<IDataService> dataServiceTask) : ICommand
{
    public async Task ExecuteAsync(CancellationToken cancellationToken)
    {
        // Simulating some processing before needing the dependency
        await Task.Delay(1, cancellationToken);

        // The dependency is resolved asynchronously, so we await it here.
        // This allows the dependency to be created in parallel with the execution of this method.
        var dataService = await dataServiceTask;
        var items = await dataService.GetItemsAsync(cancellationToken);
    }
}
```

To run the above code, the following NuGet packages must be added:
 - [Pure.DI](https://www.nuget.org/packages/Pure.DI)
 - [Shouldly](https://www.nuget.org/packages/Shouldly)

>[!NOTE]
>`Task` injection provides automatic background execution with optional cancellation support for asynchronous operations.

## ValueTask

A dependency can be injected as `ValueTask<T>` and awaited when needed — an allocation-friendly alternative to `Task<T>` for values that are usually available synchronously. Bind the underlying type as usual and request `ValueTask<T>`; here `DataProcessor` awaits `ValueTask<IConnection>` before using the connection.

```c#
using Shouldly;
using Pure.DI;

DI.Setup(nameof(Composition))
    .Bind<IConnection>().To<CloudConnection>()
    .Bind<IDataProcessor>().To<DataProcessor>()

    // Composition root
    .Root<IDataProcessor>("DataProcessor");

var composition = new Composition();
var processor = composition.DataProcessor;
await processor.ProcessDataAsync();

interface IConnection
{
    ValueTask<bool> PingAsync();
}

class CloudConnection : IConnection
{
    public ValueTask<bool> PingAsync() => ValueTask.FromResult(true);
}

interface IDataProcessor
{
    ValueTask ProcessDataAsync();
}

class DataProcessor(ValueTask<IConnection> connectionTask) : IDataProcessor
{
    public async ValueTask ProcessDataAsync()
    {
        // The connection is resolved asynchronously, allowing potential
        // non-blocking initialization or resource allocation.
        var connection = await connectionTask;
        await connection.PingAsync();
    }
}
```

To run the above code, the following NuGet packages must be added:
 - [Pure.DI](https://www.nuget.org/packages/Pure.DI)
 - [Shouldly](https://www.nuget.org/packages/Shouldly)

>[!NOTE]
>`ValueTask<T>` reduces allocations compared to `Task<T>` when operations complete synchronously, making it ideal for high-performance scenarios.

## Manually started tasks

By default, tasks are started automatically when they are injected. But you can override this behavior as shown in the example below. It is also recommended to add a binding for <c>CancellationToken</c> to be able to cancel the execution of a task.

```c#
using Pure.DI;

DI.Setup(nameof(Composition))
    // Overrides the default binding that performs an auto-start of a task
    // when it is created. This binding will simply create the task.
    // The start will be handled by the consumer.
    .Bind<Task<TT>>().To(ctx => {
        ctx.Inject(ctx.Tag, out Func<TT> factory);
        ctx.Inject(out CancellationToken cancellationToken);
        return new Task<TT>(factory, cancellationToken);
    })
    // Specifies to use CancellationToken from the composition root argument,
    // if not specified, then CancellationToken.None will be used
    .RootArg<CancellationToken>("cancellationToken")
    .Bind<IUserPreferences>().To<UserPreferences>()
    .Bind<IDashboardService>().To<DashboardService>()

    // Composition root
    .Root<IDashboardService>("GetDashboard");

var composition = new Composition();
using var cancellationTokenSource = new CancellationTokenSource();

// Creates a composition root with the CancellationToken passed to it
var dashboard = composition.GetDashboard(cancellationTokenSource.Token);
await dashboard.LoadAsync(cancellationTokenSource.Token);

interface IUserPreferences
{
    ValueTask LoadAsync(CancellationToken cancellationToken);
}

class UserPreferences : IUserPreferences
{
    public ValueTask LoadAsync(CancellationToken cancellationToken) => ValueTask.CompletedTask;
}

interface IDashboardService
{
    Task LoadAsync(CancellationToken cancellationToken);
}

class DashboardService : IDashboardService
{
    private readonly Task<IUserPreferences> _preferencesTask;

    public DashboardService(Task<IUserPreferences> preferencesTask)
    {
        _preferencesTask = preferencesTask;
        // The task is started manually in the constructor.
        // This allows the loading of preferences to begin immediately in the background,
        // while the service continues its initialization.
        _preferencesTask.Start();
    }

    public async Task LoadAsync(CancellationToken cancellationToken)
    {
        // Wait for the preferences loading task to complete
        var preferences = await _preferencesTask;
        await preferences.LoadAsync(cancellationToken);
    }
}
```

To run the above code, the following NuGet package must be added:
 - [Pure.DI](https://www.nuget.org/packages/Pure.DI)

>[!IMPORTANT]
>The method `Inject()`cannot be used outside of the binding setup.

## Async Enumerable

Specifying `IAsyncEnumerable<T>` as the injection type allows instances of all bindings implementing type `T` to be injected in an asynchronous-lazy manner - the instances will be provided one at a time, in an order corresponding to the sequence of the bindings.

```c#
using Shouldly;
using Pure.DI;

DI.Setup(nameof(Composition))
    .Bind<IHealthCheck>().To<MemoryCheck>()
    .Bind<IHealthCheck>("External").To<ExternalServiceCheck>()
    .Bind<IHealthService>().To<HealthService>()

    // Composition root
    .Root<IHealthService>("HealthService");

var composition = new Composition();
var healthService = composition.HealthService;
var checks = await healthService.GetChecksAsync();

checks[0].ShouldBeOfType<MemoryCheck>();
checks[1].ShouldBeOfType<ExternalServiceCheck>();

interface IHealthCheck;

class MemoryCheck : IHealthCheck;

class ExternalServiceCheck : IHealthCheck;

interface IHealthService
{
    Task<IReadOnlyList<IHealthCheck>> GetChecksAsync();
}

class HealthService(IAsyncEnumerable<IHealthCheck> checks) : IHealthService
{
    public async Task<IReadOnlyList<IHealthCheck>> GetChecksAsync()
    {
        var results = new List<IHealthCheck>();
        await foreach (var check in checks)
        {
            results.Add(check);
        }

        return results;
    }
}
```

To run the above code, the following NuGet packages must be added:
 - [Pure.DI](https://www.nuget.org/packages/Pure.DI)
 - [Shouldly](https://www.nuget.org/packages/Shouldly)

>[!NOTE]
>IAsyncEnumerable<T> provides efficient lazy enumeration for scenarios where you need to process many instances without loading them all into memory at once.

## Tuple

The tuples feature provides concise syntax to group multiple data elements in a lightweight data structure. The following example shows how a type can ask to inject a tuple argument into it:

```c#
using Shouldly;
using Pure.DI;

DI.Setup(nameof(Composition))
    .Bind<IEngine>().To<ElectricEngine>()
    .Bind<Coordinates>().To(() => new Coordinates(10, 20))
    .Bind<IVehicle>().To<Car>()

    // Composition root
    .Root<IVehicle>("Vehicle");

var composition = new Composition();
var vehicle = composition.Vehicle;

interface IEngine;

class ElectricEngine : IEngine;

readonly record struct Coordinates(int X, int Y);

interface IVehicle
{
    IEngine Engine { get; }
}

class Car((Coordinates StartPosition, IEngine Engine) specs) : IVehicle
{
    // The tuple 'specs' groups the initialization data (like coordinates)
    // and dependencies (like engine) into a single lightweight argument.
    public IEngine Engine { get; } = specs.Engine;
}
```

To run the above code, the following NuGet packages must be added:
 - [Pure.DI](https://www.nuget.org/packages/Pure.DI)
 - [Shouldly](https://www.nuget.org/packages/Shouldly)

>[!NOTE]
>Tuples are useful for returning multiple values from a method or grouping related dependencies without creating explicit types.

## Weak Reference

Injecting `WeakReference<T>` lets a service hold a dependency without keeping it alive — useful for large, recreatable objects such as caches. Bind the underlying type as usual and request `WeakReference<T>`; the consumer then calls `TryGetTarget`, which returns `false` once the object has been garbage-collected.

```c#
using Shouldly;
using Pure.DI;

DI.Setup(nameof(Composition))
    .Bind<ILargeCache>().To<LargeCache>()
    .Bind<IService>().To<Service>()

    // Composition root
    .Root<IService>("MyService");

var composition = new Composition();
var service = composition.MyService;

// Represents a large memory object (e.g., a cache of images or large datasets)
interface ILargeCache;

class LargeCache : ILargeCache;

interface IService;

class Service(WeakReference<ILargeCache> cache) : IService
{
    public ILargeCache? Cache =>
        // Tries to retrieve the target object from the WeakReference.
        // If the object has been collected by the GC, it returns null.
        cache.TryGetTarget(out var value)
            ? value
            : null;
}
```

To run the above code, the following NuGet packages must be added:
 - [Pure.DI](https://www.nuget.org/packages/Pure.DI)
 - [Shouldly](https://www.nuget.org/packages/Shouldly)

>[!NOTE]
>`WeakReference<T>` is useful for caching scenarios where you want to allow garbage collection when memory is constrained.

## Default BCL bindings

Pure.DI provides default bindings for commonly used .NET BCL types, so they can be injected without extra setup code.

```c#
using Shouldly;
using Pure.DI;
using System.Globalization;
using System.Security.Cryptography;

DI.Setup(nameof(Composition))
    .Root<ReportService>("ReportService");

var composition = new Composition();
var reportService = composition.ReportService;

reportService.Culture.ShouldBe(CultureInfo.CurrentCulture);
reportService.FormatProvider.ShouldBe(reportService.Culture);
reportService.CompareInfo.ShouldBe(CultureInfo.CurrentCulture.CompareInfo);
reportService.StringComparer.ShouldBe(StringComparer.Ordinal);
reportService.StringComparison.ShouldBe(StringComparison.Ordinal);
reportService.TimeProvider.ShouldBe(TimeProvider.System);
reportService.Completion.Task.CreationOptions
    .HasFlag(TaskCreationOptions.RunContinuationsAsynchronously)
    .ShouldBeTrue();

var bytes = new byte[4];
reportService.RandomNumberGenerator.GetBytes(bytes);
bytes.Length.ShouldBe(4);

class ReportService(
    CultureInfo culture,
    IFormatProvider formatProvider,
    CompareInfo compareInfo,
    StringComparer stringComparer,
    StringComparison stringComparison,
    TimeProvider timeProvider,
    TaskCompletionSource<string> completion,
    RandomNumberGenerator randomNumberGenerator)
{
    public CultureInfo Culture { get; } = culture;

    public IFormatProvider FormatProvider { get; } = formatProvider;

    public CompareInfo CompareInfo { get; } = compareInfo;

    public StringComparer StringComparer { get; } = stringComparer;

    public StringComparison StringComparison { get; } = stringComparison;

    public TimeProvider TimeProvider { get; } = timeProvider;

    public TaskCompletionSource<string> Completion { get; } = completion;

    public RandomNumberGenerator RandomNumberGenerator { get; } = randomNumberGenerator;
}
```

To run the above code, the following NuGet packages must be added:
 - [Pure.DI](https://www.nuget.org/packages/Pure.DI)
 - [Shouldly](https://www.nuget.org/packages/Shouldly)

>[!NOTE]
>Default BCL bindings can still be overridden in the composition when an application needs a different policy.

## Overriding the BCL binding

At any time, the default binding to the BCL type can be changed to your own:

```c#
using Shouldly;
using Pure.DI;

DI.Setup(nameof(Composition))
    .Bind<IMessageSender[]>().To<IMessageSender[]>(() =>
        [new EmailSender(), new SmsSender(), new EmailSender()]
    )
    .Bind<INotificationService>().To<NotificationService>()

    // Composition root
    .Root<INotificationService>("NotificationService");

var composition = new Composition();
var notificationService = composition.NotificationService;
notificationService.Senders.Length.ShouldBe(3);
notificationService.Senders[0].ShouldBeOfType<EmailSender>();
notificationService.Senders[1].ShouldBeOfType<SmsSender>();
notificationService.Senders[2].ShouldBeOfType<EmailSender>();

interface IMessageSender;

class EmailSender : IMessageSender;

class SmsSender : IMessageSender;

interface INotificationService
{
    IMessageSender[] Senders { get; }
}

class NotificationService(IMessageSender[] senders) : INotificationService
{
    public IMessageSender[] Senders { get; } = senders;
}
```

To run the above code, the following NuGet packages must be added:
 - [Pure.DI](https://www.nuget.org/packages/Pure.DI)
 - [Shouldly](https://www.nuget.org/packages/Shouldly)

>[!NOTE]
>Overriding BCL bindings allows you to provide custom implementations for standard types, enabling specialized behavior for your application.

## Service collection

The `// OnNewRoot = On` hint specifies to create a static method that will be called for each registered composition root. This method can be used, for example, to create an `IServiceCollection` object:

```c#
using Pure.DI.MS;
using Shouldly;
using Microsoft.Extensions.DependencyInjection;
using Pure.DI;

var composition = new Composition();
var serviceCollection = composition.ServiceCollection;
var serviceProvider = serviceCollection.BuildServiceProvider();
var thermostat = serviceProvider.GetRequiredService<IThermostat>();
var sensor = serviceProvider.GetRequiredKeyedService<ISensor>("LivingRoom");
thermostat.Sensor.ShouldBe(sensor);

interface ISensor;

class TemperatureSensor : ISensor;

interface IThermostat
{
    ISensor Sensor { get; }
}

class Thermostat([Tag("LivingRoom")] ISensor sensor) : IThermostat
{
    public ISensor Sensor { get; } = sensor;
}

partial class Composition : ServiceProviderFactory<Composition>
{
    public IServiceCollection ServiceCollection =>
        CreateServiceCollection(this);

    static void Setup() =>
        DI.Setup()
            .Bind<ISensor>("LivingRoom").As(Lifetime.Singleton).To<TemperatureSensor>()
            .Bind<IThermostat>().To<Thermostat>()
            .Root<ISensor>(tag: "LivingRoom")
            .Root<IThermostat>();
}
```

To run the above code, the following NuGet packages must be added:
 - [Pure.DI](https://www.nuget.org/packages/Pure.DI)
 - [Pure.DI.MS](https://www.nuget.org/packages/Pure.DI.MS)
 - [Shouldly](https://www.nuget.org/packages/Shouldly)
 - [Microsoft.Extensions.DependencyInjection](https://www.nuget.org/packages/Microsoft.Extensions.DependencyInjection)

>[!NOTE]
>This enables integration with _Microsoft.Extensions.DependencyInjection_, allowing you to leverage both DI systems together.

## Service provider

The `// ObjectResolveMethodName = GetService` hint overriding the `object Resolve(Type type)` method name in `GetService()`, allowing the `IServiceProvider` interface to be implemented in a partial class.
>[!IMPORTANT]
>Only composition roots (regular or anonymous) can be resolved through the `IServiceProvider` interface. These roots must be registered using `Root(...)` or `RootBind()` calls.

This example shows how to implement a custom `IServiceProvider` using a partial class, utilizing a specific hint to override the default `Resolve()` method name:

```c#
using Shouldly;
using Microsoft.Extensions.DependencyInjection;
using Pure.DI;

var serviceProvider = new Composition();
var orderService = serviceProvider.GetRequiredService<IOrderService>();
var logger = serviceProvider.GetRequiredService<ILogger>();

// Check that the singleton instance is correctly injected
orderService.Logger.ShouldBe(logger);

// Represents a dependency, e.g., a logging service
interface ILogger;

class ConsoleLogger : ILogger;

// Represents a service that depends on ILogger
interface IOrderService
{
    ILogger Logger { get; }
}

class OrderService(ILogger logger) : IOrderService
{
    public ILogger Logger { get; } = logger;
}

partial class Composition : IServiceProvider
{
    static void Setup() =>
        DI.Setup()
            // The following hint overrides the name of the
            // "object Resolve(Type type)" method in "GetService",
            // which implements the "IServiceProvider" interface
            .Hint(Hint.ObjectResolveMethodName, "GetService")
            .Bind<ILogger>().As(Lifetime.Singleton).To<ConsoleLogger>()
            .Bind<IOrderService>().To<OrderService>()

            // Roots are required for resolution via IServiceProvider
            .Root<ILogger>()
            .Root<IOrderService>();
}
```

To run the above code, the following NuGet packages must be added:
 - [Pure.DI](https://www.nuget.org/packages/Pure.DI)
 - [Shouldly](https://www.nuget.org/packages/Shouldly)
 - [Microsoft.Extensions.DependencyInjection](https://www.nuget.org/packages/Microsoft.Extensions.DependencyInjection)

Important Notes:
- Hint Overriding: The `ObjectResolveMethodName = GetService` hint overrides the default object `Resolve(Type type)` method name to implement `IServiceProvider` interface
- Roots: Only roots can be resolved. Use `Root(...)` or `RootBind()` calls for registration

## Service provider with scope

A composition class can implement the _Microsoft.Extensions.DependencyInjection_ scoping contracts — `IKeyedServiceProvider`, `IServiceScopeFactory`, and `IServiceScope` — with hints renaming the generated `Resolve` methods to `GetService` and `GetRequiredKeyedService`. Each `CreateScope()` call returns a new `Composition` instance, so `Scoped` bindings like `ISession` get one instance per scope, while `Singleton` bindings like `IConfiguration` are shared across all scopes.

>[!IMPORTANT]
>Only composition roots (regular or anonymous) can be resolved through the `IServiceProvider` interface. These roots must be registered using `Root(...)` or `RootBind()` calls.

```c#
using Shouldly;
using Microsoft.Extensions.DependencyInjection;
using Pure.DI;

using var composition = new Composition();

// Creates the first scope (e.g., for a web request)
using var scope1 = composition.CreateScope();
var session1 = scope1.ServiceProvider.GetRequiredService<ISession>();
var config1 = composition.GetRequiredService<IConfiguration>();

// The session must use the global configuration
session1.Configuration.ShouldBe(config1);

// Within the same scope, the session instance must be the same
session1.ShouldBe(scope1.ServiceProvider.GetRequiredService<ISession>());

// Creates the second scope
using var scope2 = composition.CreateScope();
var session2 = scope2.ServiceProvider.GetRequiredService<ISession>();
var config2 = composition.GetRequiredService<IConfiguration>();

session2.Configuration.ShouldBe(config2);
session2.ShouldBe(scope2.ServiceProvider.GetRequiredService<ISession>());

// Sessions in different scopes are different instances
session1.ShouldNotBe(session2);

// Configuration is a singleton, so it's the same instance
config1.ShouldBe(config2);

// Represents a global configuration (Singleton)
interface IConfiguration;

class Configuration : IConfiguration;

// Represents a user session (Scoped)
interface ISession : IDisposable
{
    IConfiguration Configuration { get; }
}

class Session(IConfiguration configuration) : ISession
{
    public IConfiguration Configuration { get; } = configuration;

    public void Dispose() {}
}

partial class Composition
    : IKeyedServiceProvider, IServiceScopeFactory, IServiceScope
{
    static void Setup() =>
        // The following hint overrides the name of the
        // "object Resolve(Type type)" method in "GetService",
        // which implements the "IServiceProvider" interface:
        DI.Setup()
            // The following hint overrides the name of the
            // "object Resolve(Type type)" method in "GetService",
            // which implements the "IServiceProvider" interface
            .Hint(Hint.ObjectResolveMethodName, "GetService")
            // The following hint overrides the name of the
            // "object Resolve(Type type, object tag)" method in "GetRequiredKeyedService",
            // which implements the "IKeyedServiceProvider" interface
            .Hint(Hint.ObjectResolveByTagMethodName, "GetRequiredKeyedService")
            .Bind<IConfiguration>().As(Lifetime.Singleton).To<Configuration>()
            .Bind<ISession>().As(Lifetime.Scoped).To<Session>()

            // Composition roots
            .Root<IConfiguration>()
            .Root<ISession>();

    public IServiceProvider ServiceProvider => this;

    public IServiceScope CreateScope() => new Composition(this);

    public object GetKeyedService(Type serviceType, object? serviceKey) =>
        GetRequiredKeyedService(serviceType, serviceKey);
}
```

To run the above code, the following NuGet packages must be added:
 - [Pure.DI](https://www.nuget.org/packages/Pure.DI)
 - [Shouldly](https://www.nuget.org/packages/Shouldly)
 - [Microsoft.Extensions.DependencyInjection](https://www.nuget.org/packages/Microsoft.Extensions.DependencyInjection)

>[!NOTE]
>This enables scoped service resolution compatible with _Microsoft.Extensions.DependencyInjection's_ scoping model.

## Keyed service provider

A composition class can implement `IKeyedServiceProvider` from _Microsoft.Extensions.DependencyInjection_, exposing tagged composition roots as keyed services. The `ObjectResolveMethodName` and `ObjectResolveByTagMethodName` hints rename the generated `Resolve` methods to `GetService` and `GetRequiredKeyedService`, so binding tags such as `"PayPal"` and `"Online"` become the service keys.

```c#
using Shouldly;
using Microsoft.Extensions.DependencyInjection;
using Pure.DI;

var serviceProvider = new Composition();

// Resolve the order service by key "Online".
// This service expects a dependency with the key "PayPal".
var orderService = serviceProvider.GetRequiredKeyedService<IOrderService>("Online");

// Resolve the payment gateway by key "PayPal" to verify the correct injection
var paymentGateway = serviceProvider.GetRequiredKeyedService<IPaymentGateway>("PayPal");

// Check that the expected gateway instance was injected into the order service
orderService.PaymentGateway.ShouldBe(paymentGateway);

// Payment gateway interface
interface IPaymentGateway;

// Payment gateway implementation (e.g., PayPal)
class PayPalGateway : IPaymentGateway;

// Order service interface
interface IOrderService
{
    IPaymentGateway PaymentGateway { get; }
}

// Implementation of the service for online orders.
// The [Tag("PayPal")] attribute indicates that an implementation
// of IPaymentGateway registered with the key "PayPal" should be injected.
class OnlineOrderService([Tag("PayPal")] IPaymentGateway paymentGateway) : IOrderService
{
    public IPaymentGateway PaymentGateway { get; } = paymentGateway;
}

partial class Composition : IKeyedServiceProvider
{
    static void Setup() =>
        DI.Setup()
            // The following hint overrides the name of the
            // "object Resolve(Type type)" method in "GetService",
            // which implements the "IServiceProvider" interface
            .Hint(Hint.ObjectResolveMethodName, "GetService")
            // The following hint overrides the name of the
            // "object Resolve(Type type, object tag)" method in "GetRequiredKeyedService",
            // which implements the "IKeyedServiceProvider" interface
            .Hint(Hint.ObjectResolveByTagMethodName, "GetRequiredKeyedService")

            // Register PayPalGateway as a singleton with the key "PayPal"
            .Bind<IPaymentGateway>("PayPal").As(Lifetime.Singleton).To<PayPalGateway>()

            // Register OnlineOrderService with the key "Online"
            .Bind<IOrderService>("Online").To<OnlineOrderService>()

            // Composition roots available by keys
            .Root<IPaymentGateway>(tag: "PayPal")
            .Root<IOrderService>(tag: "Online");

    public object GetKeyedService(Type serviceType, object? serviceKey) =>
        GetRequiredKeyedService(serviceType, serviceKey);
}
```

To run the above code, the following NuGet packages must be added:
 - [Pure.DI](https://www.nuget.org/packages/Pure.DI)
 - [Shouldly](https://www.nuget.org/packages/Shouldly)
 - [Microsoft.Extensions.DependencyInjection](https://www.nuget.org/packages/Microsoft.Extensions.DependencyInjection)

>[!NOTE]
>This enables compatibility with Microsoft's DI container ecosystem when using keyed service resolution.

## Default Func with ReadOnlySpan

Pure.DI can generate the standard `Func<ReadOnlySpan<char>, T>` factory automatically. The runtime span argument is kept as a local value inside the generated delegate invocation, so no manual `ctx.Override(...)` call and no `lock (ctx.Lock)` block are required.

```c#
using Shouldly;
using Pure.DI;
using System;

DI.Setup(nameof(Composition))

    // Composition root
    .Root<Func<ReadOnlySpan<char>, Parser>>("ParserFactory");

var composition = new Composition();
var parser = composition.ParserFactory("Hello".AsSpan());

parser.Value.ShouldBe("Hello");

class Parser
{
    private string _value = "";

    [Ordinal]
    public void Initialize(ReadOnlySpan<char> text)
    {
        _value = text.ToString();
    }

    public string Value => _value;
}
```

To run the above code, the following NuGet packages must be added:
 - [Pure.DI](https://www.nuget.org/packages/Pure.DI)
 - [Shouldly](https://www.nuget.org/packages/Shouldly)

Use this default `Func` binding when the standard delegate shape is enough. Use a custom delegate factory with explicit `ctx.Override<T>(...)` only when you need a custom delegate type or additional factory logic.

## Allows ref struct factory

A delegate factory can accept stack-only values when the value is consumed immediately inside the same invocation. For custom generic delegate APIs that use `where T : allows ref struct`, pass the delegate argument through `ctx.Override<T>(...)` or `ctx.Let<T>(...)`, resolve the target immediately, and keep the override plus injection inside `lock (ctx.Lock)` when thread safety is enabled.

```c#
using Shouldly;
using Pure.DI;
using System;

DI.Setup(nameof(Composition))
    .Bind<ParserFactory<ReadOnlySpan<char>>>().To(ctx => new ParserFactory<ReadOnlySpan<char>>(text =>
    {
        lock (ctx.Lock)
        {
            ctx.Override<ReadOnlySpan<char>>(text);
            ctx.Inject<Parser<ReadOnlySpan<char>>>(out var parser);
            return parser.Initialized;
        }
    }))

    // Composition root
    .Root<ParserFactory<ReadOnlySpan<char>>>("ParserFactory");

var composition = new Composition();
var initialized = composition.ParserFactory("Hello".AsSpan());

initialized.ShouldBeTrue();

delegate bool ParserFactory<in T>(T text)
    where T : allows ref struct;

class Parser<T>
    where T : allows ref struct
{
    private bool _initialized;

    [Ordinal]
    public void Initialize(T text)
    {
        _ = text;
        _initialized = true;
    }

    public bool Initialized => _initialized;
}
```

To run the above code, the following NuGet packages must be added:
 - [Pure.DI](https://www.nuget.org/packages/Pure.DI)
 - [Shouldly](https://www.nuget.org/packages/Shouldly)

This manual factory pattern keeps `ReadOnlySpan<T>` and other stack-only values inside the current synchronous frame. Pure.DI reports `DIE049` if the value is captured by a nested or returned delegate, and `DIW013` if the stack-only override is not synchronized while thread safety is enabled.
When the standard delegate shape is enough, prefer the generated default `Func<ReadOnlySpan<char>, T>` binding. It uses local values in the generated delegate invocation and does not require a manual `lock`.
The fluent contract API accepts ref-like type arguments in `Bind<T>()`, `RootBind<T>()`, `Root<T>()`, and `DefaultLifetime<T>()`. The `Transient<T>()`, `PerResolve<T>()`, and `PerBlock<T>()` shortcuts support ref-like implementations and factory results because generated instances remain local to root execution.
`Singleton<T>()` and `Scoped<T>()` keep their implementation and factory-result type heap-safe. Their parameterized factory overloads accept ref-like dependency types so Pure.DI can apply its stored-lifetime validation and report `DIE046` instead of failing earlier with the C# generic-argument diagnostic `CS9244`.

## Method injection for a hot path

Method injection is useful when a service has stable dependencies but the hot-path input changes on every call. Instead of storing request state in a heap object, pass the per-call value as a root argument and consume it immediately in an initialization method.
The route matcher below has a singleton-like route table and receives a `ReadOnlySpan<char>` request path for each call. The generated root method passes the span into `Match(...)`, so the stack-only value stays inside the current call frame and is not stored in a field or property.

```c#
using Shouldly;
using Pure.DI;
using System;

DI.Setup(nameof(Composition))
    .Bind<IRouteTable>().To<RouteTable>()
    .Bind<IRouteMatcher>().To<RouteMatcher>()
    .RootArg<ReadOnlySpan<char>>("path")
    .Root<IRouteMatcher>("CreateMatcher");

var composition = new Composition();

var matcher = composition.CreateMatcher("/orders/42".AsSpan());
matcher.Route.ShouldBe("orders");

interface IRouteTable
{
    string Find(ReadOnlySpan<char> path);
}

sealed class RouteTable : IRouteTable
{
    public string Find(ReadOnlySpan<char> path) =>
        path.StartsWith("/orders/".AsSpan(), StringComparison.Ordinal)
            ? "orders"
            : "not-found";
}

interface IRouteMatcher
{
    string Route { get; }
}

sealed class RouteMatcher(IRouteTable routeTable) : IRouteMatcher
{
    public string Route { get; private set; } = "";

    [Ordinal(0)]
    public void Match(ReadOnlySpan<char> path) =>
        Route = routeTable.Find(path);
}
```

To run the above code, the following NuGet packages must be added:
 - [Pure.DI](https://www.nuget.org/packages/Pure.DI)
 - [Shouldly](https://www.nuget.org/packages/Shouldly)

This is a good shape for parsers, routers, protocol decoders, validation pipelines, and other code that reads request data immediately. Keep the method side-effect small and do not store `Span<T>`/`ReadOnlySpan<T>` in the created object; Pure.DI reports diagnostics when stack-only values are routed to storage-like injection sites.

## ArrayPool buffer

Use `ArrayPool<T>` when a hot path needs temporary buffers whose size is too large or too variable for `stackalloc`. Pure.DI supports `ArrayPool<T>` out of the box, so the setup does not need to bind `ArrayPool<byte>.Shared` manually; request `ArrayPool<byte>` like any other dependency.
In this example a CSV export endpoint creates a per-request `ExportBuffer`. The buffer owner receives the shared `ArrayPool<byte>` from Pure.DI, rents an array using application options, exposes only `Memory<byte>` to the exporter, and returns the array when the `Owned<IReportExporter>` operation scope is disposed.

```c#
using Shouldly;
using Pure.DI;
using System.Buffers;
using System.Text;

DI.Setup(nameof(Composition))
    .Bind().To(_ => new ExportBufferOptions(Size: 256))
    .Bind().To<ExportBuffer>()
    .Bind<IReportExporter>().To<CsvReportExporter>()
    .Root<Owned<IReportExporter>>("Exporter");

var composition = new Composition();
var exporter = composition.Exporter;

var bytesWritten = exporter.Value.Export(
    [
        new Order(17, 42.50m),
        new Order(18, 13.25m)
    ]);

bytesWritten.ShouldBeGreaterThan(0);
exporter.Value.Buffer.Returned.ShouldBeFalse();

readonly record struct Order(int Id, decimal Total);

readonly record struct ExportBufferOptions(int Size);

interface IReportExporter
{
    ExportBuffer Buffer { get; }

    int Export(IReadOnlyList<Order> orders);
}

sealed class CsvReportExporter(ExportBuffer buffer) : IReportExporter
{
    public ExportBuffer Buffer => buffer;

    public int Export(IReadOnlyList<Order> orders)
    {
        var writer = new ArrayBufferWriter<byte>(buffer.Memory.Length);
        foreach (var order in orders)
        {
            var line = $"order-{order.Id},{order.Total:0.00}\n";
            writer.Write(Encoding.UTF8.GetBytes(line));
        }

        writer.WrittenSpan.CopyTo(buffer.Memory.Span);
        return writer.WrittenCount;
    }
}

sealed class ExportBuffer(
    ArrayPool<byte> pool,
    ExportBufferOptions options)
    : IDisposable
{
    private byte[]? _buffer = pool.Rent(options.Size);

    public Memory<byte> Memory => _buffer;

    public bool Returned => _buffer is null;

    public void Dispose()
    {
        if (_buffer is {} buffer)
        {
            Array.Clear(buffer);
            pool.Return(buffer);
            _buffer = null;
        }
    }
}
```

To run the above code, the following NuGet packages must be added:
 - [Pure.DI](https://www.nuget.org/packages/Pure.DI)
 - [Shouldly](https://www.nuget.org/packages/Shouldly)

This pattern is useful for serialization, compression, protocol framing, and batch export pipelines. Keep the owner lifetime short, clear sensitive buffers before returning them, and do not store the rented `Memory<T>` after the owner is disposed.
The important DI detail is ownership: bind or auto-bind the owner as the dependency that is tracked and disposed, not the raw array. The pool itself is a built-in BCL dependency; the owner is the application-specific object that defines rent size, cleanup, and return rules. `Owned<T>` keeps that cleanup tied to one root call instead of the whole composition.

## Zero-copy network packet parsing

Network servers commonly receive an owned `byte[]` from a socket, pipeline adapter, or message broker and need to inspect it without allocating another buffer. A `byte[]` root argument can satisfy a `ReadOnlySpan<byte>` method-injection target: Pure.DI generates a root method that accepts the caller-owned array and applies the compiler-provided span conversion when invoking the parser.
The packet handler below combines a stable message registry with one frame supplied for the current operation. `Decode(...)` validates the header, reads the message identifier in network byte order, and computes a payload checksum directly over the original array. The stack-only view is consumed immediately; only parsed values are retained by the heap object.

```c#
using Shouldly;
using Pure.DI;
using System;
using System.Buffers.Binary;

DI.Setup(nameof(Composition))
    .Bind<IMessageRegistry>().To<MessageRegistry>()
    .Bind<IPacketHandler>().To<PacketHandler>()
    .RootArg<byte[]>("frame")
    .Root<IPacketHandler>("Handle");

var frame = new byte[]
{
    0x00, 0x2A, // Message id 42, big endian
    0x03,       // Payload length
    0x10, 0x20, 0x30
};

var composition = new Composition();
var packet = composition.Handle(frame);

packet.MessageName.ShouldBe("OrderAccepted");
packet.PayloadLength.ShouldBe(3);
packet.PayloadChecksum.ShouldBe(0x60);

interface IMessageRegistry
{
    string GetName(ushort messageId);
}

sealed class MessageRegistry : IMessageRegistry
{
    public string GetName(ushort messageId) =>
        messageId == 42 ? "OrderAccepted" : "Unknown";
}

interface IPacketHandler
{
    string MessageName { get; }

    int PayloadLength { get; }

    int PayloadChecksum { get; }
}

sealed class PacketHandler(IMessageRegistry registry) : IPacketHandler
{
    public string MessageName { get; private set; } = "";

    public int PayloadLength { get; private set; }

    public int PayloadChecksum { get; private set; }

    [Ordinal(0)]
    public void Decode(ReadOnlySpan<byte> frame)
    {
        if (frame.Length < 3)
        {
            throw new ArgumentException("The packet header is incomplete.", nameof(frame));
        }

        var payloadLength = frame[2];
        if (frame.Length != payloadLength + 3)
        {
            throw new ArgumentException("The packet payload length is invalid.", nameof(frame));
        }

        var messageId = BinaryPrimitives.ReadUInt16BigEndian(frame);
        var checksum = 0;
        foreach (var value in frame[3..])
        {
            checksum += value;
        }

        MessageName = registry.GetName(messageId);
        PayloadLength = payloadLength;
        PayloadChecksum = checksum;
    }
}
```

To run the above code, the following NuGet packages must be added:
 - [Pure.DI](https://www.nuget.org/packages/Pure.DI)
 - [Shouldly](https://www.nuget.org/packages/Shouldly)

This shape is useful for socket protocols, telemetry ingestion, binary file headers, and queue consumers. The caller keeps ownership of the array and may return it to a pool after the root call completes. Do not store the span or the original pooled array in the handler unless ownership is explicitly transferred; keep parsing synchronous and copy only the fields that must outlive the call.
Unlike regular `Span<T>` collection injection, this path does not build a collection from element bindings. Pure.DI detects an implicit span conversion from an existing root argument and reuses that value. Ordinary small value-type span collections continue to use the generator's `stackalloc` optimization.
The conversion matrix follows the [official C# 14 first-class Span types specification](https://learn.microsoft.com/en-us/dotnet/csharp/language-reference/proposals/csharp-14.0/first-class-span-types).

## Object pool

Object pools help when a service is expensive to allocate but can be reset and reused safely. A pool keeps a small set of warm objects and gives each request a short-lived lease that returns the instance when disposed.
Here a notification pipeline rents an `EmailTemplateRenderer` for every message. The renderer owns reusable buffers that are cleared before it goes back to the pool, so the hot path avoids repeatedly allocating the renderer and its internal state.

```c#
using Shouldly;
using Pure.DI;
using static Pure.DI.Lifetime;

DI.Setup(nameof(Composition))
    .Bind().As(Singleton).To<RendererPool>()
    .Bind().To<RendererLease>()
    .Bind<INotificationComposer>().To<NotificationComposer>()
    .Root<INotificationComposer>("Composer");

var composition = new Composition();
var composer1 = composition.Composer;
using var composer2 = composition.Composer;

composer1.Compose("Ada", "Ready").ShouldBe("Hello Ada, Ready");
composer2.Compose("Linus", "Done").ShouldBe("Hello Linus, Done");

composer1.Lease.Renderer.ShouldNotBe(composer2.Lease.Renderer);

interface INotificationComposer : IDisposable
{
    RendererLease Lease { get; }

    string Compose(string userName, string message);
}

sealed class NotificationComposer(RendererLease lease) : INotificationComposer
{
    public RendererLease Lease => lease;

    public string Compose(string userName, string message) =>
        lease.Renderer.Render(userName, message);

    public void Dispose() => lease.Dispose();
}

sealed class RendererLease(RendererPool pool) : IDisposable
{
    public EmailTemplateRenderer Renderer { get; } = pool.Rent();

    public void Dispose()
    {
        Renderer.Reset();
        pool.Return(Renderer);
    }
}

sealed class RendererPool
{
    private readonly Stack<EmailTemplateRenderer> _renderers = [];

    public EmailTemplateRenderer Rent() =>
        _renderers.Count > 0 ? _renderers.Pop() : new EmailTemplateRenderer();

    public void Return(EmailTemplateRenderer renderer) =>
        _renderers.Push(renderer);
}

sealed class EmailTemplateRenderer
{
    private readonly List<string> _parts = [];

    public string Render(string userName, string message)
    {
        _parts.Add("Hello ");
        _parts.Add(userName);
        _parts.Add(", ");
        _parts.Add(message);
        return string.Concat(_parts);
    }

    public void Reset() => _parts.Clear();
}
```

To run the above code, the following NuGet packages must be added:
 - [Pure.DI](https://www.nuget.org/packages/Pure.DI)
 - [Shouldly](https://www.nuget.org/packages/Shouldly)

This pattern fits serializers, encoders, template renderers, parsers, and compression helpers. It is not a replacement for regular DI lifetimes: only pool objects that have a clear reset rule, are not used concurrently while leased, and do not keep request-specific references after `Reset()`.
The composition owns the pool as a singleton, while each root call creates a lightweight lease. That keeps reuse explicit and avoids leaking pooled instances outside the operation scope.

## Struct dependency

Small immutable value-type services are useful on hot paths where the dependency represents a policy, a formatter, or a calculator with no identity and no shared mutable state. Pure.DI can compose those values directly, so consumers receive a strongly typed value without runtime lookup.
This example uses a `readonly struct` shipping-price policy in a checkout quote calculator. The policy is cheap to copy, deterministic, and has no lifetime state, which makes it a good fit for value semantics.

```c#
using Shouldly;
using Pure.DI;

DI.Setup(nameof(Composition))
    .Bind().To<ShippingPolicy>()
    .Bind<IQuoteCalculator>().To<QuoteCalculator>()
    .Root<IQuoteCalculator>("Calculator");

var composition = new Composition();
var calculator = composition.Calculator;

calculator.GetTotal(new Cart(120m, 2.5m)).ShouldBe(125.99m);

readonly record struct Cart(decimal Subtotal, decimal WeightKg);

readonly struct ShippingPolicy
{
    public decimal Calculate(decimal weightKg) =>
        weightKg <= 1m ? 2.99m : 5.99m;
}

interface IQuoteCalculator
{
    decimal GetTotal(Cart cart);
}

sealed class QuoteCalculator(ShippingPolicy shippingPolicy) : IQuoteCalculator
{
    public decimal GetTotal(Cart cart) =>
        cart.Subtotal + shippingPolicy.Calculate(cart.WeightKg);
}
```

To run the above code, the following NuGet packages must be added:
 - [Pure.DI](https://www.nuget.org/packages/Pure.DI)
 - [Shouldly](https://www.nuget.org/packages/Shouldly)

Prefer this pattern for tiny stateless rules and calculations. Do not turn large mutable objects into structs just to avoid allocation; copying a large struct can be more expensive than allocating one object. Keep value-type services small, immutable, and obvious.

## ValueTask root

A root can return `ValueTask<T>` when the caller naturally awaits the result but most executions complete synchronously. This avoids allocating a `Task<T>` for the fast path while still allowing the same API to grow into asynchronous initialization later.
The example models a feature flag snapshot used by request routing. The snapshot is normally available from an in-memory cache, so the composition root returns a completed `ValueTask<IFeatureSnapshot>` and the caller can await it without forcing a heap allocation for the common case.

```c#
using Shouldly;
using Pure.DI;

DI.Setup(nameof(Composition))
    .Bind<IFeatureSnapshot>().To<FeatureSnapshot>()
    .Root<ValueTask<IFeatureSnapshot>>("GetSnapshotAsync");

var composition = new Composition();

var snapshot = await composition.GetSnapshotAsync;
snapshot.IsEnabled("checkout-v2").ShouldBeTrue();

interface IFeatureSnapshot
{
    bool IsEnabled(string name);
}

sealed class FeatureSnapshot : IFeatureSnapshot
{
    private readonly HashSet<string> _enabled =
    [
        "checkout-v2",
        "new-search"
    ];

    public bool IsEnabled(string name) => _enabled.Contains(name);
}
```

To run the above code, the following NuGet packages must be added:
 - [Pure.DI](https://www.nuget.org/packages/Pure.DI)
 - [Shouldly](https://www.nuget.org/packages/Shouldly)

Use `ValueTask<T>` for roots that are awaited frequently and usually complete synchronously. Keep the usual `ValueTask<T>` rules: await it once, do not store it for later, and use `Task<T>` when the result is naturally shared or awaited multiple times.
Pure.DI still generates regular strongly typed code; the performance benefit comes from choosing an allocation-friendly asynchronous shape at the root boundary.

## ThreadSafe Off for single-thread composition

Pure.DI generates thread-safe code by default because composition instances are often shared. When a composition is created and used on one thread, such as inside a command-line import step, a game-loop setup phase, or a short-lived benchmark harness, the synchronization path can be disabled explicitly.
This example builds a report import pipeline that is created, used, and discarded inside one job. `ThreadSafe = Off` removes generated locking for composition-owned cached instances, while the application keeps the single-threaded ownership rule at the boundary.

```c#
using Shouldly;
using Pure.DI;
using static Pure.DI.Hint;
using static Pure.DI.Lifetime;

DI.Setup(nameof(Composition))
    .Hint(ThreadSafe, "Off")
    .Bind().As(Singleton).To<ImportCache>()
    .Bind<IImportJob>().To<ImportJob>()
    .Root<IImportJob>("Job");

var composition = new Composition();
var job = composition.Job;

job.Import(["A-100", "A-100", "B-200"]).ShouldBe(2);

interface IImportJob
{
    int Import(IReadOnlyList<string> productCodes);
}

sealed class ImportJob(ImportCache cache) : IImportJob
{
    public int Import(IReadOnlyList<string> productCodes)
    {
        foreach (var code in productCodes)
        {
            cache.SeenCodes.Add(code);
        }

        return cache.SeenCodes.Count;
    }
}

sealed class ImportCache
{
    public HashSet<string> SeenCodes { get; } = [];
}
```

To run the above code, the following NuGet packages must be added:
 - [Pure.DI](https://www.nuget.org/packages/Pure.DI)
 - [Shouldly](https://www.nuget.org/packages/Shouldly)

Use this only when the composition instance is not shared across threads. If delegates, factories, or roots can be invoked concurrently, keep thread safety enabled or synchronize the critical factory section yourself with `ctx.Lock`.

## Advanced interception

Advanced interception is useful when cross-cutting behavior is required on a hot path and the proxy factory itself must not become the bottleneck. Instead of asking Castle DynamicProxy to discover the construction path repeatedly, this scenario compiles and caches a strongly typed proxy factory per service type.
The example wraps business services with a logging interceptor and caches the proxy creation delegate in a generic nested `ProxyFactory<T>`. The generated Pure.DI graph still creates the target services directly, while the interception hook applies the cached proxy layer after each dependency is built.

```c#
using Shouldly;
using Castle.DynamicProxy;
using System.Collections.Immutable;
using System.Linq.Expressions;
using System.Runtime.CompilerServices;
using Pure.DI;

// OnDependencyInjection = On
DI.Setup(nameof(Composition))
    .Bind().To<DataService>()
    .Bind().To<BusinessService>()
    .Root<IBusinessService>("BusinessService");

var log = new List<string>();
var composition = new Composition(log);
var businessService = composition.BusinessService;

// Use the services to verify interception.
businessService.Process();
businessService.DataService.Count();

log.ShouldBe(
    [
        "Process returns Processed",
        "get_DataService returns Castle.Proxies.IDataServiceProxy",
        "Count returns 55"
    ]);

public interface IDataService
{
    int Count();
}

class DataService : IDataService
{
    public int Count() => 55;
}

public interface IBusinessService
{
    IDataService DataService { get; }

    string Process();
}

class BusinessService(IDataService dataService) : IBusinessService
{
    public IDataService DataService { get; } = dataService;

    public string Process() => "Processed";
}

internal partial class Composition : IInterceptor
{
    private readonly List<string> _log;
    private static readonly IProxyBuilder ProxyBuilder = new DefaultProxyBuilder();
    private readonly IInterceptor[] _interceptors;

    public Composition(List<string> log)
    {
        _log = log;
        _interceptors = [this];
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private partial T OnDependencyInjection<T>(
        in T value,
        object? tag,
        Lifetime lifetime)
    {
        if (typeof(T).IsValueType)
        {
            return value;
        }

        return ProxyFactory<T>.GetFactory(ProxyBuilder)(
            value,
            _interceptors);
    }

    public void Intercept(IInvocation invocation)
    {
        invocation.Proceed();
        _log.Add($"{invocation.Method.Name} returns {invocation.ReturnValue}");
    }

    private static class ProxyFactory<T>
    {
        private static Func<T, IInterceptor[], T>? _factory;

        public static Func<T, IInterceptor[], T> GetFactory(IProxyBuilder proxyBuilder) =>
            _factory ?? CreateFactory(proxyBuilder);

        private static Func<T, IInterceptor[], T> CreateFactory(IProxyBuilder proxyBuilder)
        {
            // Compiles a delegate to create a proxy for the performance boost
            var proxyType = proxyBuilder.CreateInterfaceProxyTypeWithTargetInterface(
                typeof(T),
                Type.EmptyTypes,
                ProxyGenerationOptions.Default);
            var ctor = proxyType.GetConstructors()
                .Single(i => i.GetParameters().Length == 2);
            var instance = Expression.Parameter(typeof(T));
            var interceptors = Expression.Parameter(typeof(IInterceptor[]));
            var newProxyExpression = Expression.New(ctor, interceptors, instance);
            return _factory = Expression.Lambda<Func<T, IInterceptor[], T>>(
                    newProxyExpression,
                    instance,
                    interceptors)
                .Compile();
        }
    }
}
```

To run the above code, the following NuGet packages must be added:
 - [Pure.DI](https://www.nuget.org/packages/Pure.DI)
 - [Shouldly](https://www.nuget.org/packages/Shouldly)
 - [Castle.DynamicProxy](https://www.nuget.org/packages/Castle.DynamicProxy)

>[!NOTE]
>Advanced interception provides high-performance proxy generation for scenarios where runtime interception overhead must be minimized.
Use this only when decorators are not expressive enough or when an existing interception ecosystem is required. For simple cross-cutting behavior, decorators are easier to read and cheaper to reason about.

## Factory without closure capture

Factory delegates often appear in hot object creation paths. Keep them deterministic and allocation-friendly by taking dependencies as factory parameters instead of capturing outer variables. Pure.DI can see those parameters as dependencies and generate the code that supplies them.
The receipt formatter below needs a currency formatter and tax policy. Both are declared as lambda parameters, so the factory does not close over mutable setup-local state and the generated graph remains explicit.

```c#
using Shouldly;
using Pure.DI;
using System.Globalization;

DI.Setup(nameof(Composition))
    .Bind().To<CurrencyFormatter>()
    .Bind().To<TaxPolicy>()
    .Bind<IReceiptFormatter>().To((
        CurrencyFormatter currency,
        TaxPolicy tax) => new ReceiptFormatter(currency, tax))
    .Root<IReceiptFormatter>("Formatter");

var composition = new Composition();
var formatter = composition.Formatter;

formatter.Format(100m).ShouldBe("$120.00");

interface IReceiptFormatter
{
    string Format(decimal subtotal);
}

sealed class ReceiptFormatter(
    CurrencyFormatter currency,
    TaxPolicy tax)
    : IReceiptFormatter
{
    public string Format(decimal subtotal) =>
        currency.Format(tax.Apply(subtotal));
}

sealed class CurrencyFormatter
{
    public string Format(decimal value) =>
        $"${value.ToString("0.00", CultureInfo.InvariantCulture)}";
}

readonly struct TaxPolicy
{
    public decimal Apply(decimal subtotal) => subtotal * 1.20m;
}
```

To run the above code, the following NuGet packages must be added:
 - [Pure.DI](https://www.nuget.org/packages/Pure.DI)
 - [Shouldly](https://www.nuget.org/packages/Shouldly)

This is most useful when a factory adds a small construction decision or validates dependencies before creating an object. If the factory needs runtime values, prefer root arguments or `Func<TArg, TResult>` instead of capturing variables from the setup method.

## Thread-safe overrides

When a factory delegate can be invoked from several threads at once — as with the `Func<int, int, IOrderHandler>` called in parallel here — its `ctx.Override(...)` calls must be synchronized. Wrap the overrides together with the subsequent `ctx.Inject(...)` in a `lock (ctx.Lock)` block so that each object graph is built with its own override values and parallel invocations don't overwrite each other.

```c#
using Shouldly;
using Pure.DI;
using System.Collections.Immutable;

DI.Setup(nameof(Composition))
    .Bind("Global").To(() => new ProcessingToken("TOKEN-123"))
    .Bind().As(Lifetime.Singleton).To<TimeProvider>()
    .Bind().To<Func<int, int, IOrderHandler>>(ctx =>
        (orderId, customerId) => {
            // Retrieves a global processing token to be passed to the handler
            ctx.Inject("Global", out ProcessingToken token);

            // The factory is invoked in parallel, so we must lock
            // the context to safely perform overrides for the specific graph
            lock (ctx.Lock)
            {
                // Overrides the 'int' dependency (OrderId)
                ctx.Override(orderId);

                // Overrides the tagged 'int' dependency (CustomerId)
                ctx.Override(customerId, "customer");

                // Overrides the 'string' dependency (TraceId)
                ctx.Override($"Order:{orderId}-Cust:{customerId}");

                // Overrides the 'ProcessingToken' dependency with the injected value
                ctx.Override(token);

                // Creates the handler with the overridden dependencies
                ctx.Inject<OrderHandler>(out var handler);
                return handler;
            }
        })
    .Bind().To<OrderBatchProcessor>()

    // Composition root
    .Root<IOrderBatchProcessor>("OrderProcessor");

var composition = new Composition();
var orderProcessor = composition.OrderProcessor;

orderProcessor.Handlers.Length.ShouldBe(100);
for (var i = 0; i < 100; i++)
{
    orderProcessor.Handlers.Count(h => h.OrderId == i).ShouldBe(1);
}

record ProcessingToken(string Value);

interface ITimeProvider
{
    DateTimeOffset Now { get; }
}

class TimeProvider : ITimeProvider
{
    public DateTimeOffset Now => DateTimeOffset.Now;
}

interface IOrderHandler
{
    string TraceId { get; }

    int OrderId { get; }

    int CustomerId { get; }
}

class OrderHandler(
    string traceId,
    ITimeProvider timeProvider,
    int orderId,
    [Tag("customer")] int customerId,
    ProcessingToken token)
    : IOrderHandler
{
    public string TraceId => traceId;

    public int OrderId => orderId;

    public int CustomerId => customerId;
}

interface IOrderBatchProcessor
{
    ImmutableArray<IOrderHandler> Handlers { get; }
}

class OrderBatchProcessor(Func<int, int, IOrderHandler> orderHandlerFactory)
    : IOrderBatchProcessor
{
    public ImmutableArray<IOrderHandler> Handlers { get; } =
    [
        // Simulates parallel processing of orders
        ..Enumerable.Range(0, 100)
            .AsParallel()
            .Select(i => orderHandlerFactory(i, 99))
    ];
}
```

To run the above code, the following NuGet packages must be added:
 - [Pure.DI](https://www.nuget.org/packages/Pure.DI)
 - [Shouldly](https://www.nuget.org/packages/Shouldly)

The same rule applies to stack-only values such as `Span<T>`, `ReadOnlySpan<T>`, and generic `T` with `where T : allows ref struct`. Pure.DI reports `DIW013` when such values are overridden in a factory delegate without `lock (ctx.Lock)` while thread safety is enabled.
>[!IMPORTANT]
>Thread-safe overrides are essential when composition instances are shared across multiple threads or when parallel resolution is required.

## Non-boxing union result

The compact `union` declaration stores its value as `object`, which is convenient but boxes value-type cases. On a hot path, a custom union can keep each value-type case in a dedicated field and expose the optional `HasValue`/`TryGetValue` pattern. Pure.DI still treats the selected case as the union DI contract through the compiler's implicit union conversion; no reflection, runtime lookup, or wrapper allocation is introduced by the composition.
This cache lookup example binds the value-type `CacheHit` case to the `CacheLookupResult` contract. The generated root constructs `CacheHit` directly and the C# compiler invokes the union creation constructor. The consumer reads it with `TryGetValue(out CacheHit)`, so the normal success path does not access the object-typed `Value` fallback and does not box the case.

```c#
using Shouldly;
using Pure.DI;

DI.Setup(nameof(Composition))
    // CacheHit is a value-type case of CacheLookupResult
    .Bind<CacheLookupResult>().To<CacheHit>()
    .Bind<int>("product id").To(() => 42)
    .Bind<decimal>("price").To(() => 19.95m)
    .Root<CacheLookupResult>("CachedProduct");

var result = new Composition().CachedProduct;

result.TryGetValue(out CacheHit hit).ShouldBeTrue();
hit.ProductId.ShouldBe(42);
hit.Price.ShouldBe(19.95m);

readonly record struct CacheHit(
    [Tag("product id")] int ProductId,
    [Tag("price")] decimal Price);

readonly record struct CacheMiss(int ProductId);

// A custom union stores value-type cases directly. Value is the required
// general fallback; TryGetValue is the allocation-free access path.
[System.Runtime.CompilerServices.Union]
readonly struct CacheLookupResult : System.Runtime.CompilerServices.IUnion
{
    private readonly byte _kind;
    private readonly CacheHit _hit;
    private readonly CacheMiss _miss;

    public CacheLookupResult(CacheHit hit) =>
        (_kind, _hit, _miss) = (1, hit, default);

    public CacheLookupResult(CacheMiss miss) =>
        (_kind, _hit, _miss) = (2, default, miss);

    public object? Value => _kind switch
    {
        1 => _hit,
        2 => _miss,
        _ => null
    };

    public bool HasValue => _kind != 0;

    public bool TryGetValue(out CacheHit value)
    {
        value = _hit;
        return _kind == 1;
    }

    public bool TryGetValue(out CacheMiss value)
    {
        value = _miss;
        return _kind == 2;
    }
}
```

To run the above code, the following NuGet packages must be added:
 - [Pure.DI](https://www.nuget.org/packages/Pure.DI)
 - [Shouldly](https://www.nuget.org/packages/Shouldly)

>[!NOTE]
>The `Value` property is mandatory for a custom union and necessarily boxes value-type cases when it is read. Use the strongly typed `TryGetValue` members on performance-sensitive paths; reserve `Value` for diagnostics and general-purpose inspection.
This scenario requires the preview language version and .NET 11 Preview 5 or later, where the union runtime types are available.

## Generics

Generic types are supported out of the box: a single binding like `Bind<IRepository<TT>>().To<Repository<TT>>()` covers `IRepository<User>`, `IRepository<Order>` and any other instantiation used in the object graph. Since Pure.DI is a source generator, each of them is turned into concrete, reflection-free code at compile time.
>[!IMPORTANT]
>Instead of open generic types, as in classical DI container libraries, regular generic types with `marker` types as type parameters are used here. Such "marker" types allow to define dependency graph more precisely.

For the case of `IRepository<TT>`, `TT` is a `marker` type, which allows the usual `IRepository<TT>` to be used instead of an open generic type like `IRepository<>`. This makes it easy to bind generic types by specifying `marker` types such as `TT`, `TT1`, etc. as parameters of generic types:

```c#
using Shouldly;
using Pure.DI;

DI.Setup(nameof(Composition))
    // Disable Resolve methods to keep the public API minimal
    .Hint(Hint.Resolve, "Off")
    // Binding a generic interface to a generic implementation
    // using the marker type TT. This allows Pure.DI to match
    // IRepository<User> to Repository<User>, IRepository<Order> to Repository<Order>, etc.
    .Bind<IRepository<TT>>().To<Repository<TT>>()
    .Bind<IDataService>().To<DataService>()

    // Composition root
    .Root<IDataService>("DataService");

var composition = new Composition();
var dataService = composition.DataService;

// Verifying that the correct generic types were injected
dataService.Users.ShouldBeOfType<Repository<User>>();
dataService.Orders.ShouldBeOfType<Repository<Order>>();

interface IRepository<T>;

class Repository<T> : IRepository<T>;

// Domain entities
record User;

record Order;

interface IDataService
{
    IRepository<User> Users { get; }

    IRepository<Order> Orders { get; }
}

class DataService(
    IRepository<User> users,
    IRepository<Order> orders)
    : IDataService
{
    public IRepository<User> Users { get; } = users;

    public IRepository<Order> Orders { get; } = orders;
}
```

To run the above code, the following NuGet packages must be added:
 - [Pure.DI](https://www.nuget.org/packages/Pure.DI)
 - [Shouldly](https://www.nuget.org/packages/Shouldly)

Actually, the property `DataService` looks like:
```c#
public IDataService DataService
{
  get
  {
    return new DataService(new Repository<User>(), new Repository<Order>());
  }
}
```
Even in this simple scenario, it is not possible to precisely define the binding of an abstraction to its implementation using open generic types:
```c#
.Bind(typeof(IMap<,>)).To(typeof(Map<,>))
```
You can try to match them by order or by name derived from the .NET type reflection. But this is not reliable, since order and name matching is not guaranteed. For example, there is some interface with two arguments of type _key and _value_. But in its implementation the sequence of type arguments is mixed up: first comes the _value_ and then the _key_ and the names do not match:
```c#
class Map<TV, TK>: IMap<TKey, TValue> { }
```
At the same time, the marker types `TT1` and `TT2` handle this easily. They determine the exact correspondence between the type arguments in the interface and its implementation:
```c#
.Bind<IMap<TT1, TT2>>().To<Map<TT2, TT1>>()
```
The first argument of the type in the interface, corresponds to the second argument of the type in the implementation and is a _key_. The second argument of the type in the interface, corresponds to the first argument of the type in the implementation and is a _value_. This is a simple example. Obviously, there are plenty of more complex scenarios where tokenized types will be useful.
Marker types are regular .NET types marked with a special attribute, such as:
```c#
[GenericTypeArgument]
internal abstract class TT1 { }

[GenericTypeArgument]
internal abstract class TT2 { }
```
This way you can easily create your own, including making them fit the constraints on the type parameter, for example:
```c#
[GenericTypeArgument]
internal struct TTS { }

[GenericTypeArgument]
internal interface TTDisposable: IDisposable { }

[GenericTypeArgument]
internal interface TTEnumerator<out T>: IEnumerator<T> { }
```

## Generic composition roots

Sometimes you want to be able to create composition roots with type parameters. In this case, the composition root can only be represented by a method.
>[!IMPORTANT]
>`Resolve()` methods cannot be used to resolve generic composition roots.

```c#
using Pure.DI;

DI.Setup(nameof(Composition))
    // Disable Resolve methods to keep the public API minimal
    .Hint(Hint.Resolve, "Off")
    .Bind().To<Repository<TT>>()
    .Bind().To<CreateCommandHandler<TT>>()
    // Creates UpdateCommandHandler manually,
    // just for the sake of example
    .Bind("Update").To(ctx => {
        ctx.Inject(out IRepository<TT> repository);
        return new UpdateCommandHandler<TT>(repository);
    })

    // Specifies to create a regular public method
    // to get a composition root of type ICommandHandler<T>
    // with the name "GetCreateCommandHandler"
    .Root<ICommandHandler<TT>>("GetCreateCommandHandler")

    // Specifies to create a regular public method
    // to get a composition root of type ICommandHandler<T>
    // with the name "GetUpdateCommandHandler"
    // using the "Update" tag
    .Root<ICommandHandler<TT>>("GetUpdateCommandHandler", "Update");

var composition = new Composition();

// createHandler = new CreateCommandHandler<int>(new Repository<int>());
var createHandler = composition.GetCreateCommandHandler<int>();

// updateHandler = new UpdateCommandHandler<string>(new Repository<string>());
var updateHandler = composition.GetUpdateCommandHandler<string>();

interface IRepository<T>;

class Repository<T> : IRepository<T>;

interface ICommandHandler<T>;

class CreateCommandHandler<T>(IRepository<T> repository) : ICommandHandler<T>;

class UpdateCommandHandler<T>(IRepository<T> repository) : ICommandHandler<T>;
```

To run the above code, the following NuGet package must be added:
 - [Pure.DI](https://www.nuget.org/packages/Pure.DI)

>[!IMPORTANT]
>The method `Inject()` cannot be used outside of the binding setup.

## Generic composition roots with constraints

Generic composition roots respect type constraints. Using constrained marker types — `TTDisposable` (`IDisposable`) and `TTS` (`struct`) — in `Root<IDataProcessor<TTDisposable, TTS>>("GetProcessor")` produces a generic method `GetProcessor<T, TOptions>()` whose type parameters carry the same constraints.
A tagged root can also fix one of the type arguments, as `GetSpecializedProcessor<T>()` does with `bool`.
>[!IMPORTANT]
>`Resolve` methods cannot be used to resolve generic composition roots.

```c#
using Pure.DI;

DI.Setup(nameof(Composition))
    // Disable Resolve methods to keep the public API minimal
    .Hint(Hint.Resolve, "Off")
    .Bind().To<StreamSource<TTDisposable>>()
    .Bind().To<DataProcessor<TTDisposable, TTS>>()
    // Creates SpecializedDataProcessor manually,
    // just for the sake of example.
    // It treats 'bool' as the options type for specific boolean flags.
    .Bind("Specialized").To(ctx => {
        ctx.Inject(out IStreamSource<TTDisposable> source);
        return new SpecializedDataProcessor<TTDisposable>(source);
    })

    // Specifies to create a regular public method
    // to get a composition root of type DataProcessor<T, TOptions>
    // with the name "GetProcessor"
    .Root<IDataProcessor<TTDisposable, TTS>>("GetProcessor")

    // Specifies to create a regular public method
    // to get a composition root of type SpecializedDataProcessor<T>
    // with the name "GetSpecializedProcessor"
    // using the "Specialized" tag
    .Root<IDataProcessor<TTDisposable, bool>>("GetSpecializedProcessor", "Specialized");

var composition = new Composition();

// Creates a processor for a Stream with 'double' as options (e.g., threshold)
// processor = new DataProcessor<Stream, double>(new StreamSource<Stream>());
var processor = composition.GetProcessor<Stream, double>();

// Creates a specialized processor for a BinaryReader
// specializedProcessor = new SpecializedDataProcessor<BinaryReader>(new StreamSource<BinaryReader>());
var specializedProcessor = composition.GetSpecializedProcessor<BinaryReader>();

interface IStreamSource<T>
    where T : IDisposable;

class StreamSource<T> : IStreamSource<T>
    where T : IDisposable;

interface IDataProcessor<T, TOptions>
    where T : IDisposable
    where TOptions : struct;

class DataProcessor<T, TOptions>(IStreamSource<T> source) : IDataProcessor<T, TOptions>
    where T : IDisposable
    where TOptions : struct;

class SpecializedDataProcessor<T>(IStreamSource<T> source) : IDataProcessor<T, bool>
    where T : IDisposable;
```

To run the above code, the following NuGet package must be added:
 - [Pure.DI](https://www.nuget.org/packages/Pure.DI)

>[!IMPORTANT]
>The method `Inject()` cannot be used outside of the binding setup.

## Generic async composition roots with constraints

Generic composition roots can be asynchronous and constrained at the same time. Constrained marker types — `TTDisposable` (`IDisposable`) and `TTS` (`struct`) — carry their constraints into the generated methods, and wrapping the root type in `Task<...>` yields methods like `GetDataQueryAsync<T, TStruct>(CancellationToken)` that build the object graph asynchronously.
The `CancellationToken` comes from a `RootArg` and is passed in at resolution time.
>[!IMPORTANT]
>`Resolve` methods cannot be used to resolve generic composition roots.

```c#
using Pure.DI;

DI.Setup(nameof(Composition))
    // Disable Resolve methods to keep the public API minimal
    .Hint(Hint.Resolve, "Off")
    .Bind().To<ConnectionProvider<TTDisposable>>()
    .Bind().To<DataQuery<TTDisposable, TTS>>()
    // Creates StatusQuery manually,
    // just for the sake of example
    .Bind("Status").To(ctx => {
        ctx.Inject(out IConnectionProvider<TTDisposable> connectionProvider);
        return new StatusQuery<TTDisposable>(connectionProvider);
    })

    // Specifies to use CancellationToken from the argument
    // when resolving a composition root
    .RootArg<CancellationToken>("cancellationToken")

    // Specifies to create a regular public method
    // to get a composition root of type Task<DataQuery<T, TStruct>>
    // with the name "GetDataQueryAsync"
    .Root<Task<IQuery<TTDisposable, TTS>>>("GetDataQueryAsync")

    // Specifies to create a regular public method
    // to get a composition root of type Task<StatusQuery<T>>
    // with the name "GetStatusQueryAsync"
    // using the "Status" tag
    .Root<Task<IQuery<TTDisposable, bool>>>("GetStatusQueryAsync", "Status");

var composition = new Composition();

// Resolves composition roots asynchronously
var query = await composition.GetDataQueryAsync<Stream, double>(CancellationToken.None);
var status = await composition.GetStatusQueryAsync<BinaryReader>(CancellationToken.None);

interface IConnectionProvider<T>
    where T : IDisposable;

class ConnectionProvider<T> : IConnectionProvider<T>
    where T : IDisposable;

interface IQuery<TConnection, TResult>
    where TConnection : IDisposable
    where TResult : struct;

class DataQuery<TConnection, TResult>(IConnectionProvider<TConnection> connectionProvider)
    : IQuery<TConnection, TResult>
    where TConnection : IDisposable
    where TResult : struct;

class StatusQuery<TConnection>(IConnectionProvider<TConnection> connectionProvider)
    : IQuery<TConnection, bool>
    where TConnection : IDisposable;
```

To run the above code, the following NuGet package must be added:
 - [Pure.DI](https://www.nuget.org/packages/Pure.DI)

>[!IMPORTANT]
>The method `Inject()` cannot be used outside of the binding setup.

## Complex generics

Defining generic type arguments using particular marker types like `TT` in this sample is a distinguishing and outstanding feature. This allows binding complex generic types with nested generic types and with any type constraints. For instance `IService<T1, T2, TList, TDictionary> where T2: struct where TList: IList<T1> where TDictionary: IDictionary<T1, T2> { }` and its binding to the some implementation `.Bind<IService<TT1, TTS2, TTList<TT1>, TTDictionary<TT1, TTS2>>>().To<Service<TT1, TTS2, TTList<TT1>, TTDictionary<TT1, TTS2>>>()` with all checks and code-generation at the compile time. It is clear that this example is exaggerated, it just demonstrates the ease of working with marker types like `TT, TTEnumerable, TTSet` and etc. for binding complex generic types.

```c#
using Shouldly;
using Pure.DI;

DI.Setup(nameof(Composition))
    // Disable Resolve methods to keep the public API minimal
    .Hint(Hint.Resolve, "Off")
    .RootArg<TT>("name")
    .Bind<IConsumer<TT>>().To<Consumer<TT>>()
    .Bind<IConsumer<TTS>>("struct")
    .As(Lifetime.Singleton)
    .To<StructConsumer<TTS>>()
    .Bind<IWorkflow<TT1, TTS2, TTList<TT1>, TTDictionary<TT1, TTS2>>>()
    .To<Workflow<TT1, TTS2, TTList<TT1>, TTDictionary<TT1, TTS2>>>()

    // Composition root
    .Root<Program<TT>>("GetRoot");

var composition = new Composition();
var program = composition.GetRoot<string>(name: "Super Task");
var workflow = program.Workflow;
workflow.ShouldBeOfType<Workflow<string, int, List<string>, Dictionary<string, int>>>();
workflow.TaskConsumer.ShouldBeOfType<Consumer<string>>();
workflow.PriorityConsumer.ShouldBeOfType<StructConsumer<int>>();

interface IConsumer<T>;

class Consumer<T>(T name) : IConsumer<T>;

readonly record struct StructConsumer<T> : IConsumer<T>
    where T : struct;

interface IWorkflow<TTask, TPriority, TTaskList, TTaskPriorities>
    where TPriority : struct
    where TTaskList : IList<TTask>
    where TTaskPriorities : IDictionary<TTask, TPriority>
{
    IConsumer<TTask> TaskConsumer { get; }

    IConsumer<TPriority> PriorityConsumer { get; }
}

class Workflow<TTask, TPriority, TTaskList, TTaskPriorities>(
    IConsumer<TTask> taskConsumer,
    [Tag("struct")] IConsumer<TPriority> priorityConsumer)
    : IWorkflow<TTask, TPriority, TTaskList, TTaskPriorities>
    where TPriority : struct
    where TTaskList : IList<TTask>
    where TTaskPriorities : IDictionary<TTask, TPriority>
{
    public IConsumer<TTask> TaskConsumer { get; } = taskConsumer;

    public IConsumer<TPriority> PriorityConsumer { get; } = priorityConsumer;
}

class Program<T>(IWorkflow<T, int, List<T>, Dictionary<T, int>> workflow)
    where T : notnull
{
    public IWorkflow<T, int, List<T>, Dictionary<T, int>> Workflow { get; } = workflow;
}
```

To run the above code, the following NuGet packages must be added:
 - [Pure.DI](https://www.nuget.org/packages/Pure.DI)
 - [Shouldly](https://www.nuget.org/packages/Shouldly)

It can also be useful in a very simple scenario where, for example, the sequence of type arguments does not match the sequence of arguments of the contract that implements the type.

## Custom generic argument

Besides the built-in marker types like `TT`, `TT1`, `TTS`, you can declare your own. Registering a type with `GenericTypeArgument<MyTT>()` turns it into a marker usable in generic bindings, such as `Bind<ISequence<MyTT>>().To<Sequence<MyTT>>()`.
Reach for this when the predefined markers are not enough — for example, to give markers meaningful names or specific type constraints.

```c#
using Shouldly;
using Pure.DI;

DI.Setup(nameof(Composition))
    // Registers the "MyTT" interface as a custom generic type argument
    // to be used as a marker for generic bindings
    .GenericTypeArgument<MyTT>()
    .Bind<ISequence<MyTT>>().To<Sequence<MyTT>>()
    .Bind<IProgram>().To<MyApp>()

    // Composition root
    .Root<IProgram>("Root");

var composition = new Composition();
var program = composition.Root;
program.IntSequence.ShouldBeOfType<Sequence<int>>();
program.StringSequence.ShouldBeOfType<Sequence<string>>();

// Defines a custom generic type argument marker
interface MyTT;

interface ISequence<T>;

class Sequence<T> : ISequence<T>;

interface IProgram
{
    ISequence<int> IntSequence { get; }

    ISequence<string> StringSequence { get; }
}

class MyApp(
    ISequence<int> intSequence,
    ISequence<string> stringSequence)
    : IProgram
{
    public ISequence<int> IntSequence { get; } = intSequence;

    public ISequence<string> StringSequence { get; } = stringSequence;
}
```

To run the above code, the following NuGet packages must be added:
 - [Pure.DI](https://www.nuget.org/packages/Pure.DI)
 - [Shouldly](https://www.nuget.org/packages/Shouldly)

>[!NOTE]
>Custom generic arguments provide flexibility for complex generic scenarios beyond standard marker types.

## Generic root arguments

Sometimes a composition root needs an argument whose type depends on the root's own type parameter. Declaring `RootArg<TT>("model")` together with the generic root `Root<IPresenter<TT>>("GetPresenter")` produces a generic method `GetPresenter<T>(T model)`.
The value passed to that method is injected into `Presenter<T>` through the method marked with the `[Dependency]` attribute.

```c#
using Shouldly;
using Pure.DI;

DI.Setup(nameof(Composition))
    .RootArg<TT>("model")
    .Bind<IPresenter<TT>>().To<Presenter<TT>>()

    // Composition root
    .Root<IPresenter<TT>>("GetPresenter");

var composition = new Composition();

// The "model" argument is passed to the composition root
// and then injected into the "Presenter" class
var presenter = composition.GetPresenter<string>(model: "Hello World");

presenter.Model.ShouldBe("Hello World");

interface IPresenter<out T>
{
    T? Model { get; }
}

class Presenter<T> : IPresenter<T>
{
    // The Dependency attribute specifies to perform an injection
    [Dependency]
    public void Present(T model) =>
        Model = model;

    public T? Model { get; private set; }
}
```

To run the above code, the following NuGet packages must be added:
 - [Pure.DI](https://www.nuget.org/packages/Pure.DI)
 - [Shouldly](https://www.nuget.org/packages/Shouldly)

>[!NOTE]
>Generic root arguments enable flexible type parameterization while maintaining compile-time type safety.

## Complex generic root arguments

Root arguments can be generic too. `RootArg<SourceConfig<TT>>("config")` declares a root argument whose type follows the type parameter of the composition root, so the generated `GetSource<T>` method accepts a `SourceConfig<T>` at resolution time.
This is useful when a generic service needs per-call configuration: here the config is delivered to `Source<T>` through the `Initialize` method marked with the `[Dependency]` attribute.

```c#
using Shouldly;
using Pure.DI;

DI.Setup(nameof(Composition))
    // Defines a generic root argument 'config' of type SourceConfig<T>.
    // This allows passing specific configuration when resolving ISource<T>.
    .RootArg<SourceConfig<TT>>("config")
    .Bind<ISource<TT2>>().To<Source<TT2>>()

    // Composition root that creates a source for a specific type.
    // The 'GetSource' method will accept 'SourceConfig<T>' as an argument.
    .Root<ISource<TT3>>("GetSource");

var composition = new Composition();

// Resolve a source for 'int', passing specific configuration
var source = composition.GetSource<int>(
    new SourceConfig<int>(33, "IntSource"));

source.Value.ShouldBe(33);
source.Name.ShouldBe("IntSource");

// Represents configuration for a data source, including a default value
record SourceConfig<T>(T DefaultValue, string SourceName);

interface ISource<out T>
{
    T? Value { get; }
    string Name { get; }
}

class Source<T> : ISource<T>
{
    // The Dependency attribute specifies to perform an injection.
    // We use method injection to initialize the source with configuration
    // passed from the composition root.
    [Dependency]
    public void Initialize(SourceConfig<T> config)
    {
        Value = config.DefaultValue;
        Name = config.SourceName;
    }

    public T? Value { get; private set; }

    public string Name { get; private set; } = "";
}
```

To run the above code, the following NuGet packages must be added:
 - [Pure.DI](https://www.nuget.org/packages/Pure.DI)
 - [Shouldly](https://www.nuget.org/packages/Shouldly)

>[!NOTE]
>Complex generic scenarios require careful attention to type constraints and argument order for correct resolution.

## Generic injections on demand

On-demand creation via `Func<T>` works inside generic types too. `Distributor<T>` takes a `Func<IWorker<T>>` and calls it whenever it needs another worker — each call produces a new `Worker<T>` for the same type argument.
Use this when the consumer, not the composition, decides how many instances to create and when.

```c#
using Shouldly;
using Pure.DI;
using System.Collections.Generic;

DI.Setup(nameof(Composition))
    .Bind().To<Worker<TT>>()
    .Bind().To<Distributor<TT>>()

    // Composition root
    .Root<IDistributor<int>>("Root");

var composition = new Composition();
var distributor = composition.Root;

// Check that the distributor has created 2 workers
distributor.Workers.Count.ShouldBe(2);

interface IWorker<T>;

class Worker<T> : IWorker<T>;

interface IDistributor<T>
{
    IReadOnlyList<IWorker<T>> Workers { get; }
}

class Distributor<T>(Func<IWorker<T>> workerFactory) : IDistributor<T>
{
    public IReadOnlyList<IWorker<T>> Workers { get; } =
    [
        // Creates the first instance of the worker
        workerFactory(),
        // Creates the second instance of the worker
        workerFactory()
    ];
}
```

To run the above code, the following NuGet packages must be added:
 - [Pure.DI](https://www.nuget.org/packages/Pure.DI)
 - [Shouldly](https://www.nuget.org/packages/Shouldly)

>[!NOTE]
>Generic on-demand injection provides flexibility for creating instances with different type parameters as needed.

## Generic injections on demand with arguments

When creating a generic dependency requires a runtime value, inject a factory with arguments. `SensorHub<T>` receives a `Func<int, ISensor<T>>` and calls it with a specific `id` for each sensor; the `int` argument is mapped to the `Sensor<T>` constructor parameter.
This keeps the composition in charge of wiring while letting the consumer supply per-instance data at creation time.

```c#
using Shouldly;
using Pure.DI;
using System.Collections.Generic;

DI.Setup(nameof(Composition))
    .Bind().To<Sensor<TT>>()
    .Bind().To<SensorHub<TT>>()

    // Composition root
    .Root<ISensorHub<string>>("SensorHub");

var composition = new Composition();
var hub = composition.SensorHub;
var sensors = hub.Sensors;
sensors.Count.ShouldBe(2);
sensors[0].Id.ShouldBe(1);
sensors[1].Id.ShouldBe(2);

interface ISensor<out T>
{
    int Id { get; }
}

class Sensor<T>(int id) : ISensor<T>
{
    public int Id { get; } = id;
}

interface ISensorHub<out T>
{
    IReadOnlyList<ISensor<T>> Sensors { get; }
}

class SensorHub<T>(Func<int, ISensor<T>> sensorFactory) : ISensorHub<T>
{
    public IReadOnlyList<ISensor<T>> Sensors { get; } =
    [
        sensorFactory(1),
        sensorFactory(2)
    ];
}
```

To run the above code, the following NuGet packages must be added:
 - [Pure.DI](https://www.nuget.org/packages/Pure.DI)
 - [Shouldly](https://www.nuget.org/packages/Shouldly)

>[!NOTE]
>Generic factories with arguments allow passing runtime parameters while maintaining type safety.

## Build up of an existing generic object

In other words, injecting the necessary dependencies via methods, properties, or fields into an existing object.

```c#
using Shouldly;
using Pure.DI;

DI.Setup(nameof(Composition))
    .RootArg<string>("userName")
    .Bind().To(Guid.NewGuid)
    .Bind().To(ctx => {
        // The "BuildUp" method injects dependencies into an existing object.
        // This is useful when the object is created externally (e.g., by a UI framework
        // or an ORM) or requires specific initialization before injection.
        var context = new UserContext<TTS>();
        ctx.BuildUp(context);
        return context;
    })
    .Bind().To<Facade<TTS>>()

    // Composition root
    .Root<IFacade<Guid>>("GetFacade");

var composition = new Composition();
var facade = composition.GetFacade("Erik");

facade.Context.UserName.ShouldBe("Erik");
facade.Context.Id.ShouldNotBe(Guid.Empty);

interface IUserContext<out T>
    where T : struct
{
    string UserName { get; }

    T Id { get; }
}

class UserContext<T> : IUserContext<T>
    where T : struct
{
    // The Dependency attribute specifies to perform an injection
    [Dependency]
    public string UserName { get; set; } = "";

    public T Id { get; private set; }

    // The Dependency attribute specifies to perform an injection
    [Dependency]
    public void SetId(T id) => Id = id;
}

interface IFacade<out T>
    where T : struct
{
    IUserContext<T> Context { get; }
}

record Facade<T>(IUserContext<T> Context)
    : IFacade<T> where T : struct;
```

To run the above code, the following NuGet packages must be added:
 - [Pure.DI](https://www.nuget.org/packages/Pure.DI)
 - [Shouldly](https://www.nuget.org/packages/Shouldly)

>[!NOTE]
>Generic build-up allows you to inject dependencies into existing generic objects after their creation.

## Generic builder

Builders can be generic as well. `Builder<ViewModel<TTS, TT2>>("BuildUp")` generates a generic `BuildUp` method that injects dependencies into an instance you already have — handy when objects are created by an external framework (a UI library, a serializer) rather than by the composition.
The marker types define the method's type parameters: `TTS` matches the `struct` constraint on `TId`, and `TT2` stands for the model type.

```c#
using Shouldly;
using Pure.DI;

DI.Setup(nameof(Composition))
    .Bind(Tag.Id).To(() => (TT)(object)Guid.NewGuid())
    .Bind().To<Repository<TT>>()
    // Generic service builder
    // Defines a generic builder "BuildUp".
    // This is useful when instances are created by an external framework
    // (like a UI library or serialization) but require dependencies to be injected.
    .Builder<ViewModel<TTS, TT2>>("BuildUp");

var composition = new Composition();

// A view model instance created manually (or by a UI framework)
var viewModel = new ViewModel<Guid, Customer>();

// Inject dependencies (Id and Repository) into the existing instance
var builtViewModel = composition.BuildUp(viewModel);

builtViewModel.Id.ShouldNotBe(Guid.Empty);
builtViewModel.Repository.ShouldBeOfType<Repository<Customer>>();

// Domain model
record Customer;

interface IRepository<T>;

class Repository<T> : IRepository<T>;

interface IViewModel<out TId, TModel>
{
    TId Id { get; }

    IRepository<TModel>? Repository { get; }
}

// The view model is generic, allowing it to be used for various entities
record ViewModel<TId, TModel> : IViewModel<TId, TModel>
    where TId : struct
{
    public TId Id { get; private set; }

    // The dependency to be injected
    [Dependency]
    public IRepository<TModel>? Repository { get; set; }

    // Method injection for the ID
    [Dependency]
    public void SetId([Tag(Tag.Id)] TId id) => Id = id;
}
```

To run the above code, the following NuGet packages must be added:
 - [Pure.DI](https://www.nuget.org/packages/Pure.DI)
 - [Shouldly](https://www.nuget.org/packages/Shouldly)

>[!NOTE]
>Generic builders enable flexible object initialization while maintaining type safety across different generic types.

## Generic builders

Sometimes many related types need the same build-up treatment. A single `Builders<IMessage<TT, TT2>>("BuildUp")` call creates a builder for every type implementing `IMessage<TT, TT2>` that is visible at compile time — here both `QueryMessage<,>` and `CommandMessage<,>` get their own `BuildUp` overload.
Each builder injects members marked with the `[Dependency]` attribute into an existing instance, so instances created elsewhere (e.g. by an API controller) still receive their dependencies.

```c#
using Shouldly;
using Pure.DI;

DI.Setup(nameof(Composition))
    .Bind(Tag.Id).To(() => (TT)(object)Guid.NewGuid())
    .Bind().To<MessageTracker<TT>>()
    // Generic builder to inject dependencies into existing messages
    .Builders<IMessage<TT, TT2>>("BuildUp");

var composition = new Composition();

// A Query is created (e.g. by API controller), ID is missing
var query = new QueryMessage<Guid, string>();

// Composition injects dependencies and generates an ID
var queryWithDeps = composition.BuildUp(query);

queryWithDeps.Id.ShouldNotBe(Guid.Empty);
queryWithDeps.Tracker.ShouldBeOfType<MessageTracker<string>>();

// A Command is created, usually with a specific ID
var command = new CommandMessage<Guid, int>();

// Composition injects dependencies only
var commandWithDeps = composition.BuildUp(command);

commandWithDeps.Id.ShouldBe(Guid.Empty);
commandWithDeps.Tracker.ShouldBeOfType<MessageTracker<int>>();

// Works with abstract types/interfaces too
var queryMessage = new QueryMessage<Guid, double>();
queryMessage = composition.BuildUp(queryMessage);

queryMessage.ShouldBeOfType<QueryMessage<Guid, double>>();
queryMessage.Id.ShouldNotBe(Guid.Empty);
queryMessage.Tracker.ShouldBeOfType<MessageTracker<double>>();

interface IMessageTracker<T>;

class MessageTracker<T> : IMessageTracker<T>;

interface IMessage<out TId, TContent>
{
    TId Id { get; }

    IMessageTracker<TContent>? Tracker { get; }
}

record QueryMessage<TId, TContent> : IMessage<TId, TContent>
    where TId : struct
{
    public TId Id { get; private set; }

    [Dependency]
    public IMessageTracker<TContent>? Tracker { get; set; }

    // Injects a new ID
    [Dependency]
    public void SetId([Tag(Tag.Id)] TId id) => Id = id;
}

record CommandMessage<TId, TContent> : IMessage<TId, TContent>
    where TId : struct
{
    public TId Id { get; }

    [Dependency]
    public IMessageTracker<TContent>? Tracker { get; set; }
}
```

To run the above code, the following NuGet packages must be added:
 - [Pure.DI](https://www.nuget.org/packages/Pure.DI)
 - [Shouldly](https://www.nuget.org/packages/Shouldly)

>[!NOTE]
>Generic builders provide compile-time type safety while allowing flexible object graph construction.

## Generic roots

Declaring a separate root for every generic implementation gets tedious. `Roots<IExporter<TT>>("GetMy{type}")` creates a composition root for each type implementing `IExporter<TT>` that is known at compile time at the point of the call.
The `{type}` placeholder in the name template is replaced with the implementation name, producing methods like `GetMyFileExporter_T<T>()` and `GetMyNetworkExporter_T<T>()`.

```c#
using Shouldly;
using Pure.DI;

DI.Setup(nameof(Composition))
    // Disable Resolve methods to keep the public API minimal
    .Hint(Hint.Resolve, "Off")
    .Bind().To<JsonFormatter<TT>>()
    .Bind().To<FileExporter<TT>>()
    // Creates NetworkExporter manually,
    // just for the sake of example
    .Bind<NetworkExporter<TT>>().To(ctx => {
        ctx.Inject(out IFormatter<TT> formatter);
        return new NetworkExporter<TT>(formatter);
    })

    // Specifies to define composition roots for all types inherited from IExporter<TT>
    // available at compile time at the point where the method is called
    .Roots<IExporter<TT>>("GetMy{type}");

var composition = new Composition();

// fileExporter = new FileExporter<int>(new JsonFormatter<int>());
var fileExporter = composition.GetMyFileExporter_T<int>();

// networkExporter = new NetworkExporter<string>(new JsonFormatter<string>());
var networkExporter = composition.GetMyNetworkExporter_T<string>();

interface IFormatter<T>;

class JsonFormatter<T> : IFormatter<T>;

interface IExporter<T>;

class FileExporter<T>(IFormatter<T> formatter) : IExporter<T>;

class NetworkExporter<T>(IFormatter<T> formatter) : IExporter<T>;
```

To run the above code, the following NuGet packages must be added:
 - [Pure.DI](https://www.nuget.org/packages/Pure.DI)
 - [Shouldly](https://www.nuget.org/packages/Shouldly)

>[!NOTE]
>Generic roots enable exposing multiple generic implementations without explicitly registering each one.

## Constructor ordinal attribute

Applying this attribute disables automatic constructor selection. Only constructors marked with this attribute are considered, ordered by ordinal (ascending).

```c#
using Shouldly;
using Pure.DI;

DI.Setup(nameof(Composition))
    .Arg<string>("connectionString")
    .Bind().To<Configuration>()
    .Bind().To<SqlDatabaseClient>()

    // Composition root
    .Root<IDatabaseClient>("Client");

var composition = new Composition(connectionString: "Server=.;Database=MyDb;");
var client = composition.Client;

// The client was created using the connection string constructor (Ordinal 0)
// even though the configuration constructor (Ordinal 1) was also possible.
client.ConnectionString.ShouldBe("Server=.;Database=MyDb;");

interface IConfiguration;

class Configuration : IConfiguration;

interface IDatabaseClient
{
    string ConnectionString { get; }
}

class SqlDatabaseClient : IDatabaseClient
{
    // The integer value in the argument specifies
    // the ordinal of injection.
    // The DI will try to use this constructor first (Ordinal 0).
    [Ordinal(0)]
    internal SqlDatabaseClient(string connectionString) =>
        ConnectionString = connectionString;

    // If the first constructor cannot be used (e.g. connectionString is missing),
    // the DI will try to use this one (Ordinal 1).
    [Ordinal(1)]
    public SqlDatabaseClient(IConfiguration configuration) =>
        ConnectionString = "Server=.;Database=DefaultDb;";

    public SqlDatabaseClient() =>
        ConnectionString = "InMemory";

    public string ConnectionString { get; }
}
```

To run the above code, the following NuGet packages must be added:
 - [Pure.DI](https://www.nuget.org/packages/Pure.DI)
 - [Shouldly](https://www.nuget.org/packages/Shouldly)

The `Ordinal` attribute is part of the API, but you can define your own in any assembly or namespace.
A lower constructor ordinal takes precedence over the number of injection parameters and accessibility. If that constructor cannot be resolved, Pure.DI tries the next ordinal.
Constructors with the same ordinal use the regular secondary criteria: more injection parameters first, then `public` before `internal`. Use distinct ordinal values when the choice affects behavior.

## Member ordinal attribute

When applied to a field, property, method, or method parameter, the member participates in DI, ordered by ordinal (ascending).

```c#
using Shouldly;
using Pure.DI;
using System.Text;

DI.Setup(nameof(PersonComposition))
    .Arg<int>("personId")
    .Arg<string>("personName")
    .Arg<DateTime>("personBirthday")
    .Bind().To<Person>()

    // Composition root
    .Root<IPerson>("Person");

var composition = new PersonComposition(
    personId: 123,
    personName: "Nik",
    personBirthday: new DateTime(1977, 11, 16));

var person = composition.Person;
person.Name.ShouldBe("123 Nik 1977-11-16");

interface IPerson
{
    string Name { get; }
}

class Person : IPerson
{
    private readonly StringBuilder _name = new();

    public string Name => _name.ToString();

    // The Ordinal attribute specifies to perform an injection,
    // the integer value in the argument specifies
    // the ordinal of injection
    [Ordinal(0)] public int Id;

    [Ordinal(1)]
    public string FirstName
    {
        set
        {
            _name.Append(Id);
            _name.Append(' ');
            _name.Append(value);
        }
    }

    public void SetBirthday([Ordinal(2)] DateTime value)
    {
        _name.Append(' ');
        _name.Append($"{value:yyyy-MM-dd}");
    }
}
```

To run the above code, the following NuGet packages must be added:
 - [Pure.DI](https://www.nuget.org/packages/Pure.DI)
 - [Shouldly](https://www.nuget.org/packages/Shouldly)

The `Ordinal` attribute is part of the API, but you can define your own in any assembly or namespace.
For an injection method, `Ordinal` can be placed on the method or its parameters. A method-level ordinal takes precedence; otherwise, the lowest parameter ordinal determines the method's execution order.
Required fields and required or selected `init` properties are assigned in the object initializer before regular member injection. Within regular member injection, lower ordinals run first; equal ordinals are ordered by field, property, and method, then by declaration order within each kind.
For equal ordinals and the same member kind, members declared on a derived type run before members inherited from its base types. Equal ordinals are valid and do not produce a diagnostic. Negative ordinal values are also supported.

## Dependency attribute

When applied to a property or field, the member participates in DI, ordered by ordinal (ascending).

```c#
using Shouldly;
using Pure.DI;
using System.Text;

DI.Setup(nameof(PersonComposition))
    .Arg<int>("personId")
    .Arg<string>("personName")
    .Arg<DateTime>("personBirthday")
    .Bind().To<Person>()

    // Composition root
    .Root<IPerson>("Person");

var composition = new PersonComposition(
    personId: 123,
    personName: "Nik",
    personBirthday: new DateTime(1977, 11, 16));

var person = composition.Person;
person.Name.ShouldBe("123 Nik 1977-11-16");

interface IPerson
{
    string Name { get; }
}

class Person : IPerson
{
    private readonly StringBuilder _name = new();

    public string Name => _name.ToString();

    [Dependency] public int Id;

    // The Ordinal attribute specifies to perform an injection,
    // the integer value in the argument specifies
    // the ordinal of injection
    [Dependency(ordinal: 1)]
    public string FirstName
    {
        set
        {
            _name.Append(Id);
            _name.Append(' ');
            _name.Append(value);
        }
    }

    [Dependency(ordinal: 2)]
    public DateTime Birthday
    {
        set
        {
            _name.Append(' ');
            _name.Append($"{value:yyyy-MM-dd}");
        }
    }
}
```

To run the above code, the following NuGet packages must be added:
 - [Pure.DI](https://www.nuget.org/packages/Pure.DI)
 - [Shouldly](https://www.nuget.org/packages/Shouldly)

The `Dependency` attribute is part of the API, but you can define your own in any assembly or namespace.

## Overload resolution priority

Library types sometimes keep an older constructor for compatibility while introducing a better overload for newly compiled applications. Starting with C# 13, `OverloadResolutionPriorityAttribute` tells the compiler which overload should be preferred. Pure.DI follows the same priority when it chooses a constructor and builds its dependency graph. For a primary constructor, place the attribute on the type declaration with the `method:` target.

```c#
using Shouldly;
using Pure.DI;
using System.Runtime.CompilerServices;

DI.Setup(nameof(Composition))
    // Both constructor dependencies can be created automatically.
    // The constructor attribute determines which graph is preferred.
    .Root<BillingApiClient>("Client");

var composition = new Composition();
var client = composition.Client;

client.Transport.ShouldBe("resilient-http");

class LegacyHttpOptions;

class ResilientHttpOptions;

[method: OverloadResolutionPriority(1)]
class BillingApiClient(ResilientHttpOptions options)
{
    // Kept so existing callers compiled against the old API continue to work.
    public BillingApiClient(LegacyHttpOptions options)
        : this(new ResilientHttpOptions()) => Transport = "legacy-http";

    // New compilations, including generated Pure.DI code, prefer the primary constructor.
    public ResilientHttpOptions Options { get; } = options;

    public string Transport { get; private set; } = "resilient-http";
}
```

To run the above code, the following NuGet packages must be added:
 - [Pure.DI](https://www.nuget.org/packages/Pure.DI)
 - [Shouldly](https://www.nuget.org/packages/Shouldly)

Higher integer values are preferred; unannotated constructors have priority `0`, and negative values can de-prioritize legacy overloads. If the preferred constructor cannot be resolved, Pure.DI continues with the next applicable constructor.
A primary constructor participates with the same rules as an explicitly declared constructor. Use `[method: OverloadResolutionPriority(...)]` or `[method: Ordinal(...)]` to attach a constructor attribute to a class or positional record primary constructor.
When neither `OrdinalAttribute` nor an explicit `OverloadResolutionPriorityAttribute` is present, Pure.DI uses the primary constructor only as the final tie-breaker after the number of injections and constructor accessibility. This makes an otherwise equal choice deterministic without displacing a constructor designed for richer dependency injection.
`OrdinalAttribute` remains the explicit Pure.DI override. When any accessible constructor is marked with `Ordinal`, only marked constructors participate, and their ordinal order takes precedence over `OverloadResolutionPriorityAttribute`.

## Partial constructor injection

C# 14 partial constructors let a type declare its construction contract in one part and provide the body in another. Pure.DI sees the combined constructor symbol, resolves its parameters once, and invokes it like a regular constructor. This is useful when a source generator owns the defining declaration while application code supplies the implementation.

```c#
using Shouldly;
using Pure.DI;

DI.Setup(nameof(Composition))
    .Bind<IAuditTransport>("durable").To<FileAuditTransport>()
    .Bind().To(_ => new AuditSinkOptions(BatchSize: 128))

    // Composition root
    .Root<AuditSink>("AuditSink");

var composition = new Composition();
var sink = composition.AuditSink;

sink.Transport.ShouldBeOfType<FileAuditTransport>();
sink.BatchSize.ShouldBe(128);

interface IAuditTransport;

class FileAuditTransport : IAuditTransport;

record AuditSinkOptions(int BatchSize);

abstract class AuditSinkBase(IAuditTransport transport)
{
    public IAuditTransport Transport { get; } = transport;
}

partial class AuditSink : AuditSinkBase
{
    // This defining declaration participates in constructor lookup.
    // Its parameter attributes are merged with the implementing part.
    public partial AuditSink(
        [Tag("durable")] IAuditTransport transport,
        AuditSinkOptions options);

    public int BatchSize { get; private set; }
}

partial class AuditSink
{
    // A base/this initializer is allowed only on the implementing declaration.
    public partial AuditSink(
        IAuditTransport transport,
        AuditSinkOptions options)
        : base(transport)
    {
        BatchSize = options.BatchSize;
    }
}
```

To run the above code, the following NuGet packages must be added:
 - [Pure.DI](https://www.nuget.org/packages/Pure.DI)
 - [Shouldly](https://www.nuget.org/packages/Shouldly)

A partial constructor must have exactly one defining declaration ending with `;` and one implementing declaration with a body. Only the defining declaration participates in lookup, while constructor and parameter attributes from both parts are combined.
Place `this(...)` or `base(...)` constructor initializers on the implementing declaration. `OrdinalAttribute`, `TagAttribute`, and `OverloadResolutionPriorityAttribute` can be placed on either part and are observed through the combined Roslyn symbol.

## Tag attribute

Tags let you choose among multiple implementations of the same contract.

```c#
using Shouldly;
using Pure.DI;

DI.Setup(nameof(Composition))
    .Bind("Fast").To<FastRenderer>()
    .Bind("Quality").To<QualityRenderer>()
    .Bind().To<PageRenderer>()

    // Composition root
    .Root<IPageRenderer>("Renderer");

var composition = new Composition();
var pageRenderer = composition.Renderer;
pageRenderer.FastRenderer.ShouldBeOfType<FastRenderer>();
pageRenderer.QualityRenderer.ShouldBeOfType<QualityRenderer>();

interface IRenderer;

class FastRenderer : IRenderer;

class QualityRenderer : IRenderer;

interface IPageRenderer
{
    IRenderer FastRenderer { get; }

    IRenderer QualityRenderer { get; }
}

class PageRenderer(
    [Tag("Fast")] IRenderer fastRenderer,
    [Tag("Quality")] IRenderer qualityRenderer)
    : IPageRenderer
{
    public IRenderer FastRenderer { get; } = fastRenderer;

    public IRenderer QualityRenderer { get; } = qualityRenderer;
}
```

To run the above code, the following NuGet packages must be added:
 - [Pure.DI](https://www.nuget.org/packages/Pure.DI)
 - [Shouldly](https://www.nuget.org/packages/Shouldly)

A tag can be a constant, a type, a [smart tag](smart-tags.md), or an enum value. The `Tag` attribute is part of the API, but you can define your own in any assembly or namespace.

## Type attribute

Use the `Type` attribute to force a specific injected type, overriding the inferred type from the parameter or member.

```c#
using Shouldly;
using Pure.DI;

DI.Setup(nameof(Composition))
    .Bind().To<NotificationService>()

    // Composition root
    .Root<INotificationService>("NotificationService");

var composition = new Composition();
var notificationService = composition.NotificationService;
notificationService.UserNotifier.ShouldBeOfType<EmailSender>();
notificationService.AdminNotifier.ShouldBeOfType<SmsSender>();

interface IMessageSender;

class EmailSender : IMessageSender;

class SmsSender : IMessageSender;

interface INotificationService
{
    IMessageSender UserNotifier { get; }

    IMessageSender AdminNotifier { get; }
}

class NotificationService(
    // The [Type] attribute forces the injection of a specific type,
    // overriding the default resolution behavior.
    [Type(typeof(EmailSender))] IMessageSender userNotifier,
    [Type(typeof(SmsSender))] IMessageSender adminNotifier)
    : INotificationService
{
    public IMessageSender UserNotifier { get; } = userNotifier;

    public IMessageSender AdminNotifier { get; } = adminNotifier;
}
```

To run the above code, the following NuGet packages must be added:
 - [Pure.DI](https://www.nuget.org/packages/Pure.DI)
 - [Shouldly](https://www.nuget.org/packages/Shouldly)

The `Type` attribute is part of the API, but you can define your own in any assembly or namespace.

## Inject attribute

If you want attributes without defining your own, add this package:

[![NuGet](https://img.shields.io/nuget/v/Pure.DI.Abstractions)](https://www.nuget.org/packages/Pure.DI.Abstractions)

It provides `Inject` and `Inject<T>` for constructors, methods, properties, and fields, letting you configure injection metadata.

```c#
using Shouldly;
using Pure.DI.Abstractions;
using Pure.DI;

DI.Setup(nameof(PersonComposition))
    .Arg<int>("personId")
    .Bind<Uri>("Person Uri").To(() => new Uri("https://github.com/DevTeam/Pure.DI"))
    .Bind("NikName").To(() => "Nik")
    .Bind().To<Person>()

    // Composition root
    .Root<IPerson>("Person");

var composition = new PersonComposition(personId: 123);
var person = composition.Person;
person.ToString().ShouldBe("123 Nik https://github.com/DevTeam/Pure.DI");

interface IPerson;

class Person([Inject("NikName")] string name) : IPerson
{
    private object? _state;

    [Inject<int>] internal object Id = "";

    public void Initialize([Inject<Uri>("Person Uri", 1)] object state) =>
        _state = state;

    public override string ToString() => $"{Id} {name} {_state}";
}
```

To run the above code, the following NuGet packages must be added:
 - [Pure.DI](https://www.nuget.org/packages/Pure.DI)
 - [Shouldly](https://www.nuget.org/packages/Shouldly)
 - [Pure.DI.Abstractions](https://www.nuget.org/packages/Pure.DI.Abstractions)

This package should also be included in a project:

[![NuGet](https://img.shields.io/nuget/v/Pure.DI)](https://www.nuget.org/packages/Pure.DI)

## Custom attributes

To use custom attributes, derive from `System.Attribute` and register them with the setup API:
- `TagAttribute`
- `OrdinalAttribute`
- `TypeAttribute`
You can also use combined attributes. Each registration method can take an optional argument index (default is 0) that specifies where to read _tag_, _ordinal_, or _type_ metadata.

```c#
using Shouldly;
using Pure.DI;

DI.Setup(nameof(PersonComposition))
    .TagAttribute<MyTagAttribute>()
    .OrdinalAttribute<MyOrdinalAttribute>()
    .TypeAttribute<MyTypeAttribute>()
    .TypeAttribute<MyGenericTypeAttribute<TT>>()
    .Arg<int>("personId")
    .Bind().To(() => new Uri("https://github.com/DevTeam/Pure.DI"))
    .Bind("NikName").To(() => "Nik")
    .Bind().To<Person>()

    // Composition root
    .Root<IPerson>("Person");

var composition = new PersonComposition(personId: 123);
var person = composition.Person;
person.ToString().ShouldBe("123 Nik https://github.com/DevTeam/Pure.DI");

[AttributeUsage(
    AttributeTargets.Constructor
    | AttributeTargets.Method |
    AttributeTargets.Property |
    AttributeTargets.Field)]
class MyOrdinalAttribute(int ordinal) : Attribute;

[AttributeUsage(
    AttributeTargets.Parameter
    | AttributeTargets.Property
    | AttributeTargets.Field)]
class MyTagAttribute(object tag) : Attribute;

[AttributeUsage(
    AttributeTargets.Parameter
    | AttributeTargets.Property
    | AttributeTargets.Field)]
class MyTypeAttribute(Type type) : Attribute;

[AttributeUsage(
    AttributeTargets.Parameter
    | AttributeTargets.Property
    | AttributeTargets.Field)]
class MyGenericTypeAttribute<T> : Attribute;

interface IPerson;

class Person([MyTag("NikName")] string name) : IPerson
{
    private object? _state;

    [MyOrdinal(1)] [MyType(typeof(int))] internal object Id = "";

    [MyOrdinal(2)]
    public void Initialize([MyGenericType<Uri>] object state) =>
        _state = state;

    public override string ToString() => $"{Id} {name} {_state}";
}
```

To run the above code, the following NuGet packages must be added:
 - [Pure.DI](https://www.nuget.org/packages/Pure.DI)
 - [Shouldly](https://www.nuget.org/packages/Shouldly)

>[!NOTE]
>Custom attributes provide extensibility for advanced scenarios where standard attributes don't meet specific requirements.

## Custom universal attribute

A combined attribute can supply _tag_, _ordinal_, and _type_ metadata. Each registration method can take an optional argument index (default is 0) that specifies where to read the metadata.

```c#
using Shouldly;
using Pure.DI;

DI.Setup(nameof(PersonComposition))
    .TagAttribute<InjectAttribute<TT>>()
    .OrdinalAttribute<InjectAttribute<TT>>(1)
    .TypeAttribute<InjectAttribute<TT>>()
    .Arg<int>("personId")
    .Bind().To(() => new Uri("https://github.com/DevTeam/Pure.DI"))
    .Bind("NikName").To(() => "Nik")
    .Bind().To<Person>()

    // Composition root
    .Root<IPerson>("Person");

var composition = new PersonComposition(personId: 123);
var person = composition.Person;
person.ToString().ShouldBe("123 Nik https://github.com/DevTeam/Pure.DI");

[AttributeUsage(
    AttributeTargets.Constructor
    | AttributeTargets.Method
    | AttributeTargets.Parameter
    | AttributeTargets.Property
    | AttributeTargets.Field)]
class InjectAttribute<T>(object? tag = null, int ordinal = 0) : Attribute;

interface IPerson;

class Person([Inject<string>("NikName")] string name) : IPerson
{
    private object? _state;

    [Inject<int>(ordinal: 1)] internal object Id = "";

    public void Initialize([Inject<Uri>] object state) =>
        _state = state;

    public override string ToString() => $"{Id} {name} {_state}";
}
```

To run the above code, the following NuGet packages must be added:
 - [Pure.DI](https://www.nuget.org/packages/Pure.DI)
 - [Shouldly](https://www.nuget.org/packages/Shouldly)

>[!NOTE]
>Universal attributes reduce the number of attributes needed by combining multiple metadata types into a single attribute.

## Custom generic argument attribute

Besides the built-in `TT` marker types, you can define your own generic type argument markers. Register a custom attribute with `GenericTypeArgumentAttribute<T>()`, apply it to a marker type like `TMy`, and use that marker in bindings: a single `Bind<IRepository<TMy>>().To<Repository<TMy>>()` then resolves `IRepository<T>` for any `T`, such as `IRepository<Post>` and `IRepository<Comment>`.

```c#
using Shouldly;
using Pure.DI;

DI.Setup(nameof(Composition))
    // Registers custom generic argument
    .GenericTypeArgumentAttribute<GenericArgAttribute>()
    .Bind<IRepository<TMy>>().To<Repository<TMy>>()
    .Bind<IContentService>().To<ContentService>()

    // Composition root
    .Root<IContentService>("ContentService");

var composition = new Composition();
var service = composition.ContentService;
service.Posts.ShouldBeOfType<Repository<Post>>();
service.Comments.ShouldBeOfType<Repository<Comment>>();

[AttributeUsage(AttributeTargets.Interface | AttributeTargets.Class | AttributeTargets.Struct)]
class GenericArgAttribute : Attribute;

[GenericArg]
interface TMy;

interface IRepository<T>;

class Repository<T> : IRepository<T>;

class Post;

class Comment;

interface IContentService
{
    IRepository<Post> Posts { get; }

    IRepository<Comment> Comments { get; }
}

class ContentService(
    IRepository<Post> posts,
    IRepository<Comment> comments)
    : IContentService
{
    public IRepository<Post> Posts { get; } = posts;

    public IRepository<Comment> Comments { get; } = comments;
}
```

To run the above code, the following NuGet packages must be added:
 - [Pure.DI](https://www.nuget.org/packages/Pure.DI)
 - [Shouldly](https://www.nuget.org/packages/Shouldly)

>[!NOTE]
>Custom generic argument attributes are useful when you need to pass metadata specific to generic type parameters during binding resolution.

## Export attribute

`ExportAttribute` lets you bind properties, fields, or methods declared on the bound type.

```c#
using Pure.DI;

DI.Setup(nameof(Composition))
    .Bind().As(Lifetime.Singleton).To<DeviceFeatureProvider>()
    .Bind().To<PhotoService>()

    // Composition root
    .Root<IPhotoService>("PhotoService");

var composition = new Composition();
var photoService = composition.PhotoService;
photoService.TakePhotoWithLocation();

interface IGps
{
    void GetLocation();
}

class Gps : IGps
{
    public void GetLocation() => Console.WriteLine("Coordinates: 123, 456");
}

interface ICamera
{
    void Capture();
}

class Camera : ICamera
{
    public void Capture() => Console.WriteLine("Photo captured");
}

class DeviceFeatureProvider
{
    // The [Export] attribute specifies that the property is a source of dependency
    [Export] public IGps Gps { get; } = new Gps();

    [Export] public ICamera Camera { get; } = new Camera();
}

interface IPhotoService
{
    void TakePhotoWithLocation();
}

class PhotoService(IGps gps, Func<ICamera> cameraFactory) : IPhotoService
{
    public void TakePhotoWithLocation()
    {
        gps.GetLocation();
        cameraFactory().Capture();
    }
}
```

To run the above code, the following NuGet package must be added:
 - [Pure.DI](https://www.nuget.org/packages/Pure.DI)

It applies to instance or static members, including members that return generic types.

## Export attribute with lifetime and tag

The `[Export]` attribute accepts optional `lifetime` and `tags` parameters, so an exported member is registered exactly like a hand-written binding. Here the `GraphicsAdapter.HighPerfGpu` property is exported as a `Singleton` with the tag `"HighPerformance"`, and `RayTracer` receives that instance by requesting `[Tag("HighPerformance")] IGpu`.

```c#
using Shouldly;
using Pure.DI;

DI.Setup(nameof(Composition))
    .Bind().As(Lifetime.Singleton).To<GraphicsAdapter>()
    .Bind().To<RayTracer>()

    // Composition root
    .Root<IRenderer>("Renderer");

var composition = new Composition();
var renderer = composition.Renderer;
renderer.Render();

interface IGpu
{
    void RenderFrame();
}

class DiscreteGpu : IGpu
{
    public void RenderFrame() => Console.WriteLine("Rendering with Discrete GPU");
}

class GraphicsAdapter
{
    // Binds the property to the composition with the specified
    // lifetime and tag. This allows the "HighPerformance" GPU
    // to be injected into other components.
    [Export(lifetime: Lifetime.Singleton, tags: ["HighPerformance"])]
    public IGpu HighPerfGpu { get; } = new DiscreteGpu();
}

interface IRenderer
{
    void Render();
}

class RayTracer([Tag("HighPerformance")] IGpu gpu) : IRenderer
{
    public void Render() => gpu.RenderFrame();
}
```

To run the above code, the following NuGet packages must be added:
 - [Pure.DI](https://www.nuget.org/packages/Pure.DI)
 - [Shouldly](https://www.nuget.org/packages/Shouldly)

>[!NOTE]
>Specifying lifetime and tag in the Export attribute allows for fine-grained control over instance creation and binding resolution.

## Export attribute for a generic type

The `[Export]` attribute works with generic types too: applied to a generic factory method with a `TT` marker, as in `[Export(typeof(IComments<TT>))]`, it makes a single method the source of `IComments<T>` for any requested `T`. This is handy when a factory class produces generic dependencies and you don't want to declare a separate binding for each closed type.

```c#
using Shouldly;
using Pure.DI;

DI.Setup(nameof(Composition))
    .Bind().As(Lifetime.Singleton).To<CommentsFactory>()
    .Bind().To<ArticleService>()

    // Composition root
    .Root<IArticleService>("ArticleService");

var composition = new Composition();
var articleService = composition.ArticleService;
articleService.DisplayComments();

interface IComments<T>
{
    void Load();
}

class Comments<T> : IComments<T>
{
    public void Load()
    {
    }
}

class CommentsFactory
{
    // The 'TT' type marker in the attribute indicates that this method
    // can produce 'IComments<T>' for any generic type 'T'.
    // This allows the factory to handle all requests for IComments<T>.
    [Export(typeof(IComments<TT>))]
    public IComments<T> Create<T>() => new Comments<T>();
}

interface IArticleService
{
    void DisplayComments();
}

class ArticleService(IComments<Article> comments) : IArticleService
{
    public void DisplayComments() => comments.Load();
}

class Article;
```

To run the above code, the following NuGet packages must be added:
 - [Pure.DI](https://www.nuget.org/packages/Pure.DI)
 - [Shouldly](https://www.nuget.org/packages/Shouldly)

>[!NOTE]
>The Export attribute provides a declarative way to specify bindings directly on types, reducing the need for manual composition setup.

## Bind attribute

Shows how to declare a binding directly on an implementation type with the built-in `BindAttribute`.

```c#
using Shouldly;
using Pure.DI;

DI.Setup(nameof(Composition))

    // Composition root
    .Root<IMessageWriter>("Writer", "console");

var composition = new Composition();
var writer = composition.Writer;
writer.Write("Pure.DI");

writer.ShouldBeOfType<ConsoleMessageWriter>();
writer.ShouldBeSameAs(composition.Writer);

interface IMessageWriter
{
    void Write(string message);
}

[Bind(typeof(IMessageWriter), Lifetime.Singleton, "console")]
class ConsoleMessageWriter : IMessageWriter
{
    public void Write(string message) => Console.WriteLine(message);
}
```

To run the above code, the following NuGet packages must be added:
 - [Pure.DI](https://www.nuget.org/packages/Pure.DI)
 - [Shouldly](https://www.nuget.org/packages/Shouldly)

>[!NOTE]
>`BindAttribute` is registered by default and can provide the contract type, lifetime, and tag. Attributes inside the same square-bracket group form one binding; separate `Bind` groups create separate bindings.

## Custom bind attribute

Shows how to declare a binding directly on an implementation type with a custom attribute. A custom attribute can combine contract type, lifetime, and tag metadata by registering argument positions in the composition setup.

```c#
using Shouldly;
using Pure.DI;

DI.Setup(nameof(Composition))
    .TypeAttribute<ServiceAttribute<TT>>()
    .LifetimeAttribute<ServiceAttribute<TT>>()
    .TagAttribute<ServiceAttribute<TT>>(1)

    // Composition root
    .Root<IMessageWriter>("Writer", "console");

var composition = new Composition();
var writer = composition.Writer;
writer.Write("Pure.DI");

writer.ShouldBeOfType<ConsoleMessageWriter>();
writer.ShouldBeSameAs(composition.Writer);

[AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
class ServiceAttribute<T> : Attribute
{
    public ServiceAttribute(
        Lifetime lifetime = Lifetime.Transient,
        object? tag = null)
    {
    }
}

interface IMessageWriter
{
    void Write(string message);
}

[Service<IMessageWriter>(Lifetime.Singleton, "console")]
class ConsoleMessageWriter : IMessageWriter
{
    public void Write(string message) => Console.WriteLine(message);
}
```

To run the above code, the following NuGet packages must be added:
 - [Pure.DI](https://www.nuget.org/packages/Pure.DI)
 - [Shouldly](https://www.nuget.org/packages/Shouldly)

>[!NOTE]
>Implementation-level binding attributes are useful when the implementation assembly should describe its DI role while keeping the composition concise. Custom attributes participate in the same merge rules as the built-in `Bind`, `Type`, `Tag`, and `Lifetime` attributes: attributes in one square-bracket group form one binding, while separate `Bind` groups create separate bindings.

## Bind type attribute

Shows how the `Type` attribute can declare a contract directly on an implementation type.

```c#
using Shouldly;
using Pure.DI;

DI.Setup(nameof(Composition))

    // Composition root
    .Root<IMessageWriter>("Writer");

var composition = new Composition();
var writer = composition.Writer;

writer.ShouldBeOfType<ConsoleMessageWriter>();

interface IMessageWriter;

[Type(typeof(IMessageWriter))]
class ConsoleMessageWriter : IMessageWriter;
```

To run the above code, the following NuGet packages must be added:
 - [Pure.DI](https://www.nuget.org/packages/Pure.DI)
 - [Shouldly](https://www.nuget.org/packages/Shouldly)

>[!NOTE]
>When a registered type attribute is applied to a class or struct, Pure.DI treats it as binding metadata for that implementation.

## Bind type attributes

Shows how several `Type` attributes can expose one implementation through several contracts.

```c#
using Shouldly;
using Pure.DI;

DI.Setup(nameof(Composition))

    // Composition roots
    .Root<IMessageWriter>("Writer")
    .Root<IDiagnosticsSink>("Diagnostics");

var composition = new Composition();

composition.Writer.ShouldBeOfType<ConsoleChannel>();
composition.Diagnostics.ShouldBeOfType<ConsoleChannel>();

interface IMessageWriter;

interface IDiagnosticsSink;

[Type(typeof(IMessageWriter))]
[Type(typeof(IDiagnosticsSink))]
class ConsoleChannel : IMessageWriter, IDiagnosticsSink;
```

To run the above code, the following NuGet packages must be added:
 - [Pure.DI](https://www.nuget.org/packages/Pure.DI)
 - [Shouldly](https://www.nuget.org/packages/Shouldly)

>[!NOTE]
>Multiple type attributes in the same square-bracket binding group are merged into one binding with several contracts.

## Generic bind type attribute

Shows how a custom generic attribute can declare a contract type on an implementation.

```c#
using Shouldly;
using Pure.DI;

DI.Setup(nameof(Composition))
    .TypeAttribute<BindingAttribute<TT>>()

    // Composition root
    .Root<IMessageWriter>("Writer");

var composition = new Composition();
var writer = composition.Writer;

writer.ShouldBeOfType<FileMessageWriter>();

[AttributeUsage(AttributeTargets.Class)]
class BindingAttribute<T> : Attribute;

interface IMessageWriter;

[Binding<IMessageWriter>]
class FileMessageWriter : IMessageWriter;
```

To run the above code, the following NuGet packages must be added:
 - [Pure.DI](https://www.nuget.org/packages/Pure.DI)
 - [Shouldly](https://www.nuget.org/packages/Shouldly)

>[!NOTE]
>Registering the custom generic attribute with `TypeAttribute<T>()` lets Pure.DI read the contract from the attribute type argument.

## Bind generic contract attribute

Shows how the built-in `BindAttribute` can declare a generic contract with the `TT` marker on a generic implementation.

```c#
using Shouldly;
using Pure.DI;

DI.Setup(nameof(Composition))
    .Bind<ICat>().To<ShroedingersCat>()

    // Composition root
    .Root<IBox<ICat>>("Box");

var composition = new Composition();
var box = composition.Box;

box.ShouldBeOfType<CardboardBox<ICat>>();
box.Content.ShouldBeOfType<ShroedingersCat>();

interface IBox<out T>
{
    T Content { get; }
}

interface ICat;

[Bind(typeof(IBox<TT>))]
record CardboardBox<T>(T Content) : IBox<T>;

class ShroedingersCat : ICat;
```

To run the above code, the following NuGet packages must be added:
 - [Pure.DI](https://www.nuget.org/packages/Pure.DI)
 - [Shouldly](https://www.nuget.org/packages/Shouldly)

>[!NOTE]
>`typeof(IBox<TT>)` is a marker-based generic contract. It is different from the open generic `typeof(IBox<>)`: Pure.DI uses `TT` to construct the matching implementation type, such as `CardboardBox<TT>`.

## Bind tag attribute

Shows how tags can be declared directly on implementation types, including several tags for one implementation.

```c#
using Shouldly;
using Pure.DI;

DI.Setup(nameof(Composition))

    // Composition roots
    .Root<IMessageWriter>("ConsoleWriter", "console")
    .Root<IMessageWriter>("DefaultWriter", "default")
    .Root<IMessageWriter>("FileWriter", "file");

var composition = new Composition();

composition.ConsoleWriter.ShouldBeOfType<ConsoleMessageWriter>();
composition.DefaultWriter.ShouldBeOfType<ConsoleMessageWriter>();
composition.FileWriter.ShouldBeOfType<FileMessageWriter>();

interface IMessageWriter;

[Tag("console")]
[Tag("default")]
class ConsoleMessageWriter : IMessageWriter;

[Tag("file")]
class FileMessageWriter : IMessageWriter;
```

To run the above code, the following NuGet packages must be added:
 - [Pure.DI](https://www.nuget.org/packages/Pure.DI)
 - [Shouldly](https://www.nuget.org/packages/Shouldly)

>[!NOTE]
>A tag attribute on an implementation type becomes a binding tag. Several tag attributes in the same square-bracket binding group are merged into one binding, so the same implementation can be resolved by any of those tags.

## Bind lifetime attribute

Shows how the `Lifetime` attribute can declare the lifetime of an implementation binding.

```c#
using Shouldly;
using Pure.DI;

DI.Setup(nameof(Composition))

    // Composition root
    .Root<IClock>("Clock");

var composition = new Composition();

composition.Clock.ShouldBeSameAs(composition.Clock);

interface IClock;

[Type(typeof(IClock))]
[Lifetime(Lifetime.Singleton)]
class SystemClock : IClock;
```

To run the above code, the following NuGet packages must be added:
 - [Pure.DI](https://www.nuget.org/packages/Pure.DI)
 - [Shouldly](https://www.nuget.org/packages/Shouldly)

>[!NOTE]
>A lifetime attribute on an implementation type is equivalent to applying `.As(...)` to the generated binding. Lifetime metadata can be specified only once inside one square-bracket binding group; repeated lifetime metadata in the same group is a compilation error.

## Bind metadata merge

Shows how binding metadata attributes on one implementation type are combined into one binding.

```c#
using Shouldly;
using Pure.DI;

DI.Setup(nameof(Composition))

    // Composition roots
    .Root<IMessageWriter>("ConsoleWriter", "console")
    .Root<IMessageWriter>("DefaultWriter", "default")
    .Root<IDiagnosticsSink>("ConsoleDiagnostics", "console");

var composition = new Composition();

composition.ConsoleWriter.ShouldBeOfType<ConsoleChannel>();
composition.DefaultWriter.ShouldBeSameAs(composition.ConsoleWriter);
composition.ConsoleDiagnostics.ShouldBeSameAs(composition.ConsoleWriter);

interface IMessageWriter;

interface IDiagnosticsSink;

[Bind(typeof(IMessageWriter)), Bind(typeof(IDiagnosticsSink)), Tag("console"), Tag("default"), Lifetime(Lifetime.Singleton)]
class ConsoleChannel : IMessageWriter, IDiagnosticsSink;
```

To run the above code, the following NuGet packages must be added:
 - [Pure.DI](https://www.nuget.org/packages/Pure.DI)
 - [Shouldly](https://www.nuget.org/packages/Shouldly)

>[!NOTE]
>Attributes inside the same square-bracket group form one binding. Contracts and tags are merged, but lifetime must be specified at most once in the group. Separate `Bind` attribute groups create separate bindings.

## Bind attribute groups

Shows how separate `BindAttribute` groups let one implementation participate in several independent bindings.

```c#
using Shouldly;
using Pure.DI;
using System.Collections.Immutable;
using System.Linq;

DI.Setup(nameof(Composition))
    // Composition roots
    .Root<IOrderCheckout>("Checkout")
    .Root<IFailedPaymentReporter>("FailedPaymentReporter")
    .Root<IPaymentAuditSink>("AuditSink", "operations");

var composition = new Composition();
var checkout = composition.Checkout;
var failedPaymentReporter = composition.FailedPaymentReporter;

checkout.Processors.Length.ShouldBe(2);
checkout.Processors.OfType<CardPaymentGateway>().Count().ShouldBe(1);

var checkoutAudit = checkout.Processors.OfType<PaymentAuditAdapter>().Single();
checkoutAudit.ShouldBeSameAs(composition.Checkout.Processors.OfType<PaymentAuditAdapter>().Single());

failedPaymentReporter.AuditSinks.Length.ShouldBe(1);
failedPaymentReporter.AuditSinks[0].ShouldBeOfType<PaymentAuditAdapter>();
failedPaymentReporter.AuditSinks[0].ShouldNotBeSameAs(composition.AuditSink);

interface IPaymentProcessor
{
    void Process(Payment payment);
}

interface IPaymentAuditSink
{
    void Record(Payment payment);
}

[Bind(typeof(IPaymentProcessor))]
class CardPaymentGateway : IPaymentProcessor
{
    public void Process(Payment payment)
    {
        // Calling an external card payment provider...
    }
}

interface IEventStore
{
    void Append(Payment payment);
}

[Bind(typeof(IEventStore), Lifetime.Singleton)]
class EventStore : IEventStore
{
    public void Append(Payment payment)
    {
        // Persisting an audit event...
    }
}

interface IOrderCheckout
{
    ImmutableArray<IPaymentProcessor> Processors { get; }
}

[Bind(typeof(IOrderCheckout))]
class OrderCheckout(IEnumerable<IPaymentProcessor> processors) : IOrderCheckout
{
    public ImmutableArray<IPaymentProcessor> Processors { get; } = [..processors];
}

interface IFailedPaymentReporter
{
    ImmutableArray<IPaymentAuditSink> AuditSinks { get; }
}

[Bind(typeof(IFailedPaymentReporter))]
class FailedPaymentReporter(
    [Tag("operations")] IEnumerable<IPaymentAuditSink> auditSinks)
    : IFailedPaymentReporter
{
    public ImmutableArray<IPaymentAuditSink> AuditSinks { get; } = [..auditSinks];
}

// Binding group #1:
// add this adapter to the main checkout pipeline as a tagged payment processor.
[Bind(typeof(IPaymentProcessor), Lifetime.Singleton, "audit")]

// Binding group #2:
// expose the same adapter as an operational audit sink with its own lifetime.
[Bind(typeof(IPaymentAuditSink), Lifetime.Transient, "operations")]
class PaymentAuditAdapter(IEventStore eventStore) :
    IPaymentProcessor,
    IPaymentAuditSink
{
    public void Process(Payment payment) => Record(payment);

    public void Record(Payment payment) => eventStore.Append(payment);
}

record Payment(decimal Amount);
```

To run the above code, the following NuGet packages must be added:
 - [Pure.DI](https://www.nuget.org/packages/Pure.DI)
 - [Shouldly](https://www.nuget.org/packages/Shouldly)

>[!NOTE]
>Attributes inside one square-bracket group are merged into one binding. To create several bindings for the same implementation type, place `Bind` attributes in separate square-bracket groups.
This is useful when one adapter has several roles: for example, it can be part of a normal processing pipeline and also be exposed through a tagged operational pipeline with another lifetime or contract.
In this scenario, all bindings are declared with attributes, and the setup keeps only composition roots.

## Decorator

Interception is the ability to intercept calls between objects in order to enrich or change their behavior, but without having to change their code. A prerequisite for interception is weak binding. That is, if programming is abstraction-based, the underlying implementation can be transformed or improved by "packaging" it into other implementations of the same abstraction. At its core, intercept is an application of the Decorator design pattern. This pattern provides a flexible alternative to inheritance by dynamically "attaching" additional responsibility to an object. Decorator "packs" one implementation of an abstraction into another implementation of the same abstraction like a "matryoshka doll".
`Decorator` is a well-known and useful design pattern. It is convenient to use tagged dependencies to build a chain of nested decorators, as in the example below:

```c#
using Shouldly;
using Pure.DI;

DI.Setup(nameof(Composition))
    .Bind("base").To<TextWidget>()
    .Bind().To<BoxWidget>()
    .Root<IWidget>("Widget");

var composition = new Composition();
var widget = composition.Widget;
widget.Render().ShouldBe("[ Hello World ]");

interface IWidget
{
    string Render();
}

class TextWidget : IWidget
{
    public string Render() => "Hello World";
}

class BoxWidget([Tag("base")] IWidget baseWidget) : IWidget
{
    public string Render() => $"[ {baseWidget.Render()} ]";
}
```

To run the above code, the following NuGet packages must be added:
 - [Pure.DI](https://www.nuget.org/packages/Pure.DI)
 - [Shouldly](https://www.nuget.org/packages/Shouldly)

Here an instance of the `TextWidget` type, labeled `"base"`, is injected in the decorator `BoxWidget`. You can use any tag that semantically reflects the feature of the abstraction being embedded. The tag can be a constant, a type, or a value of an enumerated type.

## Interception

Interception lets you enrich or change the behavior of a certain set of objects from the object graph being created without changing the code of the corresponding types.

```c#
using Shouldly;
using Castle.DynamicProxy;
using System.Runtime.CompilerServices;
using Pure.DI;

// OnDependencyInjection = On
// OnDependencyInjectionContractTypeNameWildcard = *IGreeter
DI.Setup(nameof(Composition))
    .Bind().To<Greeter>()
    .Root<IGreeter>("Greeter");

var composition = new Composition();
var greeter = composition.Greeter;

// The greeting is modified by the interceptor
greeter.Greet("World").ShouldBe("Hello World !!!");

public interface IGreeter
{
    string Greet(string name);
}

class Greeter : IGreeter
{
    public string Greet(string name) => $"Hello {name}";
}

partial class Composition : IInterceptor
{
    private static readonly ProxyGenerator ProxyGenerator = new();

    // Intercepts the instantiation of services to wrap them in a proxy
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private partial T OnDependencyInjection<T>(
        in T value,
        object? tag,
        Lifetime lifetime)
    {
        // Proxying is only possible for reference types (interfaces, classes)
        if (typeof(T).IsValueType)
        {
            return value;
        }

        // Creates a proxy that delegates calls to the 'value' object
        // and passes them through the 'this' interceptor
        return (T)ProxyGenerator.CreateInterfaceProxyWithTargetInterface(
            typeof(T),
            value,
            this);
    }

    // Logic performed when a method on the proxy is called
    public void Intercept(IInvocation invocation)
    {
        // Executes the original method
        invocation.Proceed();

        // Enhances the result of the Greet method
        if (invocation.Method.Name == nameof(IGreeter.Greet)
            && invocation.ReturnValue is string message)
        {
            invocation.ReturnValue = $"{message} !!!";
        }
    }
}
```

To run the above code, the following NuGet packages must be added:
 - [Pure.DI](https://www.nuget.org/packages/Pure.DI)
 - [Shouldly](https://www.nuget.org/packages/Shouldly)
 - [Castle.DynamicProxy](https://www.nuget.org/packages/Castle.DynamicProxy)

Using an intercept gives you the ability to add end-to-end functionality such as:

- Logging

- Action logging

- Performance monitoring

- Security

- Caching

- Error handling

- Providing resistance to failures, etc.

## Resolve hint

Hints are used to fine-tune code generation. The `Resolve` hint determines whether to generate `Resolve` methods. By default, a set of four `Resolve` methods are generated. Set this hint to `Off` to disable the generation of resolve methods. This will reduce class composition generation time, and no anonymous composition roots will be generated in this case. When the `Resolve` hint is disabled, only the regular root properties are available, so be sure to define them explicitly with the `Root<T>(...)` method.
In addition, setup hints can be comments before the `Setup` method in the form `hint = value`, for example: `// Resolve = Off`.

```c#
using Pure.DI;
using static Pure.DI.Hint;

DI.Setup(nameof(Composition))
    .Hint(Resolve, "Off")
    .Bind().To<DatabaseService>()
    // When the "Resolve" hint is disabled, only the regular root properties
    // are available, so be sure to define them explicitly
    // with the "Root<T>(...)" method.
    .Root<IDatabaseService>("DatabaseService")
    .Bind().To<UserService>()
    .Root<IUserService>("UserService");

var composition = new Composition();

// The "Resolve" method is not generated,
// so we can only access the roots through properties:
var userService = composition.UserService;
var databaseService = composition.DatabaseService;

// composition.Resolve<IUserService>(); // Compile error

interface IDatabaseService;

class DatabaseService : IDatabaseService;

interface IUserService;

class UserService(IDatabaseService database) : IUserService;
```

To run the above code, the following NuGet package must be added:
 - [Pure.DI](https://www.nuget.org/packages/Pure.DI)

For more hints, see [this](../README.md#setup-hints) page.

## ThreadSafe hint

Hints are used to fine-tune code generation. The `ThreadSafe` hint determines whether object composition will be created in a thread-safe manner. This hint is `On` by default. It is good practice not to use threads when creating an object graph, in which case this hint can be turned off, which will lead to a slight increase in performance.
In addition, setup hints can be comments before the `Setup` method in the form `hint = value`, for example: `// ThreadSafe = Off`.

```c#
using Pure.DI;
using static Pure.DI.Hint;

DI.Setup(nameof(Composition))
    // Disabling thread-safety can improve performance.
    // This is safe when the object graph is resolved on a single thread,
    // for example at application startup.
    .Hint(ThreadSafe, "Off")
    .Bind().To<SqlDatabaseConnection>()
    .Bind().As(Lifetime.Singleton).To<ReportGenerator>()
    .Root<IReportGenerator>("Generator");

var composition = new Composition();
var reportGenerator = composition.Generator;

interface IDatabaseConnection;

class SqlDatabaseConnection : IDatabaseConnection;

interface IReportGenerator;

class ReportGenerator(Func<IDatabaseConnection> connectionFactory) : IReportGenerator;
```

To run the above code, the following NuGet package must be added:
 - [Pure.DI](https://www.nuget.org/packages/Pure.DI)

For more hints, see [this](../README.md#setup-hints) page.

## OnDependencyInjection regular expression hint

Hints are used to fine-tune code generation. The `OnDependencyInjection` hint determines whether to generate partial `OnDependencyInjection` method to control of dependency injection.
In addition, setup hints can be comments before the `Setup` method in the form `hint = value`, for example: `// OnDependencyInjection = On`.

```c#
using Shouldly;
using Pure.DI;
using static Pure.DI.Hint;

// OnDependencyInjection = On
DI.Setup(nameof(Composition))
    // Filters types by regular expression to control which types trigger the OnDependencyInjection method.
    // In this case, we want to intercept the injection of any "Gateway" (like IPaymentGateway)
    // and integer configuration values.
    .Hint(OnDependencyInjectionContractTypeNameRegularExpression, "(.*Gateway|int)$")
    .RootArg<int>("maxAttempts")
    .Bind().To<PayPalGateway>()
    .Bind().To<PaymentService>()
    .Root<IPaymentService>("GetPaymentService");

var log = new List<string>();
var composition = new Composition(log);

// Resolving the root service triggers the injection chain.
// 1. int maxAttempts is injected into PayPalGateway.
// 2. PayPalGateway is injected into PaymentService.
// PaymentService itself is not logged because "IPaymentService" does not match the regex.
var service = composition.GetPaymentService(3);

log.ShouldBe([
    "Int32 injected",
    "PayPalGateway injected"
]);

interface IPaymentGateway;

record PayPalGateway(int MaxAttempts) : IPaymentGateway;

interface IPaymentService
{
    IPaymentGateway Gateway { get; }
}

class PaymentService(IPaymentGateway gateway) : IPaymentService
{
    public IPaymentGateway Gateway { get; } = gateway;
}

partial class Composition(List<string> log)
{
    private partial T OnDependencyInjection<T>(
        in T value,
        object? tag,
        Lifetime lifetime)
    {
        // Logs the actual runtime type of the injected instance
        log.Add($"{value?.GetType().Name} injected");
        return value;
    }
}
```

To run the above code, the following NuGet packages must be added:
 - [Pure.DI](https://www.nuget.org/packages/Pure.DI)
 - [Shouldly](https://www.nuget.org/packages/Shouldly)

The `OnDependencyInjectionContractTypeNameRegularExpression` hint helps identify the set of types that require injection control. You can use it to specify a regular expression to filter the full name of a type.
For more hints, see [this](../README.md#setup-hints) page.

## OnDependencyInjection wildcard hint

Hints are used to fine-tune code generation. The `OnDependencyInjection` hint determines whether to generate partial `OnDependencyInjection` method to control of dependency injection.
In addition, setup hints can be comments before the `Setup` method in the form `hint = value`, for example: `// OnDependencyInjection = On`.

```c#
using Shouldly;
using Pure.DI;
using static Pure.DI.Hint;

// OnDependencyInjection = On
DI.Setup(nameof(Composition))
    .Hint(OnDependencyInjectionContractTypeNameWildcard, "*IUserRepository")
    .Hint(OnDependencyInjectionContractTypeNameWildcard, "*IUserService")
    .RootArg<int>("id")
    .Bind().To<UserRepository>()
    .Bind().To<UserService>()
    .Root<IUserService>("GetUserService");

var log = new List<string>();
var composition = new Composition(log);
var service = composition.GetUserService(33);

log.ShouldBe([
    "UserRepository injected",
    "UserService injected"
]);

interface IUserRepository;

record UserRepository(int Id) : IUserRepository;

interface IUserService
{
    IUserRepository Repository { get; }
}

class UserService(IUserRepository repository) : IUserService
{
    public IUserRepository Repository { get; } = repository;
}

partial class Composition(List<string> log)
{
    private partial T OnDependencyInjection<T>(
        in T value,
        object? tag,
        Lifetime lifetime)
    {
        log.Add($"{value?.GetType().Name} injected");
        return value;
    }
}
```

To run the above code, the following NuGet packages must be added:
 - [Pure.DI](https://www.nuget.org/packages/Pure.DI)
 - [Shouldly](https://www.nuget.org/packages/Shouldly)

The `OnDependencyInjectionContractTypeNameWildcard` hint helps identify the set of types that require injection control. You can use it to specify a wildcard to filter the full name of a type.
For more hints, see [this](../README.md#setup-hints) page.

## OnCannotResolve regular expression hint

Hints are used to fine-tune code generation. The `OnCannotResolve` hint determines whether to generate a partial `OnCannotResolve<T>(...)` method to handle a scenario where an instance which cannot be resolved.
In addition, setup hints can be comments before the `Setup` method in the form `hint = value`, for example: `// OnCannotResolveContractTypeNameRegularExpression = string`.

```c#
using Shouldly;
using Pure.DI;
using static Pure.DI.Hint;

// The "OnCannotResolveContractTypeNameRegularExpression" hint defines a regular expression
// to filter the full type name of unresolved dependencies.
// In this case, we want to manually handle only "string" types.
// OnCannotResolveContractTypeNameRegularExpression = string
DI.Setup(nameof(Composition))
    .Hint(OnCannotResolve, "On")
    .Bind().To<DatabaseAccess>()
    .Bind().To<BusinessService>()
    .Root<IBusinessService>("BusinessService");

var composition = new Composition();
var businessService = composition.BusinessService;

// Check that the connection string was successfully injected via OnCannotResolve
businessService.DatabaseAccess.ConnectionString.ShouldBe("Server=localhost;Database=MyDb;");


interface IDatabaseAccess
{
    string ConnectionString { get; }
}

// A service requiring a connection string.
// The connection string is a primitive type 'string' that is not bound in the DI setup.
// It will be resolved via the 'OnCannotResolve' fallback method.
class DatabaseAccess(string connectionString) : IDatabaseAccess
{
    public string ConnectionString { get; } = connectionString;
}

interface IBusinessService
{
    IDatabaseAccess DatabaseAccess { get; }
}

class BusinessService(IDatabaseAccess databaseAccess) : IBusinessService
{
    public IDatabaseAccess DatabaseAccess { get; } = databaseAccess;
}

partial class Composition
{
    // This method is called when a dependency cannot be resolved by the standard DI.
    // It serves as a fallback mechanism.
    private partial T OnCannotResolve<T>(
        object? tag,
        Lifetime lifetime)
    {
        // Check if the requested type is a string (according to the hint filter)
        if (typeof(T) == typeof(string))
        {
            // Provide the configuration value (e.g., loaded from a file)
            return (T)(object)"Server=localhost;Database=MyDb;";
        }

        throw new InvalidOperationException("Cannot resolve " + typeof(T));
    }
}
```

To run the above code, the following NuGet packages must be added:
 - [Pure.DI](https://www.nuget.org/packages/Pure.DI)
 - [Shouldly](https://www.nuget.org/packages/Shouldly)

The `OnCannotResolveContractTypeNameRegularExpression` hint helps define the set of types that require manual dependency resolution. You can use it to specify a regular expression to filter the full type name.
For more hints, see [this](../README.md#setup-hints) page.

## OnCannotResolve wildcard hint

Hints are used to fine-tune code generation. The `OnCannotResolve` hint determines whether to generate a partial `OnCannotResolve<T>(...)` method to handle a scenario where an instance which cannot be resolved.
In addition, setup hints can be comments before the `Setup` method in the form `hint = value`, for example: `// OnCannotResolveContractTypeNameWildcard = string`.

```c#
using Shouldly;
using Pure.DI;
using static Pure.DI.Hint;

// OnCannotResolveContractTypeNameWildcard = string
DI.Setup(nameof(Composition))
    .Hint(OnCannotResolve, "On")
    .Bind().To<DatabaseSettings>()
    .Bind().To<DataService>()
    .Root<IDataService>("DataService");

var composition = new Composition();
var dataService = composition.DataService;
dataService.Settings.ConnectionString.ShouldBe("Server=localhost;");


interface IDatabaseSettings
{
    string ConnectionString { get; }
}

class DatabaseSettings(string connectionString) : IDatabaseSettings
{
    public string ConnectionString { get; } = connectionString;
}

interface IDataService
{
    IDatabaseSettings Settings { get; }
}

class DataService(IDatabaseSettings settings) : IDataService
{
    public IDatabaseSettings Settings { get; } = settings;
}

partial class Composition
{
    private partial T OnCannotResolve<T>(
        object? tag,
        Lifetime lifetime)
    {
        // Emulates obtaining a configuration value
        if (typeof(T) == typeof(string))
        {
            return (T)(object)"Server=localhost;";
        }

        throw new InvalidOperationException("Cannot resolve.");
    }
}
```

To run the above code, the following NuGet packages must be added:
 - [Pure.DI](https://www.nuget.org/packages/Pure.DI)
 - [Shouldly](https://www.nuget.org/packages/Shouldly)

The `OnCannotResolveContractTypeNameWildcard` hint helps define the set of types that require manual dependency resolution. You can use it to specify a wildcard to filter the full type name.
For more hints, see [this](../README.md#setup-hints) page.

## OnNewInstance regular expression hint

Hints are used to fine-tune code generation. The `OnNewInstance` hint determines whether to generate partial `OnNewInstance` method.
In addition, setup hints can be comments before the `Setup` method in the form `hint = value`, for example: `// OnNewInstance = On`.

```c#
using Shouldly;
using Pure.DI;
using static Pure.DI.Hint;

DI.Setup(nameof(Composition))
    .Hint(OnNewInstance, "On")
    .Hint(OnNewInstanceLifetimeRegularExpression, "(Singleton|PerBlock)")
    .Bind().As(Lifetime.Singleton).To<GlobalCache>()
    .Bind().As(Lifetime.PerBlock).To<OrderProcessor>()
    .Root<IOrderProcessor>("OrderProcessor");

var log = new List<string>();
var composition = new Composition(log);

// Create the OrderProcessor twice
var processor1 = composition.OrderProcessor;
var processor2 = composition.OrderProcessor;

log.ShouldBe([
    "GlobalCache created",
    "OrderProcessor created",
    "OrderProcessor created"
]);

interface IGlobalCache;

class GlobalCache : IGlobalCache;

interface IOrderProcessor
{
    IGlobalCache Cache { get; }
}

class OrderProcessor(IGlobalCache cache) : IOrderProcessor
{
    public IGlobalCache Cache { get; } = cache;
}

internal partial class Composition(List<string> log)
{
    partial void OnNewInstance<T>(
        ref T value,
        object? tag,
        Lifetime lifetime) =>
        log.Add($"{typeof(T).Name} created");
}
```

To run the above code, the following NuGet packages must be added:
 - [Pure.DI](https://www.nuget.org/packages/Pure.DI)
 - [Shouldly](https://www.nuget.org/packages/Shouldly)

The `OnNewInstanceLifetimeRegularExpression` hint helps you define a set of lifetimes that require instance creation control. You can use it to specify a regular expression to filter bindings by lifetime name.
For more hints, see [this](../README.md#setup-hints) page.

## OnNewInstance wildcard hint

Hints are used to fine-tune code generation. The `OnNewInstance` hint determines whether to generate partial `OnNewInstance` method.
In addition, setup hints can be comments before the `Setup` method in the form `hint = value`, for example: `// OnNewInstance = On`.

```c#
using Shouldly;
using Pure.DI;
using static Pure.DI.Hint;

DI.Setup(nameof(Composition))
    .Hint(OnNewInstance, "On")
    // Hints restrict the generation of the partial OnNewInstance method
    // to only those types whose names match the specified wildcards.
    // In this case, we want to track the creation of repositories and services.
    .Hint(OnNewInstanceImplementationTypeNameWildcard, "*Repository")
    .Hint(OnNewInstanceImplementationTypeNameWildcard, "*Service")
    .Bind().As(Lifetime.Singleton).To<UserRepository>()
    .Bind().To<OrderService>()
    // This type will not be tracked because its name
    // does not match the wildcards
    .Bind().To<ConsoleLogger>()
    .Root<IOrderService>("Root");

var log = new List<string>();
var composition = new Composition(log);

var service1 = composition.Root;
var service2 = composition.Root;

log.ShouldBe([
    "UserRepository created",
    "OrderService created",
    "OrderService created"
]);

interface IRepository;

class UserRepository : IRepository
{
    public override string ToString() => nameof(UserRepository);
}

interface ILogger;

class ConsoleLogger : ILogger
{
    public override string ToString() => nameof(ConsoleLogger);
}

interface IOrderService
{
    IRepository Repository { get; }
}

class OrderService(IRepository repository, ILogger logger) : IOrderService
{
    public IRepository Repository { get; } = repository;

    public ILogger Logger { get; } = logger;

    public override string ToString() => nameof(OrderService);
}

internal partial class Composition(List<string> log)
{
    partial void OnNewInstance<T>(
        ref T value,
        object? tag,
        Lifetime lifetime) =>
        log.Add($"{typeof(T).Name} created");
}
```

To run the above code, the following NuGet packages must be added:
 - [Pure.DI](https://www.nuget.org/packages/Pure.DI)
 - [Shouldly](https://www.nuget.org/packages/Shouldly)

The `OnNewInstanceImplementationTypeNameWildcard` hint helps you define a set of implementation types that require instance creation control. You can use it to specify a wildcard to filter bindings by implementation name.
For more hints, see [this](../README.md#setup-hints) page.

## ToString hint

Hints are used to fine-tune code generation. The `ToString` hint determines if the `ToString()` method should be generated. This method provides a text-based class diagram in the format [_mermaid_](https://mermaid.js.org/). To see this diagram, just call the `ToString` method and copy the text to [this site](https://mermaid.live/). An example class diagram can be seen below.
In addition, setup hints can be comments before the `Setup` method in the form `hint = value`, for example: `// ToString = On`.

```c#
using Pure.DI;

DI.Setup(nameof(Composition))
    .Hint(Hint.ToString, "On")
    .Bind().To<Database>()
    .Bind().To<UserRepository>()
    .Root<IUserRepository>("GetUserRepository");

var composition = new Composition();
// The ToString() method generates a class diagram in mermaid format
string classDiagram = composition.ToString();

interface IDatabase;

class Database : IDatabase;

interface IUserRepository;

class UserRepository(IDatabase database) : IUserRepository;
```

To run the above code, the following NuGet package must be added:
 - [Pure.DI](https://www.nuget.org/packages/Pure.DI)

Developers who start using DI technology often complain that they stop seeing the structure of the application because it is difficult to understand how it is built. To make life easier, you can add the `ToString` hint by telling the generator to create a `ToString()` method.
For more hints, see [this](../README.md#setup-hints) page.

## Check for a root

Sometimes you need to check if you can get the root of a composition using the `Resolve` method before calling it, this example will show you how to do it:

```c#
using Shouldly;
using Pure.DI;

// Check if the main user service is registered
Composition.HasRoot(typeof(IUserService)).ShouldBeTrue();

// Check if the root dependency for the repository with the "Primary" tag exists
Composition.HasRoot(typeof(IUserRepository), "Primary").ShouldBeTrue();

// Verify that the abstract repository without a tag is NOT registered as a root
Composition.HasRoot(typeof(IUserRepository)).ShouldBeFalse();
Composition.HasRoot(typeof(IComparable)).ShouldBeFalse();


// Repository interface for user data access
interface IUserRepository;

// Concrete repository implementation (e.g., SQL Database)
class SqlUserRepository : IUserRepository;

// Business service interface
interface IUserService
{
    IUserRepository Repository { get; }
}

// Service requiring a specific repository implementation
class UserService : IUserService
{
    // Use the "Primary" tag to specify which database to use
    [Tag("Primary")]
    public required IUserRepository Repository { get; init; }
}

partial class Composition
{
    private static readonly HashSet<(Type type, object? tag)> Roots = [];

    // The method checks if the type can be resolved without actually creating the object.
    // Useful for diagnostics.
    internal static bool HasRoot(Type type, object? key = null) =>
        Roots.Contains((type, key));

    static void Setup() =>
        DI.Setup()
            // Specifies to use the partial OnNewRoot method to register roots
            .Hint(Hint.OnNewRoot, "On")

            // Registers the repository implementation with the "Primary" tag
            .Bind("Primary").To<SqlUserRepository>()
            .Bind().To<UserService>()

            // Defines composition roots
            .Root<IUserRepository>(tag: "Primary")
            .Root<IUserService>("Root");

    // Adds a new root to the HashSet during code generation
    private static partial void OnNewRoot<TContract, T>(
        IResolver<Composition, TContract> resolver,
        string name,
        object? tag,
        Lifetime lifetime) =>
        Roots.Add((typeof(TContract), tag));
}
```

To run the above code, the following NuGet packages must be added:
 - [Pure.DI](https://www.nuget.org/packages/Pure.DI)
 - [Shouldly](https://www.nuget.org/packages/Shouldly)

For more hints, see [this](../README.md#setup-hints) page.

## Generate an interface from a class

This example shows how a concrete service can generate a matching interface and be consumed through Pure.DI.

```c#
using Shouldly;
using Pure.DI;

DI.Setup(nameof(Composition))
    .Bind().To<EmailSender>()
    .Root<App>(nameof(App));

var composition = new Composition();
var app = composition.App;

app.Provider.ShouldBe("smtp");
app.Result.ShouldBe("sent:ops@contoso.com");

public partial interface IEmailSender;

[GenerateInterface]
public class EmailSender : IEmailSender
{
    public string Provider => "smtp";

    public string Send(string address) => $"sent:{address}";
}

public class App(IEmailSender sender)
{
    public string Provider { get; } = sender.Provider;

    public string Result { get; } = sender.Send("ops@contoso.com");
}
```

To run the above code, the following NuGet packages must be added:
 - [Pure.DI](https://www.nuget.org/packages/Pure.DI)
 - [Shouldly](https://www.nuget.org/packages/Shouldly)

The example shows how to:
- Generate an interface from a class
- Bind the generated contract in Pure.DI
- Resolve a consumer that depends on the interface

## Ignore members in the generated interface

This example shows how to exclude internal-only members from a generated interface.

```c#
using Shouldly;
using Pure.DI;

DI.Setup(nameof(Composition))
    .Bind().To<ApiClient>()
    .Root<App>(nameof(App));

var composition = new Composition();
var app = composition.App;

app.Endpoint.ShouldBe("https://api.contoso.com");

public partial interface IApiClient;

[GenerateInterface]
public class ApiClient : IApiClient
{
    public string Endpoint => "https://api.contoso.com";

    [IgnoreInterface]
    public string GetAccessToken() => "internal-token";
}

public class App(IApiClient client)
{
    public string Endpoint { get; } = client.Endpoint;
}
```

To run the above code, the following NuGet packages must be added:
 - [Pure.DI](https://www.nuget.org/packages/Pure.DI)
 - [Shouldly](https://www.nuget.org/packages/Shouldly)

The example shows how to:
- Mark members with IgnoreInterface
- Keep only the intended contract surface
- Use the generated interface in Pure.DI

## Generate interfaces with generics

This example shows how generic members, nullable annotations, and events are preserved in a reporting scenario.

```c#
using Shouldly;
using Pure.DI;

DI.Setup(nameof(Composition))
    .Bind().To<ReportFormatter>()
    .Root<App>(nameof(App));

var composition = new Composition();
var app = composition.App;

app.Formatted.ShouldBe("Order #42");
app.Title.ShouldBe("Daily Report");

public partial interface IReportFormatter;

[GenerateInterface]
public class ReportFormatter : IReportFormatter
{
    public string? Title { get; set; } = "Daily Report";

    public event EventHandler? Changed;

    public string? Format<T>(T value)
        where T : class
        => value.ToString();

    [IgnoreInterface]
    public void Hidden() { }
}

public class App(IReportFormatter formatter)
{
    public string Title { get; } = formatter.Title ?? string.Empty;

    public string Formatted { get; } = formatter.Format(new Order(42)) ?? string.Empty;
}

public class Order(int id)
{
    public override string ToString() => $"Order #{id}";
}
```

To run the above code, the following NuGet packages must be added:
 - [Pure.DI](https://www.nuget.org/packages/Pure.DI)
 - [Shouldly](https://www.nuget.org/packages/Shouldly)

The example shows how to:
- Generate an interface for generic members
- Preserve nullable annotations
- Preserve events and generic constraints

## Customize the generated interface

This example shows how to place a generated contract in a dedicated Contracts namespace.

```c#
DI.Setup(nameof(Composition))
    .Bind().To<InvoiceGenerator>()
    .Root<App>(nameof(App));

var composition = new Composition();
var app = composition.App;

app.InvoiceId.ShouldBe("INV-0042");

public class App(IMyInvoiceGenerator generator)
{
    public string InvoiceId { get; } = generator.Format(42);
}

namespace Contracts
{
    public partial interface IMyInvoiceGenerator;

    [GenerateInterface(namespaceName: "Contracts", interfaceName: nameof(IMyInvoiceGenerator))]
    public class InvoiceGenerator : IMyInvoiceGenerator
    {
        public string Format(int number) => $"INV-{number:0000}";
    }
}
```

To run the above code, the following NuGet package must be added:
 - [Pure.DI](https://www.nuget.org/packages/Pure.DI)

The example shows how to:
- Generate an interface into a custom namespace
- Rename the generated interface
- Keep the contract separate from implementation details

## Generate several interfaces from one class

This example shows how one implementation can generate multiple interfaces with shared and selective members.

```c#
using Shouldly;
using Pure.DI;

DI.Setup(nameof(Composition))
    .Bind().To<Gateway>()
    .Root<App>(nameof(App));

var composition = new Composition();
var app = composition.App;

app.ReadResult.ShouldBe("GET:/orders");
app.SharedResult.ShouldBe("ok");

public partial interface IReadGateway;

public partial interface IWriteGateway;

[GenerateInterface(interfaceName: nameof(IReadGateway))]
[GenerateInterface(interfaceName: nameof(IWriteGateway))]
public class Gateway : IReadGateway, IWriteGateway
{
    [GenerateInterface(interfaceName: nameof(IReadGateway))]
    public string Get(string path) => $"GET:{path}";

    [GenerateInterface(interfaceName: nameof(IWriteGateway))]
    public void Post(string path)
    {
    }

    [GenerateInterface(interfaceName: nameof(IReadGateway))]
    [GenerateInterface(interfaceName: nameof(IWriteGateway))]
    public string Ping() => "ok";
}

public class App(IReadGateway readGateway, IWriteGateway writeGateway)
{
    public string ReadResult { get; } = readGateway.Get("/orders");

    public string SharedResult { get; } = writeGateway.Ping();
}
```

To run the above code, the following NuGet packages must be added:
 - [Pure.DI](https://www.nuget.org/packages/Pure.DI)
 - [Shouldly](https://www.nuget.org/packages/Shouldly)

The example shows how to:
- Generate multiple interfaces from one class
- Select members per interface using member attributes
- Reuse one member in several generated interfaces

## Control generated interfaces by members

This example shows selective member inclusion and IgnoreInterface priority when generating interfaces.

```c#
using Shouldly;
using Pure.DI;

DI.Setup(nameof(Composition))
    .Bind().To<ProfileService>()
    .Root<App>(nameof(App));

var composition = new Composition();
var app = composition.App;

app.Summary.ShouldBe("42:Ada");

public partial interface IProfileReader;

public partial interface IProfileWriter;

public class ProfileService : IProfileReader, IProfileWriter
{
    [GenerateInterface(interfaceName: nameof(IProfileReader))]
    public int GetId() => 42;

    [GenerateInterface(interfaceName: nameof(IProfileWriter))]
    public void Rename(string name)
    {
    }

    [GenerateInterface(interfaceName: nameof(IProfileWriter))]
    [IgnoreInterface]
    public string Secret() => "hidden";

    [GenerateInterface(interfaceName: nameof(IProfileReader))]
    public string GetName() => "Ada";
}

public class App(IProfileReader reader)
{
    public string Summary { get; } = $"{reader.GetId()}:{reader.GetName()}";
}
```

To run the above code, the following NuGet packages must be added:
 - [Pure.DI](https://www.nuget.org/packages/Pure.DI)
 - [Shouldly](https://www.nuget.org/packages/Shouldly)

The example shows how to:
- Generate interface members selectively
- Keep class-level generation settings
- Exclude explicitly ignored members from all generated interfaces

## Composition root kinds

By default, a composition root is a public instance property, but the `kind` argument of `Root<T>(...)` lets you change that. Combine `RootKinds` flags to generate the root as a method instead of a property, adjust its visibility (`Public`, `Internal`, `Private`), or make it `Static` or `Partial`.
A private partial root is useful when you want to wrap the generated code in your own hand-written member, as the `PaymentService` property does here.

```c#
using Shouldly;
using Pure.DI;
using static Pure.DI.RootKinds;

var composition = new Composition();
var paymentService = composition.PaymentService;
var cashPaymentService = composition.GetCashPaymentService();
var validator = Composition.Validator;

interface ICreditCardValidator;

class LuhnValidator : ICreditCardValidator;

interface IPaymentService;

class CardPaymentService : IPaymentService
{
    public CardPaymentService(ICreditCardValidator validator)
    {
    }
}

class CashPaymentService : IPaymentService;

partial class Composition
{
    void Setup() =>
        DI.Setup(nameof(Composition))
            .Bind<IPaymentService>().To<CardPaymentService>()
            .Bind<IPaymentService>("Cash").To<CashPaymentService>()
            .Bind<ICreditCardValidator>().To<LuhnValidator>()

            // Creates a public root method named "GetCashPaymentService"
            .Root<IPaymentService>("GetCashPaymentService", "Cash", Public | Method)

            // Creates a private partial root method named "GetCardPaymentService"
            .Root<IPaymentService>("GetCardPaymentService", kind: Private | Partial | Method)

            // Creates an internal static root named "Validator"
            .Root<ICreditCardValidator>("Validator", kind: Internal | Static);

    private partial IPaymentService GetCardPaymentService();

    public IPaymentService PaymentService => GetCardPaymentService();
}
```

To run the above code, the following NuGet packages must be added:
 - [Pure.DI](https://www.nuget.org/packages/Pure.DI)
 - [Shouldly](https://www.nuget.org/packages/Shouldly)

>[!NOTE]
>Composition roots can be customized with different kinds to control accessibility and lifetime, enabling flexible API design patterns.

## Root with name template

Instead of a fixed root name, `Root<T>()` accepts a name template where the `{type}` placeholder is replaced with the dependency's type name. This is handy when you declare many roots and want them to follow a consistent naming convention: the template `"My{type}"` here produces a root property named `MyApiClient`.

```c#
using Shouldly;
using Pure.DI;

DI.Setup("Composition")
    // The name template "My{type}" specifies that the root property name
    // will be formed by adding the prefix "My" to the type name "ApiClient".
    .Root<ApiClient>("My{type}");

var composition = new Composition();

// The property name is "MyApiClient" instead of "ApiClient"
// thanks to the name template "My{type}"
var apiClient = composition.MyApiClient;

apiClient.GetProfile().ShouldBe("Content from https://example.com/profile");

class NetworkClient
{
    public string Get(string uri) => $"Content from {uri}";
}

class ApiClient(NetworkClient client)
{
    public string GetProfile() => client.Get("https://example.com/profile");
}
```

To run the above code, the following NuGet packages must be added:
 - [Pure.DI](https://www.nuget.org/packages/Pure.DI)
 - [Shouldly](https://www.nuget.org/packages/Shouldly)

>[!NOTE]
>Name templates provide flexibility in root naming but should be used consistently to maintain code readability.

## Light roots

Light roots optimize code generation by avoiding the creation of separate composition objects for each root. Instead, they share a common lightweight composition and use delegates to create instances. This is particularly useful for simple, frequently resolved roots where the overhead of generating separate compositions outweighs the benefits. Anonymous roots (roots without explicit names) are lightweight by default.

```c#
using Shouldly;
using Pure.DI;
using static Pure.DI.RootKinds;

DI.Setup(nameof(Composition))
    // Infrastructure bindings with simple lifetimes
    .Bind().To<ConsoleLogger>()
    .Bind().To<MemoryCache>()
    .Bind().To<PrometheusMetrics>()
    .Bind().To<AppConfiguration>()
    .Bind().To<ApplicationService>()

    // Regular root for complex composition
    .Root<IApplicationService>("ApplicationService")

    // Named lightweight root
    .Root<IConfiguration>("Config", kind: Light)

    // Anonymous lightweight roots (lightweight by default)
    .Root<ILogger>()
    .Root<ICache>()
    .Root<IMetrics>();

var composition = new Composition();
var applicationService = composition.ApplicationService;
var config = composition.Config;

// Anonymous roots are resolved via the Resolve method
var logger = composition.Resolve<ILogger>();
var cache = composition.Resolve<ICache>();
var metrics = composition.Resolve<IMetrics>();

// Verify that all light roots return correct types
logger.ShouldBeOfType<ConsoleLogger>();
cache.ShouldBeOfType<MemoryCache>();
metrics.ShouldBeOfType<PrometheusMetrics>();
config.ShouldBeOfType<AppConfiguration>();

// Light roots can be resolved independently without complex composition overhead
var anotherLogger = composition.Resolve<ILogger>();
anotherLogger.ShouldNotBeSameAs(logger);

// Application service with complex dependencies
interface IApplicationService;

class ApplicationService(
    ILogger logger,
    ICache cache,
    IMetrics metrics,
    IConfiguration config)
    : IApplicationService;

// Simple logger interface and implementation
interface ILogger;

class ConsoleLogger : ILogger;

// Simple cache interface and implementation
interface ICache;

class MemoryCache : ICache;

// Simple metrics interface and implementation
interface IMetrics;

class PrometheusMetrics : IMetrics;

// Simple configuration interface and implementation
interface IConfiguration;

class AppConfiguration : IConfiguration;
```

To run the above code, the following NuGet packages must be added:
 - [Pure.DI](https://www.nuget.org/packages/Pure.DI)
 - [Shouldly](https://www.nuget.org/packages/Shouldly)

>[!NOTE]
>Light roots are ideal for simple services, factories, or utilities that don't require complex dependency graphs. They reduce generated code size and improve compilation time.

## Partial class

The composition class is generated as a partial class, so you can put the setup code in your own part of it and extend the generated code with hand-written members. Here the custom part adds a constructor accepting `storeName` and a `GenerateId()` method, and both are used directly inside factory bindings.

```c#
using Shouldly;
using Pure.DI;
using static Pure.DI.RootKinds;
using System.Diagnostics;

var composition = new Composition("NorthStore");
var orderService = composition.OrderService;

// Checks that the dependencies were created correctly
orderService.Order1.Id.ShouldBe(1);
orderService.Order2.Id.ShouldBe(2);
// Checks that the injected string contains the store name and the generated ID
orderService.OrderDetails.ShouldBe("NorthStore_3");

interface IOrder
{
    long Id { get; }
}

class Order(long id) : IOrder
{
    public long Id { get; } = id;
}

class OrderService(
    [Tag("Order details")] string details,
    IOrder order1,
    IOrder order2)
{
    public string OrderDetails { get; } = details;

    public IOrder Order1 { get; } = order1;

    public IOrder Order2 { get; } = order2;
}

// The partial class is also useful for specifying access modifiers to the generated class
public partial class Composition(string storeName)
{
    private long _id;

    private long GenerateId() => Interlocked.Increment(ref _id);

    // In fact, this method will not be called at runtime
    [Conditional("DI")]
    void Setup() =>

        DI.Setup()
            .Bind<IOrder>().To<Order>()
            .Bind<long>().To(GenerateId)
            // Binds the string with the tag "Order details"
            .Bind<string>("Order details").To(() => $"{storeName}_{GenerateId()}")
            .Root<OrderService>("OrderService", kind: Internal);
}
```

To run the above code, the following NuGet packages must be added:
 - [Pure.DI](https://www.nuget.org/packages/Pure.DI)
 - [Shouldly](https://www.nuget.org/packages/Shouldly)

The partial class is also useful for specifying access modifiers to the generated class.

## A few partial classes

The setting code for one Composition can be located in several methods and/or in several partial classes.

```c#
using Shouldly;
using Pure.DI;

var composition = new Composition();
var commenter = composition.Commenter;

// Infrastructure interface for retrieving comments (e.g., from a database)
interface IComments;

class Comments : IComments;

// Domain service for handling class comments
interface IClassCommenter;

class ClassCommenter(IComments comments) : IClassCommenter;

partial class Composition
{

    // Infrastructure layer setup.
    // This method isolates the configuration of databases or external services.
    static void SetupInfrastructure() =>
        DI.Setup()
            .Bind<IComments>().To<Comments>();
}

partial class Composition
{
    // Domain logic layer setup.
    // Here we bind domain services.
    static void SetupDomain() =>
        DI.Setup()
            .Bind<IClassCommenter>().To<ClassCommenter>();
}

partial class Composition
{
    // Public API setup (Composition Roots).
    // Determines which objects can be retrieved directly from the composition.
    private static void SetupApi() =>
        DI.Setup()
            .Root<IClassCommenter>("Commenter");
}
```

To run the above code, the following NuGet packages must be added:
 - [Pure.DI](https://www.nuget.org/packages/Pure.DI)
 - [Shouldly](https://www.nuget.org/packages/Shouldly)

>[!NOTE]
>Splitting composition setup across multiple partial classes can improve organization for large compositions but may reduce readability if overused.

## Inheritance of compositions

Common bindings can be shared between compositions through plain C# inheritance. Define them in a base class whose setup uses `DI.Setup(kind: Internal)` — the `Internal` composition kind marks the setup as reusable configuration that does not generate a composition class of its own.
A composition class that derives from it automatically picks up the inherited bindings and combines them with its own.

```c#
using Shouldly;
using Pure.DI;
using static Pure.DI.CompositionKind;

var composition = new Composition();
var app = composition.App;

// The base composition provides common infrastructure,
// such as database access, that can be shared across different parts of the application.
class Infrastructure
{
    // The 'Internal' kind indicates that this setup is intended
    // to be inherited and does not produce a public API on its own.
    private static void Setup() =>
        DI.Setup(kind: Internal)
            .Bind<IDatabase>().To<SqlDatabase>();
}

// The main composition inherits the infrastructure configuration
// and defines the application-specific dependencies.
partial class Composition : Infrastructure
{
    private void Setup() =>
        DI.Setup()
            .Bind<IUserManager>().To<UserManager>()
            .Root<App>(nameof(App));
}

interface IDatabase;

class SqlDatabase : IDatabase;

interface IUserManager;

class UserManager(IDatabase database) : IUserManager;

partial class App(IUserManager userManager)
{
    public IUserManager UserManager { get; } = userManager;
}
```

To run the above code, the following NuGet packages must be added:
 - [Pure.DI](https://www.nuget.org/packages/Pure.DI)
 - [Shouldly](https://www.nuget.org/packages/Shouldly)

>[!NOTE]
>Composition inheritance provides a way to share common bindings while still allowing each derived composition to add its own specific bindings.

## Dependent compositions

The `Setup` method has an additional argument `kind`, which defines the type of composition:
- `CompositionKind.Public` - will create a normal composition class, this is the default setting and can be omitted, it can also use the `DependsOn` method to use it as a dependency in other compositions
- `CompositionKind.Internal` - the composition class will not be created, but that composition can be used to create other compositions by calling the `DependsOn` method with its name
- `CompositionKind.Global` - the composition class will also not be created, but that composition will automatically be used to create other compositions
Use dependent compositions to split large object graphs into reusable setup layers.

```c#
using Pure.DI;
using static Pure.DI.CompositionKind;

// This setup does not generate code, but can be used as a dependency
// and requires the use of the "DependsOn" call to add it as a dependency
DI.Setup("Infrastructure", Internal)
    .Bind<IDatabase>().To<SqlDatabase>();

// This setup generates code and can also be used as a dependency
DI.Setup(nameof(Composition))
    // Uses "Infrastructure" setup
    .DependsOn("Infrastructure")
    .Bind<IUserService>().To<UserService>()
    .Root<IUserService>("UserService");

// As in the previous case, this setup generates code and can also be used as a dependency
DI.Setup(nameof(OtherComposition))
    // Uses "Composition" setup
    .DependsOn(nameof(Composition))
    .Root<Ui>("Ui");

var composition = new Composition();
var userService = composition.UserService;

var otherComposition = new OtherComposition();
userService = otherComposition.Ui.UserService;

interface IDatabase;

class SqlDatabase : IDatabase;

interface IUserService;

class UserService(IDatabase database) : IUserService;

partial class Ui(IUserService userService)
{
    public IUserService UserService { get; } = userService;
}
```

To run the above code, the following NuGet package must be added:
 - [Pure.DI](https://www.nuget.org/packages/Pure.DI)

Limitations: too many setup layers can make graph ownership unclear; keep boundaries explicit and naming consistent.

## Dependent compositions with setup context

This scenario shows how to pass an explicit setup context when a dependent setup uses instance members.
When this occurs: you need base setup state (e.g., Unity-initialized fields) inside a dependent composition.
What it solves: avoids missing instance members in dependent compositions and keeps state access explicit.
How it is solved in the example: uses DependsOn(setupName, kind, name) and passes the base setup instance into the dependent composition.

```c#
using Pure.DI;
using static Pure.DI.CompositionKind;

var baseContext = new BaseComposition { Settings = new AppSettings("prod", 3) };
var composition = new Composition(baseContext);
var service = composition.Service;

interface IService
{
    string Report { get; }
}

class Service(IAppSettings settings) : IService
{
    public string Report { get; } = $"env={settings.Environment}, retries={settings.RetryCount}";
}

internal partial class BaseComposition
{
    internal AppSettings Settings { get; set; } = new("", 0);

    private void Setup()
    {
        DI.Setup(nameof(BaseComposition), Internal)
            .Bind<IAppSettings>().To(_ => Settings);
    }
}

internal partial class Composition
{
    private void Setup()
    {
        DI.Setup(nameof(Composition))
            .DependsOn(nameof(BaseComposition), SetupContextKind.Argument, "baseContext")
            .Bind<IService>().To<Service>()
            .Root<IService>("Service");
    }
}

record AppSettings(string Environment, int RetryCount) : IAppSettings;

interface IAppSettings
{
    string Environment { get; }

    int RetryCount { get; }
}
```

To run the above code, the following NuGet package must be added:
 - [Pure.DI](https://www.nuget.org/packages/Pure.DI)

What it shows:
- Explicit setup context injection for dependent compositions.

Important points:
- The dependent composition receives the base setup instance via a constructor argument.

Useful when:
- Base setup has instance members initialized externally (e.g., Unity).


## Dependent compositions with setup context members

This scenario shows how to copy referenced members from a base setup into the dependent composition.
When this occurs: you want to reuse base setup state without passing a separate context instance.
What it solves: lets dependent compositions access base setup members directly (Unity-friendly, no constructor args).
How it is solved in the example: uses DependsOn(..., SetupContextKind.Members) and sets members on the composition instance. The name parameter is optional; methods are declared partial and implemented by the user.

```c#
using Pure.DI;
using static Pure.DI.CompositionKind;

var composition = new Composition
{
    ConnectionSettings = new DatabaseConnectionSettings("prod-db.example.com", 5432, "app_database"),
    LogLevel = "Info",
    MaxRetries = 5
};

var service = composition.DataService;

interface IDataService
{
    string GetStatus();
}

/// <summary>
/// Data service using connection settings and logging configuration
/// </summary>
class DataService(
    IDatabaseConnectionSettings connectionSettings,
    [Tag("logLevel")] string logLevel,
    [Tag("maxRetries")] int maxRetries,
    [Tag("timeout")] int timeoutMs,
    [Tag("enableDiagnostics")] bool enableDiagnostics) : IDataService
{
    public string GetStatus() => enableDiagnostics
        ? $"Database: {connectionSettings.DatabaseName}@{connectionSettings.Host}:{connectionSettings.Port}, " +
          $"LogLevel: {logLevel}, " +
          $"MaxRetries: {maxRetries}, " +
          $"Timeout: {timeoutMs}ms"
        : "OK";
}

/// <summary>
/// Base composition providing database connection settings, default timeout, and diagnostics flag
/// </summary>
internal partial class BaseComposition
{
    /// <summary>
    /// Enable detailed diagnostics logging (protected field accessible in derived compositions via DependsOn)
    /// </summary>
    protected bool EnableDiagnostics = false;

    public DatabaseConnectionSettings ConnectionSettings { get; set; } = new("", 0, "");

    private int GetDefaultTimeout() => 5000;

    private void Setup()
    {
        DI.Setup(nameof(BaseComposition), Internal)
            .Bind<IDatabaseConnectionSettings>().To(_ => ConnectionSettings)
            .Bind("enableDiagnostics").To(_ => EnableDiagnostics)
            .Bind("timeout").To(_ => GetDefaultTimeout());
    }
}

/// <summary>
/// Dependent composition extending the base with logging level and max retries, and enabling diagnostics
/// </summary>
internal partial class Composition
{
    /// <summary>
    /// Constructor enables diagnostics by default
    /// </summary>
    public Composition() => EnableDiagnostics = true;

    /// <summary>
    /// Application logging level
    /// </summary>
    public string LogLevel { get; set; } = "Warning";

    /// <summary>
    /// Maximum number of retry attempts
    /// </summary>
    public int MaxRetries { get; set; } = 3;

    private void Setup()
    {
        DI.Setup(nameof(Composition))
            .DependsOn(nameof(BaseComposition), SetupContextKind.Members)
            .Bind<string>("logLevel").To(_ => LogLevel)
            .Bind<int>("maxRetries").To(_ => MaxRetries)
            .Bind<IDataService>().To<DataService>()
            .Root<IDataService>("DataService");
    }

    /// <summary>
    /// Implementation of partial method from base composition
    /// </summary>
    private partial int GetDefaultTimeout() => 5000;
}

/// <summary>
/// Database connection settings
/// </summary>
record DatabaseConnectionSettings(string Host, int Port, string DatabaseName) : IDatabaseConnectionSettings;

/// <summary>
/// Database connection settings interface
/// </summary>
interface IDatabaseConnectionSettings
{
    string Host { get; }

    int Port { get; }

    string DatabaseName { get; }
}
```

To run the above code, the following NuGet package must be added:
 - [Pure.DI](https://www.nuget.org/packages/Pure.DI)

What it shows:
- Setup context members copied into the dependent composition.
- Realistic scenario: configuring database connection and logging for a data service.

Important points:
- The composition remains parameterless and can be configured via its own members.

Example demonstrates:
  1. BaseComposition provides database connection settings, default timeout, and a protected diagnostics field
  2. Dependent Composition adds logging level and max retries configuration
  3. DataService uses all settings including the protected field for conditional output
  4. Protected field 'EnableDiagnostics' from BaseComposition is accessible in Composition via DependsOn
  5. Composition's constructor sets EnableDiagnostics to true to enable detailed status

Note: SetupContextKind.Members copies both public and protected members to the dependent composition.

Useful when:
- Base setup has instance members initialized by the host or framework.
- You need to extend configuration with additional settings in derived compositions.


## Dependent compositions with setup context members and property accessors

This scenario shows how to copy referenced members and implement custom property accessors via partial methods.
When this occurs: you need base setup properties with logic, but the dependent composition must remain parameterless.
What it solves: keeps Unity-friendly composition while letting the user implement property logic.
How it is solved in the example: uses DependsOn(..., SetupContextKind.Members) and implements partial get_ methods.

```c#
using Pure.DI;
using static Pure.DI.CompositionKind;

var composition = new Composition
{
	ConnectionString = "Server=prod-db.example.com;Database=AppDb;"
};

var service = composition.DatabaseService;

interface IDatabaseService
{
	string ConnectionString { get; }
	int MaxConnections { get; }
}

class DatabaseService(
	[Tag("connectionString")] string connectionString,
	[Tag("maxConnections")] int maxConnections) : IDatabaseService
{
	public string ConnectionString { get; } = connectionString;
	public int MaxConnections { get; } = maxConnections;
}

/// <summary>
/// Base composition providing database configuration properties
/// </summary>
internal partial class BaseComposition
{
	/// <summary>
	/// Connection string - simple property with field-backed accessor (no custom logic)
	/// </summary>
	public string ConnectionString { get; set; } = "";

	/// <summary>
	/// Maximum number of connections - property with custom getter logic
	/// </summary>
	private int _maxConnections = 100;

	public int MaxConnections
	{
		get => _maxConnections;
		set => _maxConnections = value;
	}

	private void Setup()
	{
		DI.Setup(nameof(BaseComposition), Internal)
			.Bind<string>("connectionString").To(_ => ConnectionString)
			.Bind<int>("maxConnections").To(_ => MaxConnections);
	}
}

/// <summary>
/// Dependent composition implementing custom accessor logic for properties
/// </summary>
internal partial class Composition
{
	/// <summary>
	/// MaxConnections backing field
	/// </summary>
	private int _maxConnections = 100;

	/// <summary>
	/// Custom accessor logic: returns configured value + 1 to ensure minimum buffer
	/// </summary>
	private partial int get__MaxConnections() => _maxConnections + 1;

	/// <summary>
	/// Setter for MaxConnections
	/// </summary>
	public void SetMaxConnections(int value) => _maxConnections = value;

	private void Setup()
	{
		DI.Setup(nameof(Composition))
			.DependsOn(nameof(BaseComposition), SetupContextKind.Members)
			.Bind<IDatabaseService>().To<DatabaseService>()
			.Root<IDatabaseService>("DatabaseService");
	}
}
```

To run the above code, the following NuGet package must be added:
 - [Pure.DI](https://www.nuget.org/packages/Pure.DI)

What it shows:
- Custom property logic via partial accessor methods.
- Properties with simple field-backed accessors (no logic).

Important points:
- Accessor logic is not copied; the user provides partial implementations.
- Simple property accessors (field-backed) can be used without partial methods.

Example demonstrates:
 1. BaseComposition provides connection string and max connections properties
 2. ConnectionString has simple field-backed accessor (no logic)
 3. MaxConnections has custom getter logic via partial method
 4. Dependent Composition implements custom accessor logic for MaxConnections

Useful when:
- Properties include custom logic and are referenced by bindings in a dependent setup.
- Some properties are simple field-backed while others have custom logic.


## Dependent compositions with setup context root argument

This scenario shows how to pass an explicit setup context as a root argument.
When this occurs: you need external state from the base setup but cannot use a constructor (e.g., Unity MonoBehaviour).
What it solves: keeps the dependent composition safe while avoiding constructor arguments.
How it is solved in the example: uses DependsOn(..., SetupContextKind.RootArgument, name) and passes the base setup instance to the root method.

```c#
using Pure.DI;
using static Pure.DI.CompositionKind;

var baseContext = new BaseComposition { Settings = new AppSettings("staging", 2) };
var composition = new Composition();
var service = composition.Service(baseContext: baseContext);

interface IService
{
    string Report { get; }
}

class Service(IAppSettings settings) : IService
{
    public string Report { get; } = $"env={settings.Environment}, retries={settings.RetryCount}";
}

internal partial class BaseComposition
{
    internal AppSettings Settings { get; set; } = new("", 0);

    private void Setup()
    {
        DI.Setup(nameof(BaseComposition), Internal)
            .Bind<IAppSettings>().To(_ => Settings);
    }
}

internal partial class Composition
{
    private void Setup()
    {
        // Resolve = Off
        DI.Setup(nameof(Composition))
            .DependsOn(nameof(BaseComposition), SetupContextKind.RootArgument, "baseContext")
            .Bind<IService>().To<Service>()
            .Root<IService>("Service");
    }
}

record AppSettings(string Environment, int RetryCount) : IAppSettings;

interface IAppSettings
{
    string Environment { get; }

    int RetryCount { get; }
}
```

To run the above code, the following NuGet package must be added:
 - [Pure.DI](https://www.nuget.org/packages/Pure.DI)

What it shows:
- Passing setup context into a root method.

Important points:
- The composition itself can still be created with a parameterless constructor.

Useful when:
- The host (like Unity) creates the composition instance.

## Global compositions

When the `Setup(name, kind)` method is called, the second optional parameter specifies the composition kind. If you set it as `CompositionKind.Global`, no composition class will be created, but this setup will be the base setup for all others in the current project, and `DependsOn(...)` is not required. The setups will be applied in the sort order of their names.

```c#
using Shouldly;
using Pure.DI;
using static Pure.DI.CompositionKind;

return;

class MyGlobalComposition
{
    static void Setup() =>
        DI.Setup(kind: Global)
            .Hint(Hint.ToString, "Off")
            .Hint(Hint.FormatCode, "On");
}

class MyGlobalComposition2
{
    static void Setup() =>
        DI.Setup("Some name", kind: Global)
            .Hint(Hint.ToString, "On");
}
```

To run the above code, the following NuGet packages must be added:
 - [Pure.DI](https://www.nuget.org/packages/Pure.DI)
 - [Shouldly](https://www.nuget.org/packages/Shouldly)

>[!IMPORTANT]
>Global compositions apply to all other compositions in the project automatically, so use them carefully to avoid unintended side effects.

## Tag Type

`Tag.Type` in bindings replaces the expression `typeof(T)`, where `T` is the type of the implementation in a binding.

```c#
using Shouldly;
using Pure.DI;

DI.Setup(nameof(Composition))
    // Tag.Type here is the same as typeof(CreditCardGateway)
    // The `default` tag is used to resolve dependencies
    // when the tag was not specified by the consumer
    .Bind<IPaymentGateway>(Tag.Type, default).To<CreditCardGateway>()
    // Tag.Type here is the same as typeof(PayPalGateway)
    .Bind<IPaymentGateway>(Tag.Type).As(Lifetime.Singleton).To<PayPalGateway>()
    .Bind<IPaymentProcessor>().To<PaymentProcessor>()

    // Composition root
    .Root<IPaymentProcessor>("PaymentSystem")

    // "PayPalRoot" is root name, typeof(PayPalGateway) is tag
    .Root<IPaymentGateway>("PayPalRoot", typeof(PayPalGateway));

var composition = new Composition();
var service = composition.PaymentSystem;
service.PrimaryGateway.ShouldBeOfType<CreditCardGateway>();
service.AlternativeGateway.ShouldBeOfType<PayPalGateway>();
service.AlternativeGateway.ShouldBe(composition.PayPalRoot);
service.DefaultGateway.ShouldBeOfType<CreditCardGateway>();

interface IPaymentGateway;

class CreditCardGateway : IPaymentGateway;

class PayPalGateway : IPaymentGateway;

interface IPaymentProcessor
{
    IPaymentGateway PrimaryGateway { get; }

    IPaymentGateway AlternativeGateway { get; }

    IPaymentGateway DefaultGateway { get; }
}

class PaymentProcessor(
    [Tag(typeof(CreditCardGateway))] IPaymentGateway primaryGateway,
    [Tag(typeof(PayPalGateway))] IPaymentGateway alternativeGateway,
    IPaymentGateway defaultGateway)
    : IPaymentProcessor
{
    public IPaymentGateway PrimaryGateway { get; } = primaryGateway;

    public IPaymentGateway AlternativeGateway { get; } = alternativeGateway;

    public IPaymentGateway DefaultGateway { get; } = defaultGateway;
}
```

To run the above code, the following NuGet packages must be added:
 - [Pure.DI](https://www.nuget.org/packages/Pure.DI)
 - [Shouldly](https://www.nuget.org/packages/Shouldly)

>[!NOTE]
>`Tag.Type` provides a convenient way to reference implementation types in tags without explicitly using `typeof()`.

## Tag Any

`Tag.Any` creates a binding that matches any tag value, including default (null), allowing flexible injection scenarios where the tag value can be used within factory contexts. This is useful when you need to dynamically handle different tag values in a single binding.

```c#
using Shouldly;
using Pure.DI;

DI.Setup(nameof(Composition))
    // Binds IQueue to the Queue implementation.
    // Tag.Any creates a binding that matches any tag (including null),
    // allowing the specific tag value to be used within the factory (ctx.Tag).
    .Bind<IQueue>(Tag.Any).To(ctx => new Queue(ctx.Tag))
    .Bind<IQueueService>().To<QueueService>()

    // Composition root
    .Root<IQueueService>("QueueService")

    // Root by Tag.Any: Resolves IQueue with the tag "Audit"
    .Root<IQueue>("AuditQueue", "Audit");

var composition = new Composition();
var queueService = composition.QueueService;

queueService.WorkItemsQueue.Id.ShouldBe("WorkItems");
queueService.PartitionQueue.Id.ShouldBe(42);
queueService.DefaultQueue.Id.ShouldBeNull();
composition.AuditQueue.Id.ShouldBe("Audit");

interface IQueue
{
    object? Id { get; }
}

record Queue(object? Id) : IQueue;

interface IQueueService
{
    IQueue WorkItemsQueue { get; }

    IQueue PartitionQueue { get; }

    IQueue DefaultQueue { get; }
}

class QueueService(
    // Injects IQueue tagged with "WorkItems"
    [Tag("WorkItems")] IQueue workItemsQueue,
    // Injects IQueue tagged with integer 42
    [Tag(42)] Func<IQueue> partitionQueueFactory,
    // Injects IQueue with the default (null) tag
    IQueue defaultQueue)
    : IQueueService
{
    public IQueue WorkItemsQueue { get; } = workItemsQueue;

    public IQueue PartitionQueue { get; } = partitionQueueFactory();

    public IQueue DefaultQueue { get; } = defaultQueue;
}
```

To run the above code, the following NuGet packages must be added:
 - [Pure.DI](https://www.nuget.org/packages/Pure.DI)
 - [Shouldly](https://www.nuget.org/packages/Shouldly)

>[!IMPORTANT]
>`Tag.Any` provides maximum flexibility but requires careful handling within factories to properly interpret and use the tag value.

## Tag Unique

`Tag.Unique` is useful to register a binding with a unique tag. It will not be available through the composition root or `Resolve` methods directly, but can be injected in compositions as some kind of enumeration.
Use this to aggregate multiple implementations without exposing each one as a direct root.

```c#
using Shouldly;
using Pure.DI;
using System.Collections.Immutable;

DI.Setup(nameof(Composition))
    .Bind<INotificationChannel<TT>>(Tag.Unique).To<EmailChannel<TT>>()
    .Bind<INotificationChannel<TT>>(Tag.Unique).To<SmsChannel<TT>>()
    .Bind<INotificationService<TT>>().To<NotificationService<TT>>()

    // Composition root
    .Root<INotificationService<string>>("NotificationService");

var composition = new Composition();
var notificationService = composition.NotificationService;
notificationService.Channels.Length.ShouldBe(2);

interface INotificationChannel<T>;

class EmailChannel<T> : INotificationChannel<T>;

class SmsChannel<T> : INotificationChannel<T>;

interface INotificationService<T>
{
    ImmutableArray<INotificationChannel<T>> Channels { get; }
}

class NotificationService<T>(IEnumerable<INotificationChannel<T>> channels)
    : INotificationService<T>
{
    public ImmutableArray<INotificationChannel<T>> Channels { get; }
        = [..channels];
}
```

To run the above code, the following NuGet packages must be added:
 - [Pure.DI](https://www.nuget.org/packages/Pure.DI)
 - [Shouldly](https://www.nuget.org/packages/Shouldly)

Limitations: unique-tag bindings are intentionally hidden from direct resolve; document this to avoid confusion in integration code.
See also: [Tags](tags.md), [Enumerable](enumerable.md).

## Tag on injection site

Sometimes you need to determine which binding will be used to inject explicitly. To do this, use a special tag created by calling the `Tag.On()` method. Tag on injection site is specified in a special format: `Tag.On("<namespace>.<type>.<member>[:argument]")`. The argument is specified only for the constructor and methods. For example, for namespace _MyNamespace_ and type _Class1_:

- `Tag.On("MyNamespace.Class1.Class1:state1") - the tag corresponds to the constructor argument named _state_ of type _MyNamespace.Class1_
- `Tag.On("MyNamespace.Class1.DoSomething:myArg")` - the tag corresponds to the _myArg_ argument of the _DoSomething_ method
- `Tag.On("MyNamespace.Class1.MyData")` - the tag corresponds to property or field _MyData_

The wildcards `*` and `?` are supported. All names are case-sensitive. The global namespace prefix `global::` must be omitted. You can also combine multiple tags in a single `Tag.On("...", "...")` call.

For generic types, the type name also contains the number of type parameters, e.g., for the `myDep` constructor argument of the `Consumer<T>` class, the tag on the injection site would be ``MyNamespace.Consumer`1.Consumer:myDep``:

```c#
using Shouldly;
using Pure.DI;

DI.Setup(nameof(Composition))
    .Bind(
        Tag.On("*UserService.UserService:localRepo"),
        // Tag on injection site for generic type
        Tag.On("*UserFetcher`1.UserFetcher:repo"))
        .To<SqlUserRepository>()
    .Bind(
        // Combined tag
        Tag.On(
            "*UserService.UserService:cloudRepo",
            "*UserService:BackupRepository"))
        .To<ApiUserRepository>()
    .Bind<IUserService>().To<UserService>()

    // Specifies to create the composition root named "Root"
    .Root<IUserService>("Ui");

var composition = new Composition();
var userService = composition.Ui;
userService.LocalRepository.ShouldBeOfType<SqlUserRepository>();
userService.CloudRepository.ShouldBeOfType<ApiUserRepository>();
userService.BackupRepository.ShouldBeOfType<ApiUserRepository>();
userService.FetcherRepository.ShouldBeOfType<SqlUserRepository>();

interface IUserRepository;

class SqlUserRepository : IUserRepository;

class ApiUserRepository : IUserRepository;

class UserFetcher<T>(IUserRepository repo)
{
    public IUserRepository Repository { get; } = repo;
}

interface IUserService
{
    IUserRepository LocalRepository { get; }

    IUserRepository CloudRepository { get; }

    IUserRepository BackupRepository { get; }

    IUserRepository FetcherRepository { get; }
}

class UserService(
    IUserRepository localRepo,
    IUserRepository cloudRepo,
    UserFetcher<string> fetcher)
    : IUserService
{
    public IUserRepository LocalRepository { get; } = localRepo;

    public IUserRepository CloudRepository { get; } = cloudRepo;

    public required IUserRepository BackupRepository { get; init; }

    public IUserRepository FetcherRepository => fetcher.Repository;
}
```

To run the above code, the following NuGet packages must be added:
 - [Pure.DI](https://www.nuget.org/packages/Pure.DI)
 - [Shouldly](https://www.nuget.org/packages/Shouldly)

>[!WARNING]
>Each potentially injectable argument, property, or field contains an additional tag. This tag can be used to specify what can be injected there. This will only work if the binding type and the tag match. So while this approach can be useful for specifying what to enter, it can be more expensive to maintain and less reliable, so it is recommended to use attributes like `[Tag(...)]` instead.

## Tag on injection site with wildcards

`Tag.On("...")` accepts injection-site paths of the form `Namespace.Type:memberOrArgName`, and those paths may contain the wildcards `*` (any characters) and `?` (a single character). This lets one binding cover several injection sites at once — for example, `"*SmartHomeSystem:zone?"` matches both the `zone1` and `zone2` constructor parameters.

```c#
using Shouldly;
using Pure.DI;

DI.Setup(nameof(Composition))
    // We use wildcards to specify logic:
    // 1. Inject TemperatureSensor into 'OutdoorSensor' property of SmartHomeSystem
    // 2. Inject TemperatureSensor into 'sensor' argument of any ClimateControl
    .Bind(Tag.On("*SmartHomeSystem:OutdoorSensor", "*ClimateControl:sensor"))
    .To<TemperatureSensor>()

    // Inject MotionSensor into any argument starting with 'zone' inside SmartHomeSystem
    // This corresponds to 'zone1' and 'zone2'
    .Bind(Tag.On("*SmartHomeSystem:zone?"))
    .To<MotionSensor>()
    .Bind<ISmartHomeSystem>().To<SmartHomeSystem>()

    // Specifies to create the composition root named "Root"
    .Root<ISmartHomeSystem>("SmartHome");

var composition = new Composition();
var smartHome = composition.SmartHome;

// Verification:
// Zone sensors should be MotionSensors (matched by "*SmartHomeSystem:zone?")
smartHome.Zone1.ShouldBeOfType<MotionSensor>();
smartHome.Zone2.ShouldBeOfType<MotionSensor>();

// Outdoor sensor should be TemperatureSensor (matched by "*SmartHomeSystem:OutdoorSensor")
smartHome.OutdoorSensor.ShouldBeOfType<TemperatureSensor>();

// Climate control sensor should be TemperatureSensor (matched by "*ClimateControl:sensor")
smartHome.ClimateSensor.ShouldBeOfType<TemperatureSensor>();

interface ISensor;

class TemperatureSensor : ISensor;

class MotionSensor : ISensor;

class ClimateControl<T>(ISensor sensor)
{
    public ISensor Sensor { get; } = sensor;
}

interface ISmartHomeSystem
{
    ISensor Zone1 { get; }

    ISensor Zone2 { get; }

    ISensor OutdoorSensor { get; }

    ISensor ClimateSensor { get; }
}

class SmartHomeSystem(
    ISensor zone1,
    ISensor zone2,
    ClimateControl<string> climateControl)
    : ISmartHomeSystem
{
    public ISensor Zone1 { get; } = zone1;

    public ISensor Zone2 { get; } = zone2;

    public required ISensor OutdoorSensor { get; init; }

    public ISensor ClimateSensor => climateControl.Sensor;
}
```

To run the above code, the following NuGet packages must be added:
 - [Pure.DI](https://www.nuget.org/packages/Pure.DI)
 - [Shouldly](https://www.nuget.org/packages/Shouldly)

>[!WARNING]
>Each potentially injectable argument, property, or field contains an additional tag. This tag can be used to specify what can be injected there. This will only work if the binding type and the tag match. So while this approach can be useful for specifying what to enter, it can be more expensive to maintain and less reliable, so it is recommended to use attributes like `[Tag(...)]` instead.

## Tag on a constructor argument

`Tag.OnConstructorArg<T>(name)` creates a tag that targets the constructor parameter `name` of type `T`, letting you choose a specific implementation for a single injection site without adding `[Tag(...)]` attributes to the consuming class — useful when you can't or don't want to modify its code. The wildcards `*` and `?` are supported.

```c#
using Shouldly;
using Pure.DI;

DI.Setup(nameof(Composition))
    .Bind(Tag.OnConstructorArg<DataReplicator>("sourceStream"))
        .To<FileStream>()
    .Bind(Tag.OnConstructorArg<StreamProcessor<TT>>("stream"))
        .To<NetworkStream>()
    .Bind<IDataReplicator>().To<DataReplicator>()

    // Specifies to create the composition root named "Root"
    .Root<IDataReplicator>("Replicator");

var composition = new Composition();
var replicator = composition.Replicator;
replicator.SourceStream.ShouldBeOfType<FileStream>();
replicator.TargetStream.ShouldBeOfType<NetworkStream>();

interface IStream;

class FileStream : IStream;

class NetworkStream : IStream;

class StreamProcessor<T>(IStream stream)
{
    public IStream Stream { get; } = stream;
}

interface IDataReplicator
{
    IStream SourceStream { get; }

    IStream TargetStream { get; }
}

class DataReplicator(
    IStream sourceStream,
    StreamProcessor<string> processor)
    : IDataReplicator
{
    public IStream SourceStream { get; } = sourceStream;

    public IStream TargetStream => processor.Stream;
}
```

To run the above code, the following NuGet packages must be added:
 - [Pure.DI](https://www.nuget.org/packages/Pure.DI)
 - [Shouldly](https://www.nuget.org/packages/Shouldly)

>[!WARNING]
>Each potentially injectable argument, property, or field contains an additional tag. This tag can be used to specify what can be injected there. This will only work if the binding type and the tag match. So while this approach can be useful for specifying what to enter, it can be more expensive to maintain and less reliable, so it is recommended to use attributes like `[Tag(...)]` instead.

## Tag on a member

`Tag.OnMember<T>(memberName)` creates a tag that targets injection into a specific property or field of type `T`, so you can override which implementation goes into that member without touching the class definition — here `StripeGateway` is injected into the `Gateway` property of `CheckoutService` while `PayPalGateway` stays the default binding elsewhere. The wildcards `*` and `?` are supported.

```c#
using Shouldly;
using Pure.DI;

DI.Setup(nameof(Composition))
    .Bind().To<PayPalGateway>()
    // Binds StripeGateway to the "Gateway" property of the "CheckoutService" class.
    // This lets you override the injected dependency for a specific member
    // without changing the class definition.
    .Bind(Tag.OnMember<CheckoutService>(nameof(CheckoutService.Gateway)))
    .To<StripeGateway>()
    .Bind<ICheckoutService>().To<CheckoutService>()

    // Specifies to create the composition root named "Root"
    .Root<ICheckoutService>("CheckoutService");

var composition = new Composition();
var checkoutService = composition.CheckoutService;

// Checks that the property was injected with the specific implementation
checkoutService.Gateway.ShouldBeOfType<StripeGateway>();

interface IPaymentGateway;

class PayPalGateway : IPaymentGateway;

class StripeGateway : IPaymentGateway;

interface ICheckoutService
{
    IPaymentGateway Gateway { get; }
}

class CheckoutService : ICheckoutService
{
    public required IPaymentGateway Gateway { get; init; }
}
```

To run the above code, the following NuGet packages must be added:
 - [Pure.DI](https://www.nuget.org/packages/Pure.DI)
 - [Shouldly](https://www.nuget.org/packages/Shouldly)

>[!WARNING]
>Each potentially injectable argument, property, or field contains an additional tag. This tag can be used to specify what can be injected there. This will only work if the binding type and the tag match. So while this approach can be useful for specifying what to enter, it can be more expensive to maintain and less reliable, so it is recommended to use attributes like `[Tag(...)]` instead.

## Tag on a method argument

`Tag.OnMethodArg<T>(methodName, argName)` creates a tag that targets a specific parameter of an injection method (one marked with the `[Dependency]` attribute) in type `T`, so a particular implementation can be supplied to just that argument without adding `[Tag(...)]` attributes to the consuming class. The wildcards `*` and `?` are supported.

```c#
using Shouldly;
using Pure.DI;

DI.Setup(nameof(Composition))
    .Bind().To<TemperatureSensor>()
    // Binds specifically to the argument "sensor" of the "Calibrate" method
    // in the "WeatherStation" class
    .Bind(Tag.OnMethodArg<WeatherStation>(nameof(WeatherStation.Calibrate), "sensor"))
    .To<HumiditySensor>()
    .Bind<IWeatherStation>().To<WeatherStation>()

    // Specifies to create the composition root named "Station"
    .Root<IWeatherStation>("Station");

var composition = new Composition();
var station = composition.Station;
station.Sensor.ShouldBeOfType<HumiditySensor>();

interface ISensor;

class TemperatureSensor : ISensor;

class HumiditySensor : ISensor;

interface IWeatherStation
{
    ISensor? Sensor { get; }
}

class WeatherStation : IWeatherStation
{
    // The [Dependency] attribute is used to mark the method for injection
    [Dependency]
    public void Calibrate(ISensor sensor) =>
        Sensor = sensor;

    public ISensor? Sensor { get; private set; }
}
```

To run the above code, the following NuGet packages must be added:
 - [Pure.DI](https://www.nuget.org/packages/Pure.DI)
 - [Shouldly](https://www.nuget.org/packages/Shouldly)

>[!WARNING]
>Each potentially injectable argument, property, or field contains an additional tag. This tag can be used to specify what can be injected there. This will only work if the binding type and the tag match. So while this approach can be useful for specifying what to enter, it can be more expensive to maintain and less reliable, so it is recommended to use attributes like `[Tag(...)]` instead.

## Factory with thread synchronization

In some cases, initialization of objects requires synchronization of the overall composition flow. This scenario demonstrates how to use factories with thread synchronization to ensure proper initialization order.

```c#
using Shouldly;
using Pure.DI;

DI.Setup(nameof(Composition))
    .Bind<IMessageBus>().To<IMessageBus>(ctx => {
        // Initialization logic requiring synchronization
        // of the overall composition flow.
        // For example, connecting to a message broker.
        lock (ctx.Lock)
        {
            ctx.Inject(out MessageBus bus);
            bus.Connect();
            return bus;
        }
    })
    .Bind<INotificationService>().To<NotificationService>()

    // Composition root
    .Root<INotificationService>("NotificationService");

var composition = new Composition();
var service = composition.NotificationService;
service.Bus.IsConnected.ShouldBeTrue();

interface IMessageBus
{
    bool IsConnected { get; }
}

class MessageBus : IMessageBus
{
    public bool IsConnected { get; private set; }

    public void Connect() => IsConnected = true;
}

interface INotificationService
{
    IMessageBus Bus { get; }
}

class NotificationService(IMessageBus bus) : INotificationService
{
    public IMessageBus Bus { get; } = bus;
}
```

To run the above code, the following NuGet packages must be added:
 - [Pure.DI](https://www.nuget.org/packages/Pure.DI)
 - [Shouldly](https://www.nuget.org/packages/Shouldly)

>[!NOTE]
>Thread synchronization in factories should be used carefully as it may impact performance. Only use when necessary for correct initialization behavior.

## Override depth

When this occurs: you need to control how far override values propagate in a factory.
What it solves: keeps overrides local to a single injection level without affecting nested dependencies.
How it is solved in the example: uses Let to keep overrides local and verifies the scope.

```c#
using Shouldly;
using Pure.DI;

DI.Setup(nameof(DeepComposition))
    .Bind().To(_ => 7)
    .Bind().To<Dependency>()
    .Bind().To<Service>(ctx =>
    {
        ctx.Override(42);
        ctx.Inject(out Service service);
        return service;
    })
    .Root<Service>("Service");

DI.Setup(nameof(ShallowComposition))
    .Bind().To(_ => 7)
    .Bind().To<Dependency>()
    .Bind().To<Service>(ctx =>
    {
        ctx.Let(42);
        ctx.Inject(out Service service);
        return service;
    })
    .Root<Service>("Service");

var deep = new DeepComposition().Service;
var shallow = new ShallowComposition().Service;

deep.Id.ShouldBe(42);
deep.Dependency.Id.ShouldBe(42);

shallow.Id.ShouldBe(42);
shallow.Dependency.Id.ShouldBe(7);

class Dependency(int id)
{
    public int Id { get; } = id;
}

class Service(int id, Dependency dependency)
{
    public int Id { get; } = id;

    public Dependency Dependency { get; } = dependency;
}
```

To run the above code, the following NuGet packages must be added:
 - [Pure.DI](https://www.nuget.org/packages/Pure.DI)
 - [Shouldly](https://www.nuget.org/packages/Shouldly)

What it shows:
- Demonstrates deep vs one-level override behavior.

Important points:
- Deep overrides propagate into nested dependency graphs.
- One-level overrides affect only the immediate injection.

Useful when:
- You want to override a constructor parameter without affecting deeper object graphs.

## Accumulators

Accumulators allow you to accumulate instances of certain types and lifetimes.
Use this when you need an aggregated view of created dependencies (for diagnostics, telemetry, or registries).

```c#
using Shouldly;
using Pure.DI;
using static Pure.DI.Lifetime;

DI.Setup(nameof(Composition))
    // Accumulates all instances implementing ITelemetrySource
    // into a collection of type TelemetryRegistry.
    // The accumulation applies to Transient and Singleton lifetimes.
    .Accumulate<ITelemetrySource, TelemetryRegistry>(Transient, Singleton)

    // Infrastructure bindings
    .Bind<IDataSource>().As(PerBlock).To<SqlDataSource>()
    .Bind<IDataSource>(Tag.Type).To<SqlDataSource>()
    .Bind<IDataSource>(Tag.Type).As(Singleton).To<NetworkDataSource>()
    .Bind<IDashboard>().To<Dashboard>()

    // Composition root
    .Root<(IDashboard dashboard, TelemetryRegistry registry)>("Root");

var composition = new Composition();
var (dashboard, registry) = composition.Root;

// Checks that all telemetry sources have been collected
registry.Count.ShouldBe(3);
// The order of accumulation depends on the order of object creation in the graph
registry[0].ShouldBeOfType<NetworkDataSource>();
registry[1].ShouldBeOfType<SqlDataSource>();
registry[2].ShouldBeOfType<Dashboard>();

// Represents a component that produces telemetry data
interface ITelemetrySource;

// Accumulator for collecting all telemetry sources in the object graph
class TelemetryRegistry : List<ITelemetrySource>;

// Abstract data source interface
interface IDataSource;

// SQL database implementation acting as a telemetry source
class SqlDataSource : IDataSource, ITelemetrySource;

// Network data source implementation acting as a telemetry source
class NetworkDataSource : IDataSource, ITelemetrySource;

// Dashboard interface
interface IDashboard;

// Dashboard implementation aggregating data from sources
class Dashboard(
    [Tag(typeof(SqlDataSource))] IDataSource primaryDb,
    [Tag(typeof(NetworkDataSource))] IDataSource externalApi,
    IDataSource fallbackDb)
    : IDashboard, ITelemetrySource;
```

To run the above code, the following NuGet packages must be added:
 - [Pure.DI](https://www.nuget.org/packages/Pure.DI)
 - [Shouldly](https://www.nuget.org/packages/Shouldly)

Limitations: accumulation order depends on object creation order in the graph, so do not treat it as a stable business ordering.
See also: [Enumerable](enumerable.md), [Lifetimes](transient.md).

## Root Name

`RootName` provides the name of the composition root being resolved. This property is useful for logging, diagnostics, or implementing root-specific behavior.
Use this when infrastructure behavior should include root-level context (for example, logging prefixes).

```c#
using Shouldly;
using Pure.DI;

var composition = new Composition();
var orderService = composition.OrderService;
orderService.Logger.Log("Processing order").ShouldContain("OrderService");

var paymentService = composition.PaymentService;
paymentService.Logger.Log("Processing payment").ShouldContain("PaymentService");

interface ILogger
{
    string Log(string message);
}

interface IOrderService
{
    ILogger Logger { get; }
}

interface IPaymentService
{
    ILogger Logger { get; }
}

class Logger(string rootName) : ILogger
{
    public string Log(string message) => $"[{rootName}] {message}";
}

class OrderService(ILogger logger) : IOrderService
{
    public ILogger Logger => logger;
}

class PaymentService(ILogger logger) : IPaymentService
{
    public ILogger Logger => logger;
}

partial class Composition
{
    private void Setup() =>

        DI.Setup(nameof(Composition))
            .Bind().To(ctx => new Logger(ctx.RootName))
            .Bind().To<OrderService>()
            .Root<IOrderService>("OrderService")
            .Bind().To<PaymentService>()
            .Root<IPaymentService>("PaymentService");
}
```

To run the above code, the following NuGet packages must be added:
 - [Pure.DI](https://www.nuget.org/packages/Pure.DI)
 - [Shouldly](https://www.nuget.org/packages/Shouldly)

Limitations: root-name-dependent behavior couples logic to API naming; avoid it in domain services.
See also: [Composition roots](composition-roots.md), [Root Type](root-type.md).

## Root Type

`RootType` provides the type of the composition root being resolved. This property is useful for implementing root-specific behavior like different caching strategies per root type.
Use this when infrastructure dependencies must vary by root contract type.

```c#
using Shouldly;
using Pure.DI;

var composition = new Composition();
var orderService = composition.OrderService;
orderService.Cache.Set("order_123", "Order Data");
orderService.Cache.Get("order_123").ShouldBe("Order Data");
orderService.Cache.KeyPrefix.ShouldBe("IOrderService");

var inventoryService = composition.InventoryService;
inventoryService.Cache.Set("item_456", "Item Data");
inventoryService.Cache.Get("item_456").ShouldBe("Item Data");
inventoryService.Cache.KeyPrefix.ShouldBe("IInventoryService");

interface ICache
{
    string KeyPrefix { get; }

    void Set(string key, string value);

    string Get(string key);
}

interface IOrderService
{
    ICache Cache { get; }
}

interface IInventoryService
{
    ICache Cache { get; }
}

class Cache(Type rootType) : ICache
{
    private readonly Dictionary<string, string> _data = new();

    public string KeyPrefix => rootType.Name;

    public void Set(string key, string value) => _data[key] = value;

    public string Get(string key) => _data.TryGetValue(key, out var value) ? value : string.Empty;
}

class OrderService(ICache cache) : IOrderService
{
    public ICache Cache => cache;
}

class InventoryService(ICache cache) : IInventoryService
{
    public ICache Cache => cache;
}

partial class Composition
{
    private void Setup() =>

        DI.Setup(nameof(Composition))
            .Bind().To(ctx => new Cache(ctx.RootType))
            .Bind().To<OrderService>()
            .Root<IOrderService>("OrderService")
            .Bind().To<InventoryService>()
            .Root<IInventoryService>("InventoryService");
}
```

To run the above code, the following NuGet packages must be added:
 - [Pure.DI](https://www.nuget.org/packages/Pure.DI)
 - [Shouldly](https://www.nuget.org/packages/Shouldly)

Limitations: root-type-specific rules can become hidden policy; keep this logic centralized and observable.
See also: [Composition roots](composition-roots.md), [Root Name](root-name.md).

## Consumer types

`ConsumerTypes` is used to get the list of consumer types of a given dependency. It contains an array of types and guarantees that it will contain at least one element. The use of `ConsumerTypes` is demonstrated on the example of [Serilog library](https://serilog.net/):
Use this when one dependency must adapt behavior based on the concrete consuming type.

```c#
using Shouldly;
using Serilog.Core;
using Serilog.Events;
using Pure.DI;
using Serilog.Core;

Serilog.ILogger serilogLogger = new Serilog.LoggerConfiguration().CreateLogger();
var composition = new Composition(logger: serilogLogger);
var orderProcessing = composition.OrderProcessing;

interface IPaymentGateway;

class PaymentGateway : IPaymentGateway
{
    public PaymentGateway(Serilog.ILogger log)
    {
        log.Information("Payment gateway initialized");
    }
}

interface IOrderProcessing
{
    IPaymentGateway PaymentGateway { get; }
}

class OrderProcessing : IOrderProcessing
{
    public OrderProcessing(
        Serilog.ILogger log,
        IPaymentGateway paymentGateway)
    {
        PaymentGateway = paymentGateway;
        log.Information("Order processing initialized");
    }

    public IPaymentGateway PaymentGateway { get; }
}

partial class Composition
{
    private void Setup() =>

        DI.Setup(nameof(Composition))
            .Arg<Serilog.ILogger>("logger", "from arg")
            .Bind().To(ctx => {
                ctx.Inject<Serilog.ILogger>("from arg", out var logger);

                // Using ConsumerTypes to get the type of the consumer.
                // This allows us to create a logger with a context specific to the consuming class.
                return logger.ForContext(ctx.ConsumerTypes[0]);
            })
            .Bind().To<PaymentGateway>()
            .Bind().To<OrderProcessing>()
            .Root<IOrderProcessing>(nameof(OrderProcessing));
}
```

To run the above code, the following NuGet packages must be added:
 - [Pure.DI](https://www.nuget.org/packages/Pure.DI)
 - [Shouldly](https://www.nuget.org/packages/Shouldly)
 - [Serilog.Core](https://www.nuget.org/packages/Serilog.Core)
 - [Serilog.Events](https://www.nuget.org/packages/Serilog.Events)

Limitations: consumer-aware configuration increases coupling to composition details; use it for infrastructure concerns (logging, tracing), not core domain behavior.
See also: [Interception](interception.md), [Factory](factory.md).

## IsLockRequired

`IsLockRequired` indicates whether a lock is required for thread-safe operations in the current context. This property is useful when you need to conditionally synchronize based on thread safety requirements.
Use this when custom factory logic must respect thread-safety semantics of generated code.

```c#
using Shouldly;
using Pure.DI;

var composition = new Composition();

var service = composition.Service;
service.Locked.ShouldBeTrue();

var singletonService = composition.SingletonService;
singletonService.Locked.ShouldBeFalse();

interface IService
{
    bool Locked { get; }
}

class Service(bool lockRequired) : IService
{
    public bool Locked => lockRequired;
}

partial class Composition
{
    private void Setup() =>

        DI.Setup(nameof(Composition))
            .Hint(Hint.ThreadSafe, "On")
            .Bind().To(ctx =>
            {
                // In a thread-safe context, IsLockRequired is true
                // Use it to conditionally lock the context
                if (ctx.IsLockRequired)
                {
                    lock (ctx.Lock)
                    {
                        return new Service(ctx.IsLockRequired);
                    }
                }

                return new Service(ctx.IsLockRequired);
            })
            .Bind(Tag.Single).As(Lifetime.Singleton).To((IService service) => service)
            .Root<IService>(nameof(Service))
            .Root<IService>(nameof(SingletonService), Tag.Single);
}
```

To run the above code, the following NuGet packages must be added:
 - [Pure.DI](https://www.nuget.org/packages/Pure.DI)
 - [Shouldly](https://www.nuget.org/packages/Shouldly)

Limitations: avoid adding business logic inside lock-aware factories; use it only for synchronization concerns.
See also: [ThreadSafe hint](threadsafe-hint.md), [Factory](factory.md).

## Tracking disposable instances per a composition root

The special `Owned<T>` type lets you track and dispose of disposable instances per composition root rather than per composition. Declare a root as `Root<Owned<T>>`: each access returns an `Owned<T>` that owns every disposable created for that dependency graph, and calling its `Dispose()` cleans up exactly those instances without affecting other roots.

```c#
using Shouldly;
using Pure.DI;

var composition = new Composition();
var orderProcessingService1 = composition.OrderProcessingService;
var orderProcessingService2 = composition.OrderProcessingService;

orderProcessingService2.Dispose();

// Checks that the disposable instances
// associated with orderProcessingService2 have been disposed of
orderProcessingService2.Value.DbConnection.IsDisposed.ShouldBeTrue();

// Checks that the disposable instances
// associated with orderProcessingService1 have not been disposed of
orderProcessingService1.Value.DbConnection.IsDisposed.ShouldBeFalse();

orderProcessingService1.Dispose();

// Checks that the disposable instances
// associated with orderProcessingService1 have been disposed of
orderProcessingService1.Value.DbConnection.IsDisposed.ShouldBeTrue();

interface IDbConnection
{
    bool IsDisposed { get; }
}

class DbConnection : IDbConnection, IDisposable
{
    public bool IsDisposed { get; private set; }

    public void Dispose() => IsDisposed = true;
}

interface IOrderProcessingService
{
    public IDbConnection DbConnection { get; }
}

class OrderProcessingService(IDbConnection dbConnection) : IOrderProcessingService
{
    public IDbConnection DbConnection { get; } = dbConnection;
}

partial class Composition
{
    static void Setup() =>

        DI.Setup()
            .Bind().To<DbConnection>()
            .Bind().To<OrderProcessingService>()

            // A special composition root
            // that allows to manage disposable dependencies
            .Root<Owned<IOrderProcessingService>>("OrderProcessingService");
}
```

To run the above code, the following NuGet packages must be added:
 - [Pure.DI](https://www.nuget.org/packages/Pure.DI)
 - [Shouldly](https://www.nuget.org/packages/Shouldly)

>[!NOTE]
>Disposable tracking ensures proper cleanup of all disposable instances within a composition scope.

## Tracking disposable instances in delegates

When a service creates disposable dependencies dynamically, inject `Func<Owned<T>>` instead of `Func<T>`. Each factory call returns an `Owned<T>` that owns all disposables created for that particular graph, so disposing it cleans up exactly those instances — graphs produced by other factory calls remain alive.

```c#
using Shouldly;
using Pure.DI;

var composition = new Composition();
var transaction1 = composition.Transaction;
var transaction2 = composition.Transaction;

transaction2.Dispose();

// Checks that the disposable instances
// associated with transaction2 have been disposed of
transaction2.Connection.IsDisposed.ShouldBeTrue();

// Checks that the disposable instances
// associated with transaction1 have not been disposed of
transaction1.Connection.IsDisposed.ShouldBeFalse();

transaction1.Dispose();

// Checks that the disposable instances
// associated with transaction1 have been disposed of
transaction1.Connection.IsDisposed.ShouldBeTrue();

interface IDbConnection
{
    bool IsDisposed { get; }
}

class DbConnection : IDbConnection, IDisposable
{
    public bool IsDisposed { get; private set; }

    public void Dispose() => IsDisposed = true;
}

interface ITransaction
{
    IDbConnection Connection { get; }
}

class Transaction(Func<Owned<IDbConnection>> connectionFactory)
    : ITransaction, IDisposable
{
    private readonly Owned<IDbConnection> _connection = connectionFactory();

    public IDbConnection Connection => _connection.Value;

    public void Dispose() => _connection.Dispose();
}

partial class Composition
{
    static void Setup() =>

        DI.Setup()
            .Bind().To<DbConnection>()
            .Bind().To<Transaction>()

            // Composition root
            .Root<Transaction>("Transaction");
}
```

To run the above code, the following NuGet packages must be added:
 - [Pure.DI](https://www.nuget.org/packages/Pure.DI)
 - [Shouldly](https://www.nuget.org/packages/Shouldly)

>[!NOTE]
>Disposable tracking in delegates ensures proper cleanup even when instances are created dynamically through factory delegates.

## Tracking disposable instances with different lifetimes

`Owned<T>` tracking respects lifetimes. Disposing an `Owned<T>` immediately disposes the transient dependencies created for that graph, while for a `Singleton` dependency it only releases ownership — the shared instance stays alive for other consumers and is disposed only when the composition itself is disposed.

```c#
using Shouldly;
using Pure.DI;

var composition = new Composition();
var queryHandler1 = composition.QueryHandler;
var queryHandler2 = composition.QueryHandler;

// The exclusive connection is created for each handler
queryHandler1.ExclusiveConnection.ShouldNotBe(queryHandler2.ExclusiveConnection);

// The shared connection is the same for all handlers
queryHandler1.SharedConnection.ShouldBe(queryHandler2.SharedConnection);

// Disposing the second handler
queryHandler2.Dispose();

// Checks that the exclusive connection
// associated with queryHandler2 has been disposed of
queryHandler2.ExclusiveConnection.IsDisposed.ShouldBeTrue();

// But the shared connection is still alive (not disposed)
// because it is a Singleton and shared with other consumers
queryHandler2.SharedConnection.IsDisposed.ShouldBeFalse();

// Checks that the connections associated with root1
// are not affected by queryHandler2 disposal
queryHandler1.ExclusiveConnection.IsDisposed.ShouldBeFalse();
queryHandler1.SharedConnection.IsDisposed.ShouldBeFalse();

// Disposing the first handler
queryHandler1.Dispose();

// Its exclusive connection is now disposed
queryHandler1.ExclusiveConnection.IsDisposed.ShouldBeTrue();

// The shared connection is STILL alive
queryHandler1.SharedConnection.IsDisposed.ShouldBeFalse();

// Disposing the  root composition
// This should dispose all Singletons
composition.Dispose();

// Now the shared connection is disposed
queryHandler1.SharedConnection.IsDisposed.ShouldBeTrue();

interface IConnection
{
    bool IsDisposed { get; }
}

class Connection : IConnection, IDisposable
{
    public bool IsDisposed { get; private set; }

    public void Dispose() => IsDisposed = true;
}

interface IQueryHandler
{
    public IConnection ExclusiveConnection { get; }

    public IConnection SharedConnection { get; }
}

class QueryHandler(
    Func<Owned<IConnection>> connectionFactory,
    [Tag("Shared")] Func<Owned<IConnection>> sharedConnectionFactory)
    : IQueryHandler, IDisposable
{
    private readonly Owned<IConnection> _exclusiveConnection = connectionFactory();
    private readonly Owned<IConnection> _sharedConnection = sharedConnectionFactory();

    public IConnection ExclusiveConnection => _exclusiveConnection.Value;

    public IConnection SharedConnection => _sharedConnection.Value;

    public void Dispose()
    {
        // Disposes the owned instances.
        // For the exclusive connection (Transient), this disposes the actual connection.
        // For the shared connection (Singleton), this just releases the ownership
        // but does NOT dispose the underlying singleton instance until the composition is disposed.
        _exclusiveConnection.Dispose();
        _sharedConnection.Dispose();
    }
}

partial class Composition
{
    static void Setup() =>

        DI.Setup()
            .Bind().To<Connection>()
            .Bind("Shared").As(Lifetime.Singleton).To<Connection>()
            .Bind().To<QueryHandler>()

            // Composition root
            .Root<QueryHandler>("QueryHandler");
}
```

To run the above code, the following NuGet packages must be added:
 - [Pure.DI](https://www.nuget.org/packages/Pure.DI)
 - [Shouldly](https://www.nuget.org/packages/Shouldly)

>[!NOTE]
>The tracking mechanism respects lifetime semantics, ensuring that transient instances are disposed immediately while singleton instances persist until composition disposal.

## Tracking disposable instances using pre-built classes

If you want ready-made classes for tracking disposable objects in your libraries but don't want to create your own, you can add this package to your projects:

[![NuGet](https://img.shields.io/nuget/v/Pure.DI.Abstractions)](https://www.nuget.org/packages/Pure.DI.Abstractions)

It contains attributes like `Inject` and `Inject<T>` that work for constructors and their arguments, methods and their arguments, properties and fields. They allow you to setup all injection parameters.

```c#
using Shouldly;
using Pure.DI.Abstractions;
using Pure.DI;

var composition = new Composition();
var dataService1 = composition.DataService;
var dataService2 = composition.DataService;

// The dedicated connection should be unique for each root
dataService1.Connection.ShouldNotBe(dataService2.Connection);

// The shared connection should be the same instance
dataService1.SharedConnection.ShouldBe(dataService2.SharedConnection);

dataService2.Dispose();

// Checks that the disposable instances
// associated with dataService2 have been disposed of
dataService2.Connection.IsDisposed.ShouldBeTrue();

// But the singleton is still not disposed of
// because it is shared and tracked by the composition
dataService2.SharedConnection.IsDisposed.ShouldBeFalse();

// Checks that the disposable instances
// associated with dataService1 have not been disposed of
dataService1.Connection.IsDisposed.ShouldBeFalse();
dataService1.SharedConnection.IsDisposed.ShouldBeFalse();

dataService1.Dispose();

// Checks that the disposable instances
// associated with dataService1 have been disposed of
dataService1.Connection.IsDisposed.ShouldBeTrue();

// But the singleton is still not disposed of
dataService1.SharedConnection.IsDisposed.ShouldBeFalse();

composition.Dispose();

// The shared singleton is disposed only when the composition is disposed
dataService1.SharedConnection.IsDisposed.ShouldBeTrue();

interface IDbConnection
{
    bool IsDisposed { get; }
}

class DbConnection : IDbConnection, IDisposable
{
    public bool IsDisposed { get; private set; }

    public void Dispose() => IsDisposed = true;
}

interface IDataService
{
    public IDbConnection Connection { get; }

    public IDbConnection SharedConnection { get; }
}

class DataService(
    Func<Own<IDbConnection>> connectionFactory,
    [Tag("shared")] Func<Own<IDbConnection>> sharedConnectionFactory)
    : IDataService, IDisposable
{
    // Own<T> is a wrapper from Pure.DI.Abstractions that owns the value.
    // It ensures that the value is disposed when Own<T> is disposed,
    // but only if the value is not a singleton or externally owned.
    private readonly Own<IDbConnection> _connection = connectionFactory();
    private readonly Own<IDbConnection> _sharedConnection = sharedConnectionFactory();

    public IDbConnection Connection => _connection.Value;

    public IDbConnection SharedConnection => _sharedConnection.Value;

    public void Dispose()
    {
        // Disposes the dedicated connection
        _connection.Dispose();

        // Notifies that we are done with the shared connection.
        // However, since it's a singleton, the underlying instance won't be disposed here.
        _sharedConnection.Dispose();
    }
}

partial class Composition
{
    static void Setup() =>

        DI.Setup()
            .Bind().To<DbConnection>()
            .Bind("shared").As(Lifetime.Singleton).To<DbConnection>()
            .Bind().To<DataService>()

            // Composition root
            .Root<DataService>("DataService");
}
```

To run the above code, the following NuGet packages must be added:
 - [Pure.DI](https://www.nuget.org/packages/Pure.DI)
 - [Shouldly](https://www.nuget.org/packages/Shouldly)
 - [Pure.DI.Abstractions](https://www.nuget.org/packages/Pure.DI.Abstractions)

This package should also be included in a project:

[![NuGet](https://img.shields.io/nuget/v/Pure.DI)](https://www.nuget.org/packages/Pure.DI)

## Tracking async disposable instances per a composition root

Declaring a root as `Root<Owned<T>>` gives every root instance ownership of its own dependency graph, including `IAsyncDisposable` dependencies. Calling `DisposeAsync()` on one `Owned<T>` asynchronously disposes only the instances created for that root, leaving graphs obtained from other root accesses untouched.

```c#
using Shouldly;
using Pure.DI;

var composition = new Composition();
// Creates two independent roots (queries), each with its own dependency graph
var query1 = composition.Query;
var query2 = composition.Query;

// Disposes of the second query
await query2.DisposeAsync();

// Checks that the connection associated with the second query has been closed
query2.Value.Connection.IsDisposed.ShouldBeTrue();

// At the same time, the connection of the first query remains active
query1.Value.Connection.IsDisposed.ShouldBeFalse();

// Disposes of the first query
await query1.DisposeAsync();

// Now the first connection is also closed
query1.Value.Connection.IsDisposed.ShouldBeTrue();

// Interface for a resource requiring asynchronous disposal (e.g., DB)
interface IDbConnection
{
    bool IsDisposed { get; }
}

class DbConnection : IDbConnection, IAsyncDisposable
{
    public bool IsDisposed { get; private set; }

    public ValueTask DisposeAsync()
    {
        IsDisposed = true;
        return ValueTask.CompletedTask;
    }
}

interface IQuery
{
    public IDbConnection Connection { get; }
}

class Query(IDbConnection connection) : IQuery
{
    public IDbConnection Connection { get; } = connection;
}

partial class Composition
{
    static void Setup() =>

        DI.Setup()
            .Bind().To<DbConnection>()
            .Bind().To<Query>()

            // A special composition root 'Owned' that allows
            // managing the lifetime of IQuery and its dependencies
            .Root<Owned<IQuery>>("Query");
}
```

To run the above code, the following NuGet packages must be added:
 - [Pure.DI](https://www.nuget.org/packages/Pure.DI)
 - [Shouldly](https://www.nuget.org/packages/Shouldly)

>[!NOTE]
>Async disposable tracking ensures proper async cleanup of all disposable instances within a composition scope.

## Tracking async disposable instances in delegates

When a service creates `IAsyncDisposable` dependencies dynamically, inject `Func<Owned<T>>` instead of `Func<T>`. Each factory call returns an `Owned<T>` that takes ownership of the dependency graph it just created, so the consumer decides exactly when to call `DisposeAsync()` — and disposing one instance does not affect graphs produced by other calls.

```c#
using Shouldly;
using Pure.DI;

var composition = new Composition();
var queryService1 = composition.QueryService;
var queryService2 = composition.QueryService;

await queryService2.DisposeAsync();

// Checks that the disposable instances
// associated with queryService2 have been disposed of
queryService2.Connection.IsDisposed.ShouldBeTrue();

// Checks that the disposable instances
// associated with queryService1 have not been disposed of
queryService1.Connection.IsDisposed.ShouldBeFalse();

await queryService1.DisposeAsync();

// Checks that the disposable instances
// associated with queryService1 have been disposed of
queryService1.Connection.IsDisposed.ShouldBeTrue();

interface IConnection
{
    bool IsDisposed { get; }
}

class DbConnection : IConnection, IAsyncDisposable
{
    public bool IsDisposed { get; private set; }

    public ValueTask DisposeAsync()
    {
        IsDisposed = true;
        return ValueTask.CompletedTask;
    }
}

interface IQueryService
{
    public IConnection Connection { get; }
}

class QueryService(Func<Owned<IConnection>> connectionFactory)
    : IQueryService, IAsyncDisposable
{
    // The Owned<T> generic type lets you manage the lifetime of a dependency
    // explicitly. In this case, the QueryService creates the connection
    // using a factory and takes ownership of it.
    private readonly Owned<IConnection> _connection = connectionFactory();

    public IConnection Connection => _connection.Value;

    public ValueTask DisposeAsync()
    {
        // When the service is disposed, it also disposes of the connection it owns
        return _connection.DisposeAsync();
    }
}

partial class Composition
{
    static void Setup() =>

        DI.Setup()
            .Bind<IConnection>().To<DbConnection>()
            .Bind().To<QueryService>()

            // Composition root
            .Root<QueryService>("QueryService");
}
```

To run the above code, the following NuGet packages must be added:
 - [Pure.DI](https://www.nuget.org/packages/Pure.DI)
 - [Shouldly](https://www.nuget.org/packages/Shouldly)

>[!NOTE]
>Async disposable tracking in delegates ensures proper async cleanup even when instances are created dynamically through factory delegates.

## Exported roots

Composition roots from other assemblies or projects can be used as a source of bindings. When you add a binding to a composition from another assembly or project, the roots of the composition with the `RootKind.Exported` type will be used in the bindings automatically. For example, in some assembly a composition is defined as:
```c#
public partial class CompositionInOtherProject
{
    private static void Setup() =>
        DI.Setup()
            .Bind().As(Lifetime.Singleton).To<MyDependency>()
            .Bind().To<MyService>()
            .Root<IMyService>("MyService", kind: RootKinds.Exported);
}
```

```c#
using Pure.DI;
using static Pure.DI.Lifetime;
using OtherAssembly;

DI.Setup(nameof(Composition))
    // Binds to exposed composition roots from other project
    .Bind().As(Singleton).To<CompositionInOtherProject>()
    .Root<Program>("Program");

var composition = new Composition();
var program = composition.Program;
program.DoSomething();

partial class Program(IMyService myService)
{
    public void DoSomething() => myService.DoSomething();
}
```

To run the above code, the following NuGet package must be added:
 - [Pure.DI](https://www.nuget.org/packages/Pure.DI)

>[!IMPORTANT]
>At this point, a composition from another assembly or another project can be used for this purpose. Compositions from the current project cannot be used in this way due to limitations of the source code generators.

## Exported roots with tags

Composition roots from other assemblies or projects can be used as a source of bindings. When you add a binding to a composition from another assembly or project, the roots of the composition with the `RootKind.Exported` type will be used in the bindings automatically. For example, in some assembly a composition is defined as:
```c#
public partial class CompositionWithTagsInOtherProject
{
    private static void Setup() =>
        DI.Setup()
            .Bind().As(Lifetime.Singleton).To<MyDependency>()
            .Bind("Some tag").To<MyService>()
            .Root<IMyService>("MyService", "Some tag", RootKinds.Exported);
}
```
Use this when a library exposes ready-made composition roots that must be reused in another composition.

```c#
using Pure.DI;
using static Pure.DI.Lifetime;
using OtherAssembly;

DI.Setup(nameof(Composition))
    // Binds to exposed composition roots from other project
    .Bind().As(Singleton).To<CompositionWithTagsInOtherProject>()
    .Root<Program>("Program");

var composition = new Composition();
var program = composition.Program;
program.DoSomething();

partial class Program([Tag("Some tag")] IMyService myService)
{
    public void DoSomething() => myService.DoSomething();
}
```

To run the above code, the following NuGet package must be added:
 - [Pure.DI](https://www.nuget.org/packages/Pure.DI)

Limitations: Exported roots create an integration contract between assemblies; tag names and root contracts should be versioned carefully.
See also: [Tags](tags.md), [Exported roots](exported-roots.md).

## Exported roots via arg

Composition roots from other assemblies or projects can be used as a source of bindings passed through composition arguments. When you add a binding to a composition from another assembly or project, the roots of the composition with the `RootKind.Exported` type will be used in the bindings automatically. For example, in some assembly a composition is defined as:
```c#
public partial class CompositionInOtherProject
{
    private static void Setup() =>
        DI.Setup()
            .Bind().As(Lifetime.Singleton).To<MyDependency>()
            .Bind().To<MyService>()
            .Root<IMyService>("MyService", kind: RootKinds.Exported);
}
```

```c#
using Shouldly;
using Pure.DI;
using OtherAssembly;

DI.Setup(nameof(Composition))
    // Binds to exposed composition roots from other project
    .Arg<CompositionInOtherProject>("baseComposition")
    .Root<Program>("Program");

var baseComposition = new CompositionInOtherProject();
var composition = new Composition(baseComposition);
var program = composition.Program;
program.DoSomething();

partial class Program(IMyService myService)
{
    public void DoSomething() => myService.DoSomething();
}
```

To run the above code, the following NuGet packages must be added:
 - [Pure.DI](https://www.nuget.org/packages/Pure.DI)
 - [Shouldly](https://www.nuget.org/packages/Shouldly)

>[!IMPORTANT]
>At this point, a composition from another assembly or another project can be used for this purpose. Compositions from the current project cannot be used in this way due to limitations of the source code generators.

## Exported roots via root arg

Composition roots from other assemblies or projects can be used as a source of bindings passed through root arguments. When you add a binding to a composition from another assembly or project, the roots of the composition with the `RootKind.Exported` type will be used in the bindings automatically. For example, in some assembly a composition is defined as:
```c#
public partial class CompositionInOtherProject
{
    private static void Setup() =>
        DI.Setup()
            .Bind().As(Lifetime.Singleton).To<MyDependency>()
            .Bind().To<MyService>()
            .Root<IMyService>("MyService", kind: RootKinds.Exported);
}
```

```c#
using Shouldly;
using Pure.DI;
using OtherAssembly;

DI.Setup(nameof(Composition))
    // Binds to exposed composition roots from other project
    .RootArg<CompositionInOtherProject>("baseComposition")
    .Root<Program>("GetProgram");

var baseComposition = new CompositionInOtherProject();
var composition = new Composition();
var program = composition.GetProgram(baseComposition);
program.DoSomething();

partial class Program(IMyService myService)
{
    public void DoSomething() => myService.DoSomething();
}
```

To run the above code, the following NuGet packages must be added:
 - [Pure.DI](https://www.nuget.org/packages/Pure.DI)
 - [Shouldly](https://www.nuget.org/packages/Shouldly)

>[!IMPORTANT]
>At this point, a composition from another assembly or another project can be used for this purpose. Compositions from the current project cannot be used in this way due to limitations of the source code generators.

## Exported generic roots

Composition roots from other assemblies or projects can be used as a source of bindings. When you add a binding to a composition from another assembly or project, the roots of the composition with the `RootKind.Exported` type will be used in the bindings automatically. For example, in some assembly a composition is defined as:
```c#
public partial class CompositionInOtherProject
{
    private static void Setup() =>
    DI.Setup()
        .Hint(Hint.Resolve, "Off")
        .Bind().To(() => 99)
        .Bind().As(Lifetime.Singleton).To<MyDependency>()
        .Bind().To<MyGenericService<TT>>()
        .Root<IMyGenericService<TT>>("GetMyService", kind: RootKinds.Exported);
}
```

```c#
using Pure.DI;
using static Pure.DI.Lifetime;
using OtherAssembly;

DI.Setup(nameof(Composition))
    // Binds to exposed composition roots from other project
    .Bind().As(Singleton).To<CompositionWithGenericRootsInOtherProject>()
    .Root<Program>("Program");

var composition = new Composition();
var program = composition.Program;
program.DoSomething(99);

partial class Program(IMyGenericService<int> myService)
{
    public void DoSomething(int value) => myService.DoSomething(value);
}
```

To run the above code, the following NuGet package must be added:
 - [Pure.DI](https://www.nuget.org/packages/Pure.DI)

>[!IMPORTANT]
>At this point, a composition from another assembly or another project can be used for this purpose. Compositions from the current project cannot be used in this way due to limitations of the source code generators.

## Exported generic roots with args

Composition roots from other assemblies or projects can be used as a source of bindings. When you add a binding to a composition from another assembly or project, the roots of the composition with the `RootKind.Exported` type will be used in the bindings automatically. For example, in some assembly a composition is defined as:
```c#
public partial class CompositionWithGenericRootsAndArgsInOtherProject
{
    private static void Setup() =>
        DI.Setup()
            .Hint(Hint.Resolve, "Off")
            .RootArg<int>("id")
            .Bind().As(Lifetime.Singleton).To<MyDependency>()
            .Bind().To<MyGenericService<TT>>()
            .Root<IMyGenericService<TT>>("GetMyService", kind: RootKinds.Exported);
}
```

```c#
using Shouldly;
using Pure.DI;
using static Pure.DI.Lifetime;
using OtherAssembly;

DI.Setup(nameof(Composition))
    .RootArg<int>("id")
    // Binds to exposed composition roots from other project
    .Bind().As(Singleton).To<CompositionWithGenericRootsAndArgsInOtherProject>()
    .Root<Program>("GetProgram");

var composition = new Composition();
var program = composition.GetProgram(33);
program.DoSomething(99);

partial class Program(IMyGenericService<int> myService)
{
    public void DoSomething(int value) => myService.DoSomething(value);
}
```

To run the above code, the following NuGet packages must be added:
 - [Pure.DI](https://www.nuget.org/packages/Pure.DI)
 - [Shouldly](https://www.nuget.org/packages/Shouldly)

>[!IMPORTANT]
>At this point, a composition from another assembly or another project can be used for this purpose. Compositions from the current project cannot be used in this way due to limitations of the source code generators.

## Unit testing

A key benefit of dependency injection is testability: to test a service, replace its dependencies with deterministic test doubles. With Pure.DI, this substitution happens in the setup code and is verified at compile time. Put the bindings shared by the application and the tests into a `CompositionKind.Internal` setup, and let each composition — the production one and the test one — add its environment-specific bindings via `DependsOn(...)`.
To use a mocking library such as _Moq_, bind a configured `Mock<T>` as a singleton, map the contract to `mock.Object`, and expose the mock itself as a composition root — the test then reaches the mock through that root to arrange behavior and verify calls.

```c#
using Shouldly;
using Moq;
using Pure.DI;
using static Pure.DI.CompositionKind;

// Bindings shared by the application and the tests
DI.Setup("Shared", Internal)
    .Bind<IOrderService>().To<OrderService>();

// The production composition uses the real clock
DI.Setup(nameof(Composition))
    .DependsOn("Shared")
    .Singleton<SystemClock>()
    .Root<IOrderService>("OrderService");

// The test composition binds a configured mock instead of the real clock
// and exposes it as the "Clock" root so the test can access it
DI.Setup(nameof(TestComposition))
    .DependsOn("Shared")
    .Singleton(_ => {
        // The test replaces the clock with a deterministic mock
        var clock = new Mock<IClock>();
        clock.SetupGet(i => i.Now)
            .Returns(new DateTimeOffset(2024, 1, 1, 12, 0, 0, TimeSpan.Zero));
        return clock;
    }).Root<Mock<IClock>>("Clock")
    .Transient((Mock<IClock> mock) => mock.Object)
    .Root<IOrderService>("OrderService");

// And verifies the service logic against the fixed time
var composition = new TestComposition();
var orderService = composition.OrderService;

// An order placed 31 days before the mocked "now" is expired
orderService.IsExpired(composition.Clock.Object.Now.AddDays(-31)).ShouldBeTrue();

// An order placed 1 day before the mocked "now" is still valid
orderService.IsExpired(composition.Clock.Object.Now.AddDays(-1)).ShouldBeFalse();

// The contract is public so that the mocking library can create a proxy for it
public interface IClock
{
    DateTimeOffset Now { get; }
}

// The real clock used by the application
class SystemClock : IClock
{
    public DateTimeOffset Now => DateTimeOffset.Now;
}

interface IOrderService
{
    bool IsExpired(DateTimeOffset orderDate);
}

// The service under test knows nothing about test doubles
class OrderService(IClock clock) : IOrderService
{
    public bool IsExpired(DateTimeOffset orderDate) =>
        clock.Now - orderDate > TimeSpan.FromDays(30);
}
```

To run the above code, the following NuGet packages must be added:
 - [Pure.DI](https://www.nuget.org/packages/Pure.DI)
 - [Shouldly](https://www.nuget.org/packages/Shouldly)
 - [Moq](https://www.nuget.org/packages/Moq)

>[!TIP]
>If you prefer to avoid mocking libraries, bind a hand-written fake in the test setup instead: `.Bind<IClock>().To<FakeClock>()`.

## Serilog

Serilog loggers are typically enriched with the type of the class that writes the log. The key here is a binding that calls `logger.ForContext(ctx.ConsumerType)`: `ConsumerType` is the type of the consumer of the given dependency, so every class receives a logger whose `SourceContext` is already set to that class. The root logger itself is passed in as a composition argument.

```c#
using Serilog.Core;
using Serilog.Events;
using Pure.DI;
using System.Runtime.CompilerServices;

Serilog.ILogger serilogLogger = new Serilog.LoggerConfiguration().CreateLogger();
var composition = new Composition(logger: serilogLogger);
var service = composition.Root;

interface IDependency;

class Dependency : IDependency;

interface IService
{
    IDependency Dependency { get; }
}

class Service : IService
{
    public Service(Serilog.ILogger log, IDependency dependency)
    {
        Dependency = dependency;
        log.Information("Created");
    }

    public IDependency Dependency { get; }
}

partial class Composition
{
    private void Setup() =>

        DI.Setup(nameof(Composition))
            .Hint(Hint.OnNewInstance, "On")
            .Hint(Hint.OnDependencyInjection, "On")
            // Excluding loggers
            .Hint(Hint.OnNewInstanceImplementationTypeNameRegularExpression, "^((?!Logger).)*$")
            .Hint(Hint.OnDependencyInjectionContractTypeNameRegularExpression, "^((?!Logger).)*$")

            .Arg<Serilog.ILogger>("logger", "from arg")
            .Bind<Serilog.ILogger>().To(ctx =>
            {
                ctx.Inject("from arg", out Serilog.ILogger logger);
                return logger.ForContext(ctx.ConsumerType);
            })

            .Bind().To<Dependency>()
            .Bind().To<Service>()
            .Root<Serilog.ILogger>(nameof(Log), kind: RootKinds.Private)
            .Root<IService>(nameof(Root));

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    partial void OnNewInstance<T>(ref T value, object? tag, Lifetime lifetime) =>
        Log.Information("Created [{Value}], tag [{Tag}] as {Lifetime}", value, tag, lifetime);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private partial T OnDependencyInjection<T>(in T value, object? tag, Lifetime lifetime)
    {
        Log.Information("Injected [{Value}], tag [{Tag}] as {Lifetime}", value, tag, lifetime);
        return value;
    }
}
```

To run the above code, the following NuGet packages must be added:
 - [Pure.DI](https://www.nuget.org/packages/Pure.DI)
 - [Serilog.Core](https://www.nuget.org/packages/Serilog.Core)
 - [Serilog.Events](https://www.nuget.org/packages/Serilog.Events)

>[!NOTE]
>This example also turns on the `OnNewInstance` and `OnDependencyInjection` hints to log the creation and injection of composition objects. The regular-expression hints exclude the logger types themselves from these callbacks.

## JSON serialization

Serialization can be hidden behind injectable functions instead of scattering `JsonSerializer` calls across the code. A shared `JsonSerializerOptions` instance is bound as a singleton, and two generic bindings tagged `JSON` expose `Func<string, TT?>` and `Func<TT, string>` delegates that deserialize and serialize any type using those options. `SettingsService` then just injects these functions to load and save its `Settings`, keeping it decoupled from the serializer.

```c#
using Shouldly;
using Pure.DI;
using System.Text.Json;
using static Pure.DI.Lifetime;
using static Pure.DI.Tag;

var composition = new Composition();
var settings = composition.Settings;
settings.Size.ShouldBe(10);

settings.Size = 99;
settings.Size.ShouldBe(99);

settings.Size = 33;
settings.Size.ShouldBe(33);

record Settings(int Size)
{
    public static readonly Settings Default = new(10);
}

interface IStorage
{
    void Save(string data);

    string? Load();
}

class Storage : IStorage
{
    private string? _data;

    public void Save(string data) => _data = data;

    public string? Load() => _data;
}

interface ISettingsService
{
    int Size { get; set; }
}

class SettingsService(
    [Tag(JSON)] Func<string, Settings?> deserialize,
    [Tag(JSON)] Func<Settings, string> serialize,
    IStorage storage)
    : ISettingsService
{
    public int Size
    {
        // ReSharper disable once ConditionalAccessQualifierIsNonNullableAccordingToAPIContract
        get => GetSettings().Size;
        set => SaveSettings(GetSettings() with { Size = value });
    }

    private Settings GetSettings() =>
        storage.Load() is {} data && deserialize(data) is {} settings
            ? settings
            : Settings.Default;

    private void SaveSettings(Settings settings) =>
        storage.Save(serialize(settings));
}

partial class Composition
{
    private void Setup() =>

        DI.Setup(nameof(Composition))
            .Root<ISettingsService>(nameof(Settings))
            .Bind().To<SettingsService>()
            .DefaultLifetime(Singleton)
            .Bind().To(() => new JsonSerializerOptions { WriteIndented = true })
            .Bind(JSON).To<JsonSerializerOptions, Func<string, TT?>>(options => json => JsonSerializer.Deserialize<TT>(json, options))
            .Bind(JSON).To<JsonSerializerOptions, Func<TT, string>>(options => value => JsonSerializer.Serialize(value, options))
            .Bind().To<Storage>();
}
```

To run the above code, the following NuGet packages must be added:
 - [Pure.DI](https://www.nuget.org/packages/Pure.DI)
 - [Shouldly](https://www.nuget.org/packages/Shouldly)

>[!TIP]
>Binding delegates with generic type markers like `TT` produces a serialize/deserialize function for every type where it is injected — no per-type bindings are needed.

## AutoMapper

AutoMapper creates target objects itself, so mapped instances normally bypass DI even when they need dependencies. Here a configured `IMapper` is bound as a singleton, and a generic `Func<TT1, TT2>` binding wraps `mapper.Map` so that consumers like `StudentService` simply inject a mapping function for the types they need. After mapping, `ctx.BuildUp(target)` injects the members marked with `[Inject]` — such as `Person.Formatter` — into the freshly mapped object.

```c#
using Shouldly;
using AutoMapper;
using Microsoft.Extensions.DependencyInjection;
using Pure.DI.Abstractions;
using Pure.DI;
using Pure.DI.Abstractions;
using AutoMapper;
using Microsoft.Extensions.Logging;
using static Pure.DI.Lifetime;
using static Pure.DI.Tag;

var logMessages = new List<string>();
using var composition = new Composition(logMessages);
var root = composition.Root;

root.Run();
logMessages.ShouldContain("John Smith");

class Person
{
    public string? FirstName { get; set; }

    public string? LastName { get; set; }

    public DateTime? BirthDate { get; set; }

    [Inject]
    public IPersonFormatter? Formatter { get; set; }
}

class Student
{
    public string? FirstName { get; set; }

    public string? LastName { get; set; }

    public DateTime? BirthDate { get; set; }

    public DateTime? AdmissionDate { get; set; }
}

interface IPersonFormatter
{
    string Format(Person person);
}

class PersonFormatter : IPersonFormatter
{
    public string Format(Person person) => $"{person.FirstName} {person.LastName}";
}

interface IStudentService
{
    string AsPersonText(Student student);
}

class StudentService(Func<Student, Person> map) : IStudentService
{
    public string AsPersonText(Student student)
    {
        var person = map(student);
        return person.Formatter?.Format(person) ?? "";
    }
}

partial class Program(ILogger logger, IStudentService studentService)
{
    public void Run()
    {
        var nik = new Student { FirstName = "John", LastName = "Smith" };
        var personText = studentService.AsPersonText(nik);
        logger.LogInformation(personText);
    }
}

partial class Composition
{
    private void Setup() =>

        DI.Setup(nameof(Composition))
            .Root<Program>(nameof(Root))
            .Arg<ICollection<string>>("logMessage")
            // Example dependency for Program
            .Bind().To<StudentService>()

            .DefaultLifetime(Singleton)
                // Example dependency for Person
                .Bind().To<PersonFormatter>()
                // Logger for AutoMapper
                .Bind().To<LoggerFactory>()
                .Bind().To((LoggerFactory loggerFactory) => loggerFactory.CreateLogger("info"))
                // Provides a mapper
                .Bind<IMapper>().To<LoggerFactory, Mapper>(loggerFactory => {
                    // Create the mapping configuration
                    var configuration = new MapperConfiguration(cfg => {
                            cfg.CreateMap<Student, Person>();
                        },
                        loggerFactory);
                    configuration.CompileMappings();
                    // Create the mapper
                    return new Mapper(configuration);
                })
                // Maps TT1 -> TT2
                .Bind().To<Func<TT1, TT2>>(ctx => source => {
                    ctx.Inject(out IMapper mapper);
                    // source -> target
                    var target = mapper.Map<TT1, TT2>(source);
                    // Building-up a mapped value with dependencies
                    ctx.BuildUp(target);
                    return target;
                });
}

class LoggerFactory(ICollection<string> logMessages)
    : ILoggerFactory
{
    public void AddProvider(ILoggerProvider provider) {}

    public ILogger CreateLogger(string categoryName) => new Logger(logMessages);

    public void Dispose() { }

    private class Logger(ICollection<string> logMessages): ILogger
    {
        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter) =>
            logMessages.Add(formatter(state, exception));

        public bool IsEnabled(LogLevel logLevel) => true;

        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;
    }
}
```

To run the above code, the following NuGet packages must be added:
 - [Pure.DI](https://www.nuget.org/packages/Pure.DI)
 - [Shouldly](https://www.nuget.org/packages/Shouldly)
 - [AutoMapper](https://www.nuget.org/packages/AutoMapper)
 - [Microsoft.Extensions.DependencyInjection](https://www.nuget.org/packages/Microsoft.Extensions.DependencyInjection)
 - [Pure.DI.Abstractions](https://www.nuget.org/packages/Pure.DI.Abstractions)

>[!NOTE]
>Since the `IMapper` binding is a singleton, the mapping configuration is created and compiled only once and then reused for all mappings.

## Request overrides

When this occurs: you need per-request overrides with different scopes for nested services.
What it solves: applies request data to the main workflow while keeping background or system dependencies isolated.
How it is solved in the example: uses nested factories and overrides to select the nearest context.

```c#
using Pure.DI;

var composition = new Composition();

var handler = composition.CreateHandler(new Request("tenant-a", "user-1"));

record Request(string TenantId, string UserId);

interface IRequestContext
{
    string TenantId { get; }

    string UserId { get; }

    bool IsSystem { get; }
}

class RequestContext(string tenantId, string userId, bool isSystem) : IRequestContext
{
    public static RequestContext System => new("system", "system", true);

    public string TenantId { get; } = tenantId;

    public string UserId { get; } = userId;

    public bool IsSystem { get; } = isSystem;
}

interface IRepository
{
    IRequestContext Context { get; }
}

class Repository(IRequestContext context) : IRepository
{
    public IRequestContext Context { get; } = context;
}

interface IAuditWriter
{
    IRequestContext Context { get; }
}

class AuditWriter(IRequestContext context) : IAuditWriter
{
    public IRequestContext Context { get; } = context;
}

class Service(
    IRequestContext context,
    Func<IRepository> repositoryFactory,
    IAuditWriter audit)
{
    public IRequestContext Context { get; } = context;

    public IRepository Repository { get; } = repositoryFactory();

    public IAuditWriter Audit { get; } = audit;
}

class Handler(Service service)
{
    public Service Service { get; } = service;
}

partial class Composition
{
    private void Setup() =>
        DI.Setup(nameof(Composition))
            .Bind<IRepository>().To<Repository>()
            .Bind<IAuditWriter>().To<AuditWriter>()
            .Bind().To<Service>()
            .Bind().To<Handler>()
            .Bind().To<Func<IRepository>>(ctx => () =>
            {
                // Inner override applies to repository dependencies only.
                ctx.Override<IRequestContext>(RequestContext.System);
                ctx.Inject(out IRepository repository);
                return repository;
            })
            .Bind().To<Func<Request, Handler>>(ctx => request =>
            {
                // Outer override applies to the request handler and its main workflow.
                ctx.Override<IRequestContext>(new RequestContext(request.TenantId, request.UserId, false));
                ctx.Inject(out Handler handler);
                return handler;
            })
            .Root<Func<Request, Handler>>(nameof(CreateHandler));
}
```

To run the above code, the following NuGet package must be added:
 - [Pure.DI](https://www.nuget.org/packages/Pure.DI)

What it shows:
- Demonstrates override precedence across nested factories.
- Shows that the closest override wins for deeper dependencies.

Important points:
- Multiple overrides for the same type pick the nearest one in the graph.
- The last override on the same level wins.

Useful when:
- You handle multi-tenant requests and need system services to run under a system context.

## Unity Basics

In Unity, `MonoBehaviour` instances are created by the engine, not by your code, so constructor injection is not an option. Pure.DI solves this with builders: the `Builders<MonoBehaviour>()` call generates a `BuildUp` method for every `MonoBehaviour` in the composition, which injects the members marked with `[Dependency]`. Here `Clock` calls `scope.BuildUp(this)` in `Awake()` to receive its `IClockService`, while regular (non-`MonoBehaviour`) dependencies like `ClockService` are wired up with ordinary bindings.

```c#
using Shouldly;
using Pure.DI;
using UnityEngine;

public class Clock : MonoBehaviour
{
    const float HoursToDegrees = -30f, MinutesToDegrees = -6f, SecondsToDegrees = -6f;
    [SerializeField] Scope scope;
    [SerializeField] Transform hoursPivot;
    [SerializeField] Transform minutesPivot;
    [SerializeField] Transform secondsPivot;

    [Dependency]
    public IClockService ClockService { private get; set; }

    public void Awake()
    {
        scope.BuildUp(this);
    }

    public void Update()
    {
        var now = ClockService.Now.TimeOfDay;
        hoursPivot.localRotation = Quaternion.Euler(0f, 0f, HoursToDegrees * (float)now.TotalHours);
        minutesPivot.localRotation = Quaternion.Euler(0f, 0f, MinutesToDegrees * (float)now.TotalMinutes);
        secondsPivot.localRotation = Quaternion.Euler(0f, 0f, SecondsToDegrees * (float)now.TotalSeconds);
    }
}

public interface IClockConfig
{
    TimeSpan Offset { get; }
}

[CreateAssetMenu(fileName = "ClockConfig", menuName = "Clock/Config")]
public class ClockConfig : ScriptableObject, IClockConfig
{
    [SerializeField] int offsetHours;

    public TimeSpan Offset => TimeSpan.FromHours(offsetHours);
}

public interface IClockService
{
    DateTime Now { get; }
}

public class ClockService : IClockService, IDisposable
{
    private readonly IClockConfig _config;

    public DateTime Now => DateTime.UtcNow + _config.Offset;

    public ClockService(IClockConfig config)
    {
        _config = config;
    }

    public void Dispose()
    {
        GC.SuppressFinalize(this);
        // Perform any necessary cleanup here
    }
}

public partial class Scope : MonoBehaviour
{
    [SerializeField] ClockConfig clockConfig;

    void Setup() =>
        DI.Setup()
        .Bind().To(() => clockConfig)
        .Bind().As(Lifetime.Singleton).To<ClockService>()
        .Builders<MonoBehaviour>();

    void OnDestroy()
    {
        Dispose();
    }
}
```

To run the above code, the following NuGet packages must be added:
 - [Pure.DI](https://www.nuget.org/packages/Pure.DI)
 - [Shouldly](https://www.nuget.org/packages/Shouldly)

>[!NOTE]
>Call `BuildUp` in `Awake()` so that dependencies are ready before the first `Update()` runs.

## Unity with prefabs

Components created from prefabs at runtime also need their dependencies injected. Building on the basic Unity example, the `ClockManager` composition root instantiates the `ClockDigital` prefab with `Object.Instantiate` and immediately passes the new instance to `BuildUp`, which fills in its `[Dependency]` members. The `Builders<MonoBehaviour>()` call generates such a `BuildUp` method for every `MonoBehaviour` in the setup.

```c#
using Shouldly;
using Pure.DI;
using UnityEngine;

public class Clock : MonoBehaviour
{
    const float HoursToDegrees = -30f, MinutesToDegrees = -6f, SecondsToDegrees = -6f;
    [SerializeField] Scope scope;
    [SerializeField] Transform hoursPivot;
    [SerializeField] Transform minutesPivot;
    [SerializeField] Transform secondsPivot;

    [Dependency]
    public IClockService ClockService { private get; set; }

    public void Awake()
    {
        scope.BuildUp(this);
    }

    public void Update()
    {
        var now = ClockService.Now.TimeOfDay;
        hoursPivot.localRotation = Quaternion.Euler(0f, 0f, HoursToDegrees * (float)now.TotalHours);
        minutesPivot.localRotation = Quaternion.Euler(0f, 0f, MinutesToDegrees * (float)now.TotalMinutes);
        secondsPivot.localRotation = Quaternion.Euler(0f, 0f, SecondsToDegrees * (float)now.TotalSeconds);
    }
}

public class ClockDigital : MonoBehaviour
{
    [SerializeField] private Text timeText;

    [Dependency] public IClockService ClockService { private get; set; }

    void FixedUpdate()
    {
        var now = ClockService.Now;
        timeText.text = now.ToString("HH:mm:ss");
    }
}

public interface IClockConfig
{
    TimeSpan Offset { get; }

    bool ShowDigital { get; }

    ClockDigital ClockDigitalPrefab { get; }
}

[CreateAssetMenu(fileName = "ClockConfig", menuName = "Clock/Config")]
public class ClockConfig : ScriptableObject, IClockConfig
{
    [SerializeField] int offsetHours;
    [SerializeField] bool showDigital;
    [SerializeField] ClockDigital clockDigitalPrefab;

    public TimeSpan Offset => TimeSpan.FromHours(offsetHours);

    public bool ShowDigital => showDigital;

    public ClockDigital ClockDigitalPrefab => clockDigitalPrefab;
}

public interface IClockService
{
    DateTime Now { get; }
}

public class ClockService : IClockService, IDisposable
{
    private readonly IClockConfig _config;

    public ClockService(IClockConfig config)
    {
        _config = config;
    }

    public DateTime Now => DateTime.UtcNow + _config.Offset;

    public void Dispose()
    {
        SuppressFinalize(this);
        // Perform any necessary cleanup here
    }
}

public class ClockManager : IDisposable
{
    private readonly Scope _scope;
    private readonly IClockConfig _config;

    public ClockManager(Scope scope, IClockConfig config)
    {
        _scope = scope;
        _config = config;
    }

    public void Start()
    {
        if (_config.ShowDigital)
        {
            _scope.BuildUp(Object.Instantiate(_config.ClockDigitalPrefab));
        }
    }

    public void Dispose()
    {
        SuppressFinalize(this);
        // Perform any necessary cleanup here
    }
}

public partial class Scope : MonoBehaviour
{
    [SerializeField]  ClockConfig clockConfig;

    void Setup() => DI.Setup()
        .Bind().To(() => clockConfig)
        .Bind().As(Lifetime.Singleton).To<ClockService>()
        .Root<ClockManager>(nameof(ClockManager))
        .Builders<MonoBehaviour>();

    void Start()
    {
        ClockManager.Start();
    }

    void OnDestroy()
    {
        Dispose();
    }
}
```

To run the above code, the following NuGet packages must be added:
 - [Pure.DI](https://www.nuget.org/packages/Pure.DI)
 - [Shouldly](https://www.nuget.org/packages/Shouldly)

>[!NOTE]
>Call `BuildUp` right after `Object.Instantiate` so the component's dependencies are set before Unity starts calling its lifecycle methods such as `FixedUpdate()`.

## Unity scene scopes

In Unity, a loaded scene is a natural lifetime boundary: services should be shared within one scene and isolated from another. This example models that with scopes — each scene gets its own scope created via the generated `SetupScope` method, `Scoped` services are unique per scene, and `Singleton` services remain shared across all scenes through the application-level composition.
Unity itself creates the `MonoBehaviour` instances, so Pure.DI only builds them up, injecting members marked with `[Dependency]` instead of using constructors.

```c#
using Shouldly;
using Pure.DI;
using UnityEngine;
using static Pure.DI.Lifetime;

// Application scope: one root for shared singletons.
var application = new Scope("Application");

// Unity loads two scenes. Each scene has its own MonoBehaviour scope object.
var menuScene = Scope.SetupScope(application, new Scope("Menu"));
var levelScene = Scope.SetupScope(application, new Scope("Level"));

// Unity creates MonoBehaviour instances. Pure.DI only builds them up.
var menuClock1 = new Clock(menuScene);
var menuClock2 = new Clock(menuScene);
var levelClock = new Clock(levelScene);

menuClock1.Awake();
menuClock2.Awake();
levelClock.Awake();

// Same scene => same scoped dependency.
menuClock1.Session.ShouldBe(menuClock2.Session);

// Different scenes => different scoped dependencies.
menuClock1.Session.ShouldNotBe(levelClock.Session);

// Singleton dependency is still shared from the application scope.
menuClock1.ClockService.ShouldBe(levelClock.ClockService);

menuScene.Dispose();
menuClock1.Session.IsDisposed.ShouldBeTrue();
levelClock.Session.IsDisposed.ShouldBeFalse();

levelScene.Dispose();
levelClock.Session.IsDisposed.ShouldBeTrue();

application.Dispose();
menuClock1.ClockService.IsDisposed.ShouldBeTrue();

public class Clock : MonoBehaviour
{
    [SerializeField] Scope scope;

    [Dependency]
    public IClockService ClockService { get; set; }

    [Dependency]
    public IClockSession Session { get; set; }

    public void Awake()
    {
        scope.BuildUp(this);
    }
}

public interface IClockConfig
{
    TimeSpan Offset { get; }
}

[CreateAssetMenu(fileName = "ClockConfig", menuName = "Clock/Config")]
public class ClockConfig : ScriptableObject, IClockConfig
{
    [SerializeField] int offsetHours;

    public TimeSpan Offset => TimeSpan.FromHours(offsetHours);
}

public interface IClockService
{
    DateTime Now { get; }

    bool IsDisposed { get; }
}

public class ClockService(IClockConfig config) : IClockService, IDisposable
{
    public DateTime Now => DateTime.UtcNow + config.Offset;

    public bool IsDisposed { get; private set; }

    public void Dispose() => IsDisposed = true;
}

public interface IClockSession
{
    string SceneName { get; }

    bool IsDisposed { get; }
}

public class ClockSession([Tag("scene name")] string sceneName) : IClockSession, IDisposable
{
    public string SceneName { get; } = sceneName;

    public bool IsDisposed { get; private set; }

    public void Dispose() => IsDisposed = true;
}

public partial class Scope : MonoBehaviour
{
    [SerializeField] ClockConfig clockConfig;
    [SerializeField] string sceneName;

    void Setup() => DI.Setup()
        .Hint(Hint.ScopeMethodName, "SetupScope")
        .Bind().To(() => clockConfig)
        .Bind("scene name").To(_ => sceneName)
        .Bind<IClockService>().As(Singleton).To<ClockService>()
        .Bind<IClockSession>().As(Scoped).To<ClockSession>()
        .Builders<MonoBehaviour>();

    void OnDestroy()
    {
        Dispose();
    }
}
```

To run the above code, the following NuGet packages must be added:
 - [Pure.DI](https://www.nuget.org/packages/Pure.DI)
 - [Shouldly](https://www.nuget.org/packages/Shouldly)

>[!NOTE]
>In a real Unity project the scene objects are created by Unity. The sample uses constructors only to simulate serialized references in a test.

# Examples of using Pure.DI for different types of .NET projects.

#### Avalonia application

[![CSharp](https://img.shields.io/badge/C%23-code-blue.svg)](/samples/AvaloniaApp)

This example shows how to build an [Avalonia](https://avaloniaui.net/) application with Pure.DI, using generated composition roots for view models and infrastructure services instead of a runtime container.

> [!TIP]
> XAML binds to virtual composition roots such as `App` and `Clock`. `Hint.Resolve` is disabled because the sample uses explicit roots instead of generated service-locator methods.

> [!NOTE]
> [Another example](/samples/SingleRootAvaloniaApp) with Avalonia shows how to create an application with a single composition root.

The definition of the composition is in [Composition.cs](/samples/AvaloniaApp/Composition.cs). This class sets up how the object graphs will be created for the application. Do not forget to define any necessary composition roots, for example, these can be view models such as _ClockViewModel_:

```c#
using Pure.DI;
using static Pure.DI.Lifetime;
using static Pure.DI.RootKinds;

namespace AvaloniaApp;

partial class Composition
{
    [System.Diagnostics.Conditional("DI")]
    private void Setup() => DI.Setup()
        .Hint(Hint.Resolve, "Off")

        // Virtual roots are properties on the composition class but are not
        // backed by a separate private field - XAML reads them through the
        // DataContext binding chain, so no dedicated storage is needed.
        .Root<IAppViewModel>(nameof(App), kind: Virtual)
        .Root<IClockViewModel>(nameof(Clock), kind: Virtual)

        .Bind().As(Singleton).To<ClockViewModel>()
        .Bind().To<ClockModel>()
        .Bind().As(Singleton).To<Ticks>()

        // Infrastructure
        .Bind().To<DebugLog<TT>>()
        .Bind().To<AvaloniaDispatcher>();
}
```

A design-time composition can override the same virtual roots with predictable view models for the Avalonia designer. The design-time setup is in [DesignTimeComposition.cs](/samples/AvaloniaApp/DesignTimeComposition.cs):

```c#
using Pure.DI;
using static Pure.DI.RootKinds;

namespace AvaloniaApp;

partial class DesignTimeComposition: Composition
{
    [System.Diagnostics.Conditional("DI")]
    private void Setup() => DI.Setup()
        .Hint(Hint.Resolve, "Off")

        // Overrides virtual roots with design-time view models
        .Root<IAppViewModel>(nameof(App), kind: Override)
        .Root<IClockViewModel>(nameof(Clock), kind: Override)

        .Bind().To<DesignTimeAppViewModel>()
        .Bind().To<DesignTimeClockViewModel>();
}
```

A single instance of the _Composition_ class is defined as a static resource in [App.xaml](/samples/AvaloniaApp/App.axaml) for later use within the _XAML_ markup everywhere:

```xml
<Application xmlns="https://github.com/avaloniaui"
             xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
             x:Class="AvaloniaApp.App"
             xmlns:app="using:AvaloniaApp"
             RequestedThemeVariant="Default">

  <!-- "Default" ThemeVariant follows system theme variant.
  "Dark" or "Light" are other available options. -->
  <Application.Styles>
    <FluentTheme />
  </Application.Styles>

  <Application.Resources>
    <!--Creates a shared resource of type Composition and with key "Composition",
    which will be further used as a data context in the views.-->
    <app:Composition x:Key="Composition"  />
  </Application.Resources>

</Application>
```

This markup fragment

```xml
<Application.Resources>
    <app:Composition x:Key="Composition" />
</Application.Resources>
```

creates a shared resource of type `Composition` with key _"Composition"_, which will be further used as a data context in the views.

Dispose the composition when the Avalonia lifetime exits. This releases singleton and scoped disposable instances created by Pure.DI.

The associated application [App.axaml.cs](/samples/AvaloniaApp/App.axaml.cs) class looks like:

```c#
public class App : Application
{
    public override void Initialize() => AvaloniaXamlLoader.Load(this);

    public override void OnFrameworkInitializationCompleted()
    {
        if (Resources[nameof(Composition)] is Composition composition)
        {
            // Assigns the main window/view
            switch (ApplicationLifetime)
            {
                case IClassicDesktopStyleApplicationLifetime desktop:
                    desktop.MainWindow = new MainWindow();
                    break;

                case ISingleViewApplicationLifetime singleView:
                    singleView.MainView = new MainWindow();
                    break;
            }

            // Handles disposables
            if (ApplicationLifetime is IControlledApplicationLifetime controlledLifetime)
            {
                controlledLifetime.Exit += (_, _) => composition.Dispose();
            }
        }

        base.OnFrameworkInitializationCompleted();
    }
}
```

You can now use bindings and the code-behind-free approach. All previously defined composition roots are now available from [markup](/samples/AvaloniaApp/MainWindow.axaml) without any effort, e.g. _Clock_:

```xml
<Window xmlns="https://github.com/avaloniaui"
        xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
        xmlns:d="http://schemas.microsoft.com/expression/blend/2008"
        xmlns:mc="http://schemas.openxmlformats.org/markup-compatibility/2006"
        xmlns:app="clr-namespace:AvaloniaApp"
        mc:Ignorable="d" d:DesignWidth="800" d:DesignHeight="450"
        x:Class="AvaloniaApp.MainWindow"
        DataContext="{StaticResource Composition}"
        x:DataType="app:Composition"
        Design.DataContext="{d:DesignInstance app:DesignTimeComposition}"
        Title="{Binding App.Title}"
        Icon="/Assets/avalonia-logo.ico"
        FontFamily="Consolas"
        FontWeight="Bold">

  <StackPanel HorizontalAlignment="Center" VerticalAlignment="Center" DataContext="{Binding Clock}">
    <TextBlock Text="{Binding Date}" FontSize="64" HorizontalAlignment="Center" />
    <TextBlock Text="{Binding Time}" FontSize="128" HorizontalAlignment="Center" />
  </StackPanel>

</Window>
```

To use bindings in views:

- You can set a shared resource as a data context

  `DataContext="{StaticResource Composition}"`

  and `Design.DataContext="{d:DesignInstance app:DesignTimeComposition}"` for the design time

- Specify the data type in the context:

  `xmlns:app="clr-namespace:AvaloniaApp"`

  `x:DataType="app:Composition"`

- Use the bindings as usual:

```xml
  <StackPanel HorizontalAlignment="Center" VerticalAlignment="Center" DataContext="{Binding Clock}">
    <TextBlock Text="{Binding Date}" FontSize="64" HorizontalAlignment="Center" />
    <TextBlock Text="{Binding Time}" FontSize="128" HorizontalAlignment="Center" />
  </StackPanel>
```

The [project file](/samples/AvaloniaApp/AvaloniaApp.csproj) looks like this:

```xml
<Project Sdk="Microsoft.NET.Sdk">
    ...
    <ItemGroup>
        <PackageReference Include="Pure.DI" Version="2.5.0">
            <PrivateAssets>all</PrivateAssets>
            <IncludeAssets>runtime; build; native; contentfiles; analyzers; buildtransitive</IncludeAssets>
        </PackageReference>
    </ItemGroup>

</Project>
```

It contains an additional reference to the NuGet package:

|         |                                                                                            |                          |
|---------|--------------------------------------------------------------------------------------------|:-------------------------|
| Pure.DI | [![NuGet](https://img.shields.io/nuget/v/Pure.DI)](https://www.nuget.org/packages/Pure.DI) | DI source code generator |

#### Blazor Server application

[![CSharp](https://img.shields.io/badge/C%23-code-blue.svg)](/samples/BlazorServerApp)

This example shows how to build a [Blazor Server](https://learn.microsoft.com/en-us/aspnet/core/blazor/hosting-models#blazor-server) application with Pure.DI while still integrating with the ASP.NET Core hosting and service-provider pipeline.

> [!NOTE]
> The composition is installed as the host service-provider factory. Components can still use the standard Blazor/ASP.NET Core service infrastructure, while application view models come from generated Pure.DI roots.

The composition setup file is [Composition.cs](/samples/BlazorServerApp/Composition.cs):

```c#
using Pure.DI;
using Pure.DI.MS;
using static Pure.DI.Lifetime;

namespace BlazorServerApp;

partial class Composition : ServiceProviderFactory<Composition>
{
    // IMPORTANT:
    // Only composition roots (regular or anonymous) can be resolved through the `IServiceProvider` interface.
    // These roots must be registered using `Root<>(...)` or `Roots<>()` calls.
    [System.Diagnostics.Conditional("DI")]
    private static void Setup() => DI.Setup()
        .Root<IAppViewModel>()
        .Root<IClockViewModel>()

        .Bind().To<ClockViewModel>()
        .Bind().To<ClockModel>()
        .Bind().As(Singleton).To<Ticks>()

        // Infrastructure
        .Bind().To<MicrosoftLoggerAdapter<TT>>()
        .Bind().To<CurrentThreadDispatcher>();
}
```

The composition class inherits from `ServiceProviderFactory<T>`, where `T` is the composition class itself. Only registered roots can be resolved through the Microsoft `IServiceProvider`, so each view model consumed by the host must be listed with `Root`.

The web application entry point is in the [Program.cs](/samples/BlazorServerApp/Program.cs) file:

```c#
var builder = WebApplication.CreateBuilder(args);

// Uses Composition as an alternative IServiceProviderFactory
using var composition = new Composition();
builder.Host.UseServiceProviderFactory(composition);
```

The [project file](/samples/BlazorServerApp/BlazorServerApp.csproj) looks like this:

```xml
<Project Sdk="Microsoft.NET.Sdk.Web">
    ...
    <ItemGroup>
        <PackageReference Include="Pure.DI" Version="2.5.0">
            <PrivateAssets>all</PrivateAssets>
            <IncludeAssets>runtime; build; native; contentfiles; analyzers; buildtransitive</IncludeAssets>
        </PackageReference>
        <PackageReference Include="Pure.DI.MS" Version="2.5.0" />
    </ItemGroup>

</Project>
```

It contains additional references to NuGet packages:

|            |                                                                                                  |                                               |
|------------|--------------------------------------------------------------------------------------------------|:----------------------------------------------|
| Pure.DI    | [![NuGet](https://img.shields.io/nuget/v/Pure.DI)](https://www.nuget.org/packages/Pure.DI)       | DI source code generator                      |
| Pure.DI.MS | [![NuGet](https://img.shields.io/nuget/v/Pure.DI.MS)](https://www.nuget.org/packages/Pure.DI.MS) | Add-ons for Pure.DI to work with Microsoft DI |

#### Blazor WebAssembly application

[![CSharp](https://img.shields.io/badge/C%23-code-blue.svg)](/samples/BlazorWebAssemblyApp)

[Here's an example](https://devteam.github.io/Pure.DI/) on GitHub Pages.

This example shows how to build a [Blazor WebAssembly](https://learn.microsoft.com/en-us/aspnet/core/blazor/hosting-models#blazor-webassembly) application with Pure.DI, exposing generated roots through the WebAssembly host container.

> [!NOTE]
> WebAssembly uses `builder.ConfigureContainer(composition)` instead of `UseServiceProviderFactory`. Keep browser-specific services, such as `HttpClient`, registered in the normal Blazor service collection.

The composition setup file is [Composition.cs](/samples/BlazorWebAssemblyApp/Composition.cs):

```c#
using Pure.DI;
using Pure.DI.MS;
using static Pure.DI.Lifetime;

namespace BlazorWebAssemblyApp;

partial class Composition : ServiceProviderFactory<Composition>
{
    // IMPORTANT:
    // Only composition roots (regular or anonymous) can be resolved through the `IServiceProvider` interface.
    // These roots must be registered using `Root<>(...)` or `Roots<>()` calls.
    [System.Diagnostics.Conditional("DI")]
    private static void Setup() => DI.Setup()
        .Root<IAppViewModel>()
        .Root<IClockViewModel>()

        .Bind().As(Singleton).To<ClockViewModel>()
        .Bind().To<ClockModel>()
        .Bind().As(Singleton).To<Ticks>()

        // Infrastructure
        .Bind().To<MicrosoftLoggerAdapter<TT>>()
        .Bind().To<CurrentThreadDispatcher>();
}
```

The composition class inherits from `ServiceProviderFactory<T>`, where `T` is the composition class itself. Only registered roots can be resolved through the Microsoft `IServiceProvider`, so each view model consumed by the host must be listed with `Root`.

The web application entry point is in the [Program.cs](/samples/BlazorWebAssemblyApp/Program.cs) file:

```c#
var builder = WebAssemblyHostBuilder.CreateDefault(args);

// Uses Composition as an alternative IServiceProviderFactory
using var composition = new Composition();
builder.ConfigureContainer(composition);

builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

builder.Services.AddScoped(_ => new HttpClient { BaseAddress = new Uri(builder.HostEnvironment.BaseAddress) });

await builder.Build().RunAsync();
```

The [project file](/samples/BlazorWebAssemblyApp/BlazorWebAssemblyApp.csproj) looks like this:

```xml
<Project Sdk="Microsoft.NET.Sdk.Web">
    ...
    <ItemGroup>
        <PackageReference Include="Pure.DI" Version="2.5.0">
            <PrivateAssets>all</PrivateAssets>
            <IncludeAssets>runtime; build; native; contentfiles; analyzers; buildtransitive</IncludeAssets>
        </PackageReference>
        <PackageReference Include="Pure.DI.MS" Version="2.5.0" />
    </ItemGroup>

</Project>
```

It contains additional references to NuGet packages:

|            |                                                                                                  |                                               |
|------------|--------------------------------------------------------------------------------------------------|:----------------------------------------------|
| Pure.DI    | [![NuGet](https://img.shields.io/nuget/v/Pure.DI)](https://www.nuget.org/packages/Pure.DI)       | DI source code generator                      |
| Pure.DI.MS | [![NuGet](https://img.shields.io/nuget/v/Pure.DI.MS)](https://www.nuget.org/packages/Pure.DI.MS) | Add-ons for Pure.DI to work with Microsoft DI |

#### Console Native AOT application

[![CSharp](https://img.shields.io/badge/C%23-code-blue.svg)](/samples/ShroedingersCatNativeAOT)

This example shows how the simple console composition can be published as a [native AOT](https://learn.microsoft.com/en-us/dotnet/core/deploying/native-aot/) application. Pure.DI generates plain C# object creation code, so the dependency graph remains friendly to trimming and ahead-of-time compilation.

> [!TIP]
> Native AOT works best when construction is explicit and reflection-light. Prefer generated roots and bindings over runtime service-location patterns in AOT samples.

The [composition](/samples/ShroedingersCatNativeAOT/Program.cs) uses the same object graph as the [top-level statements console sample](ConsoleTopLevelStatements.md). Pure.DI generates plain `new` chains with no reflection, so the dependency graph is trimming-safe without any extra annotations:

```c#
using System.Diagnostics;
using Pure.DI;
using static Pure.DI.Lifetime;

// Composition root
new Composition().Root.Run();
return;

// In fact, this code is never run, and the method can have any name or be a constructor, for example,
// and can be in any part of the compiled code because this is just a hint to set up an object graph.
// [Conditional("DI")] attribute avoids generating IL code for the method that follows it,
// since this method is needed only at compile time.
[Conditional("DI")]
static void Setup() =>
    DI.Setup(nameof(Composition))
        // Models a random subatomic event that may or may not occur
        .Bind().As(Singleton).To<Random>()
        // Represents a quantum superposition of 2 states: Alive or Dead
        .Bind().To((Random random) => (State)random.Next(2))
        .Bind().To<ShroedingersCat>()
        // Represents a cardboard box with any content
        .Bind().To<CardboardBox<TT>>()
        // Provides the composition root
        .Root<Program>("Root");

public interface IBox<out T>
{
    T Content { get; }
}

public interface ICat
{
    State State { get; }
}

public enum State
{
    Alive,
    Dead
}

public record CardboardBox<T>(T Content) : IBox<T>;

public class ShroedingersCat(Lazy<State> superposition) : ICat
{
    public State State => superposition.Value;

    public override string ToString() => $"{State} cat";
}

public partial class Program(IBox<ICat> box)
{
    private void Run() => Console.WriteLine(box);
}
```

> [!NOTE]
> Pure.DI works with native AOT out of the box. The generated code contains no reflection, no dynamic type resolution, and no runtime container — exactly the properties the AOT compiler requires for safe trimming. The only project-level change needed is enabling `<PublishAot>true</PublishAot>` in the `.csproj`.

The [project file](/samples/ShroedingersCatNativeAOT/ShroedingersCatNativeAOT.csproj) looks like this:

```xml
<Project Sdk="Microsoft.NET.Sdk">
    ...
    <PropertyGroup>
        <PublishAot>true</PublishAot>
        <RuntimeIdentifier>win-x64</RuntimeIdentifier>
    </PropertyGroup>
    <ItemGroup>
        <PackageReference Include="Pure.DI" Version="2.5.0">
            <PrivateAssets>all</PrivateAssets>
            <IncludeAssets>runtime; build; native; contentfiles; analyzers; buildtransitive</IncludeAssets>
        </PackageReference>
    </ItemGroup>

</Project>
```

It contains an additional reference to the NuGet package:

|         |                                                                                            |                          |
|---------|--------------------------------------------------------------------------------------------|:-------------------------|
| Pure.DI | [![NuGet](https://img.shields.io/nuget/v/Pure.DI)](https://www.nuget.org/packages/Pure.DI) | DI source code generator |

#### Schrödinger's cat console application

[![CSharp](https://img.shields.io/badge/C%23-code-blue.svg)](/samples/ShroedingersCat)

This example shows the smallest Pure.DI console application: abstractions, implementations, bindings, and the composition root are kept in one place so the generated object graph is easy to inspect. All code is in [one file](/samples/ShroedingersCat/Program.cs) for easy reading:

> [!TIP]
> The `Setup` method is a compile-time hint for the generator. It is not called at runtime, so it can stay private and contain only composition configuration.

```c#
using System.Diagnostics;
using Pure.DI;
using static Pure.DI.Lifetime;

namespace Sample;

// Let's create an abstraction

public interface IBox<out T>
{
    T Content { get; }
}

public interface ICat
{
    State State { get; }
}

public enum State
{
    Alive,
    Dead
}

// Here is our implementation

public record CardboardBox<T>(T Content) : IBox<T>;

public class ShroedingersCat(Lazy<State> superposition) : ICat
{
    // The decoherence of the superposition
    // at the time of observation via an irreversible process
    public State State => superposition.Value;

    public override string ToString() => $"{State} cat";
}

// Let's glue all together

partial class Composition
{
    // In fact, this code is never run, and the method can have any name or be a constructor, for example,
    // and can be in any part of the compiled code because this is just a hint to set up an object graph.
    // [Conditional("DI")] attribute avoids generating IL code for the method that follows it,
    // since this method is needed only at compile time.
    [Conditional("DI")]
    static void Setup() => DI.Setup()
        .Hint(Hint.Resolve, "off")
        // Models a random subatomic event that may or may not occur
        .Bind().As(Singleton).To<Random>()
        // Quantum superposition of two states: Alive or Dead
        .Bind().To((Random random) => (State)random.Next(2))
        .Bind().To<ShroedingersCat>()
        // Cardboard box with any contents
        .Bind().To<CardboardBox<TT>>()
        // Provides the composition root
        .Root<Program>("Root");
}

// Time to open boxes!

public class Program(IBox<ICat> box)
{
    // Composition Root, a single place in an application
    // where the composition of the object graphs
    // for an application take place
    public static void Main() => new Composition().Root.Run();

    private void Run() => Console.WriteLine(box);
}
```

The [project file](/samples/ShroedingersCat/ShroedingersCat.csproj) looks like this:

```xml
<Project Sdk="Microsoft.NET.Sdk">
    ...
    <ItemGroup>
        <PackageReference Include="Pure.DI" Version="2.5.0">
            <PrivateAssets>all</PrivateAssets>
            <IncludeAssets>runtime; build; native; contentfiles; analyzers; buildtransitive</IncludeAssets>
        </PackageReference>
    </ItemGroup>

</Project>
```

It contains an additional reference to the NuGet package:

|         |                                                                                            |                          |
|---------|--------------------------------------------------------------------------------------------|:-------------------------|
| Pure.DI | [![NuGet](https://img.shields.io/nuget/v/Pure.DI)](https://www.nuget.org/packages/Pure.DI) | DI source code generator |

#### Top-level statements console application

[![CSharp](https://img.shields.io/badge/C%23-code-blue.svg)](/samples/ShroedingersCatTopLevelStatements)

This example shows the same object graph as the [simple console application](ConsolePageTemplate.md), but defines the composition directly in [Program.cs](/samples/ShroedingersCatTopLevelStatements/Program.cs) with top-level statements. It keeps the setup compact while still producing the same compile-time validated composition:

> [!TIP]
> Top-level statements are convenient for small samples. For larger applications, move setup into a partial composition class so roots, lifetimes, and infrastructure bindings are easier to navigate.

```c#
using System.Diagnostics;
using Pure.DI;
using static Pure.DI.Lifetime;

// Composition root
new Composition().Root.Run();
return;

// In fact, this code is never run, and the method can have any name or be a constructor, for example,
// and can be in any part of the compiled code because this is just a hint to set up an object graph.
// [Conditional("DI")] attribute avoids generating IL code for the method that follows it,
// since this method is needed only at compile time.
[Conditional("DI")]
static void Setup() =>
    DI.Setup(nameof(Composition))
        // Models a random subatomic event that may or may not occur
        .Bind().As(Singleton).To<Random>()
        // Quantum superposition of two states: Alive or Dead
        .Bind().To((Random random) => (State)random.Next(2))
        .Bind().To<ShroedingersCat>()
        // Cardboard box with any contents
        .Bind().To<CardboardBox<TT>>()
        // Provides the composition root
        .Root<Program>("Root");

public interface IBox<out T>
{
    T Content { get; }
}

public interface ICat
{
    State State { get; }
}

public enum State
{
    Alive,
    Dead
}

public record CardboardBox<T>(T Content) : IBox<T>;

public class ShroedingersCat(Lazy<State> superposition) : ICat
{
    // The decoherence of the superposition
    // at the time of observation via an irreversible process
    public State State => superposition.Value;

    public override string ToString() => $"{State} cat";
}

public partial class Program(IBox<ICat> box)
{
    private void Run() => Console.WriteLine(box);
}
```

The [project file](/samples/ShroedingersCatTopLevelStatements/ShroedingersCatTopLevelStatements.csproj) looks like this:

```xml
<Project Sdk="Microsoft.NET.Sdk">
    ...
    <ItemGroup>
        <PackageReference Include="Pure.DI" Version="2.5.0">
            <PrivateAssets>all</PrivateAssets>
            <IncludeAssets>runtime; build; native; contentfiles; analyzers; buildtransitive</IncludeAssets>
        </PackageReference>
    </ItemGroup>

</Project>
```

It contains an additional reference to the NuGet package:

|         |                                                                                            |                          |
|---------|--------------------------------------------------------------------------------------------|:-------------------------|
| Pure.DI | [![NuGet](https://img.shields.io/nuget/v/Pure.DI)](https://www.nuget.org/packages/Pure.DI) | DI source code generator |

#### Entity Framework

[![CSharp](https://img.shields.io/badge/C%23-code-blue.svg)](/samples/EF)

This example shows how to combine Pure.DI with Entity Framework services supplied by Microsoft dependency injection. Pure.DI builds the application graph, while the external service provider supplies the `DbContext` and EF infrastructure.

> [!TIP]
> `PersonsDbContext` is intentionally not bound in Pure.DI. It is requested from the external `ServiceProvider`, while Pure.DI owns the application root and factories such as `Func<Person>`.

The composition setup file is [Composition.cs](/samples/EF/Composition.cs):

```c#
using System.Diagnostics;
using Pure.DI;
using Pure.DI.MS;

namespace EF;

partial class Composition : ServiceProviderFactory<Composition>
{
    [System.Diagnostics.Conditional("DI")]
    private void Setup() => DI.Setup()
        .Root<Program>(nameof(Root))

        .Bind().As(Lifetime.PerResolve).To<PersonService>()
        .Bind().As(Lifetime.Singleton).To<ContactService>();
}
```

The composition class inherits from `ServiceProviderFactory<T>`, where `T` is the composition class itself. When Pure.DI cannot resolve framework services such as `DbContext`, `ServiceProviderFactory<T>` delegates those requests to the configured Microsoft dependency injection provider.

The console application entry point is in the [Program.cs](/samples/EF/Program.cs) file:

```c#
using EF;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

var composition = new Composition
{
    ServiceProvider = new ServiceCollection()
        .AddEntityFrameworkInMemoryDatabase()
        .AddDbContext<PersonsDbContext>(options => options.UseInMemoryDatabase("Database of persons"))
        .BuildServiceProvider()
};

var root = composition.Root;
await root.RunAsync();

partial class Program(
    PersonsDbContext db,
    Func<Person> newPerson,
    Func<Contact> newContact)
{
    private async Task RunAsync()
    {
        var nik = newPerson() with
        {
            Name = "Nik",
            Contacts =
            [
                newContact() with { PhoneNumber = "+123456789" }
            ]
        };

        db.Persons.Add(nik);

        var john = newPerson() with
        {
            Name = "John",
            Contacts =
            [
                newContact() with { PhoneNumber = "+777333444" },
                newContact() with { PhoneNumber = "+999888666" }
            ]
        };

        db.Persons.Add(john);

        await db.SaveChangesAsync();
        await db.Persons.ForEachAsync(Console.WriteLine);
    }
}
```

The external service provider should be configured before resolving `composition.Root`. If a required EF service is missing, Pure.DI will fail when the root is created instead of hiding the missing registration.

The [project file](/samples/EF/EF.csproj) looks like this:

```xml
<Project Sdk="Microsoft.NET.Sdk">
    ...
    <ItemGroup>
        <PackageReference Include="Pure.DI" Version="2.5.0">
            <PrivateAssets>all</PrivateAssets>
            <IncludeAssets>runtime; build; native; contentfiles; analyzers; buildtransitive</IncludeAssets>
        </PackageReference>
        <PackageReference Include="Pure.DI.MS" Version="2.5.0" />
    </ItemGroup>

</Project>
```

It contains additional references to NuGet packages:

|            |                                                                                                  |                                               |
|------------|--------------------------------------------------------------------------------------------------|:----------------------------------------------|
| Pure.DI    | [![NuGet](https://img.shields.io/nuget/v/Pure.DI)](https://www.nuget.org/packages/Pure.DI)       | DI source code generator                      |
| Pure.DI.MS | [![NuGet](https://img.shields.io/nuget/v/Pure.DI.MS)](https://www.nuget.org/packages/Pure.DI.MS) | Add-ons for Pure.DI to work with Microsoft DI |

#### gRPC service

[![CSharp](https://img.shields.io/badge/C%23-code-blue.svg)](/samples/GrpcService)

This example shows how to build a gRPC service with Pure.DI and ASP.NET Core hosting. The generated composition exposes the service and application dependencies through the Microsoft service-provider pipeline.

> [!TIP]
> The gRPC service type itself is a composition root. Keep the ASP.NET Core gRPC registration (`AddGrpc`) in `Program.cs`, and let Pure.DI create the service graph.

The composition setup file is [Composition.cs](/samples/GrpcService/Composition.cs):

```c#
using Pure.DI;
using Pure.DI.MS;
using static Pure.DI.Lifetime;

namespace GrpcService;

partial class Composition : ServiceProviderFactory<Composition>
{
    // IMPORTANT:
    // Only composition roots (regular or anonymous) can be resolved through the `IServiceProvider` interface.
    // These roots must be registered using `Root<>(...)` or `Roots<>()` calls.
    [System.Diagnostics.Conditional("DI")]
    private static void Setup() => DI.Setup()
        .Root<ClockService>()

        .Bind().As(Singleton).To<ClockViewModel>()
        .Bind().To<ClockModel>()
        .Bind().As(Singleton).To<Ticks>()

        // Infrastructure
        .Bind().To<MicrosoftLoggerAdapter<TT>>()
        .Bind().To<CurrentThreadDispatcher>();
}
```

The composition class inherits from `ServiceProviderFactory<T>`, where `T` is the composition class itself. Only registered roots can be resolved through the Microsoft `IServiceProvider`, so the gRPC service type is declared as a root.

The web application entry point is in the [Program.cs](/samples/GrpcService/Program.cs) file:

```c#
var builder = WebApplication.CreateBuilder(args);
builder.Services.AddGrpc();

using var composition = new Composition();

// Uses Composition as an alternative IServiceProviderFactory
builder.Host.UseServiceProviderFactory(composition);
...
```

The [project file](/samples/GrpcService/GrpcService.csproj) looks like this:

```xml
<Project Sdk="Microsoft.NET.Sdk.Web">
    ...
    <ItemGroup>
        <PackageReference Include="Pure.DI" Version="2.5.0">
            <PrivateAssets>all</PrivateAssets>
            <IncludeAssets>runtime; build; native; contentfiles; analyzers; buildtransitive</IncludeAssets>
        </PackageReference>
        <PackageReference Include="Pure.DI.MS" Version="2.5.0" />
    </ItemGroup>

</Project>
```

It contains additional references to NuGet packages:

|            |                                                                                                  |                                               |
|------------|--------------------------------------------------------------------------------------------------|:----------------------------------------------|
| Pure.DI    | [![NuGet](https://img.shields.io/nuget/v/Pure.DI)](https://www.nuget.org/packages/Pure.DI)       | DI source code generator                      |
| Pure.DI.MS | [![NuGet](https://img.shields.io/nuget/v/Pure.DI.MS)](https://www.nuget.org/packages/Pure.DI.MS) | Add-ons for Pure.DI to work with Microsoft DI |

#### MAUI application

[![CSharp](https://img.shields.io/badge/C%23-code-blue.svg)](/samples/MAUIApp)

This example shows how to build a [MAUI application](https://learn.microsoft.com/en-us/dotnet/maui/what-is-maui) with Pure.DI, sharing generated view-model roots with XAML resources and the MAUI container.

> [!TIP]
> The sample creates one `Composition` in `MauiProgram`, installs it with `ConfigureContainer`, and also places the same initialized instance into application resources for XAML bindings.

The definition of the composition is in [Composition.cs](/samples/MAUIApp/Composition.cs). Do not forget to define any necessary composition roots, for example, these can be view models such as _ClockViewModel_:

```c#
using Pure.DI;
using Pure.DI.MS;
using static Pure.DI.Lifetime;

namespace MAUIApp;

partial class Composition : ServiceProviderFactory<Composition>
{
    [System.Diagnostics.Conditional("DI")]
    private void Setup() => DI.Setup()
        .Root<IAppViewModel>(nameof(App))
        .Root<IClockViewModel>(nameof(Clock))

        .Bind().As(Singleton).To<ClockViewModel>()
        .Bind().To<ClockModel>()
        .Bind().As(Singleton).To<Ticks>()

        // Infrastructure
        .Bind().To<MicrosoftLoggerAdapter<TT>>()
        .Bind().To<MauiDispatcher>();
}
```

Remember to dispose the composition on platform shutdown events. The sample handles Windows and Android explicitly; add equivalent lifecycle hooks for other target platforms when they own disposable services.

The composition class inherits from `ServiceProviderFactory<T>`, where `T` is the composition class itself. Only registered roots can be resolved through the Microsoft `IServiceProvider`, so view models shared with XAML are declared as named roots.

The application entry point is in the [MauiProgram.cs](/samples/MAUIApp/MauiProgram.cs) file:

```c#
namespace MAUIApp;

public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        var builder = MauiApp.CreateBuilder();
        var composition = new Composition();
        
        // Uses Composition as an alternative IServiceProviderFactory
        builder.ConfigureContainer(composition);
        
        builder
            .UseMauiApp(_ => new App
            {
                // Overrides the resource with an initialized Composition instance
                Resources = { ["Composition"] = composition }
            })
            .ConfigureLifecycleEvents(events =>
            {
                // Handles disposables
#if WINDOWS
                events.AddWindows(windows => windows
                    .OnClosed((_, _) => composition.Dispose()));
#endif
#if ANDROID
                events.AddAndroid(android => android
                    .OnStop(_ => composition.Dispose()));
#endif
            })
            .ConfigureFonts(fonts =>
            {
                fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
            });

#if DEBUG
        builder.Logging.AddDebug();
#endif

        return builder.Build();
    }
}
```

A single instance of the _Composition_ class is defined as a static resource in [App.xaml](/samples/MAUIApp/App.xaml) for later use within the _XAML_ markup everywhere:

```xaml
<?xml version="1.0" encoding="UTF-8"?>

<Application xmlns="http://schemas.microsoft.com/dotnet/2021/maui"
             xmlns:x="http://schemas.microsoft.com/winfx/2009/xaml"
             xmlns:local="clr-namespace:MAUIApp"
             x:Class="MAUIApp.App">
    <Application.Resources>
        <ResourceDictionary>
            <ResourceDictionary.MergedDictionaries>
                <ResourceDictionary>
                    <local:Composition x:Key="Composition" />
                </ResourceDictionary>
                <ResourceDictionary Source="Resources/Styles/Colors.xaml" />
                <ResourceDictionary Source="Resources/Styles/Styles.xaml" />
            </ResourceDictionary.MergedDictionaries>
        </ResourceDictionary>
    </Application.Resources>
</Application>
```

All previously defined composition roots are now accessible from [markup](/samples/MAUIApp/MainPage.xaml) without any effort:

```xaml
<?xml version="1.0" encoding="utf-8"?>

<ContentPage xmlns="http://schemas.microsoft.com/dotnet/2021/maui"
             xmlns:x="http://schemas.microsoft.com/winfx/2009/xaml"
             x:Class="MAUIApp.MainPage"
             xmlns:local="clr-namespace:MAUIApp"
             xmlns:clock="clr-namespace:Clock;assembly=Clock"
             BindingContext="{Binding Source={StaticResource Composition}, x:DataType=local:Composition, Path=Clock}"
             x:DataType="clock:IClockViewModel">

    <ScrollView>

        <VerticalStackLayout
            Spacing="25"
            Padding="30,0"
            VerticalOptions="Center">

            <Label
                Text="{Binding Date}"
                SemanticProperties.HeadingLevel="Level1"
                FontSize="64"
                HorizontalOptions="Center" />

            <Label
                Text="{Binding Time}"
                SemanticProperties.HeadingLevel="Level2"
                FontSize="128"
                HorizontalOptions="Center" />

        </VerticalStackLayout>
    </ScrollView>

</ContentPage>
```

The [project file](/samples/MAUIApp/MAUIApp.csproj) looks like this:

```xml
<Project Sdk="Microsoft.NET.Sdk">
    ...
    <ItemGroup>
        <PackageReference Include="Pure.DI" Version="2.5.0">
            <PrivateAssets>all</PrivateAssets>
            <IncludeAssets>runtime; build; native; contentfiles; analyzers; buildtransitive</IncludeAssets>
        </PackageReference>
        <PackageReference Include="Pure.DI.MS" Version="2.5.0" />
    </ItemGroup>

</Project>
```

It contains additional references to NuGet packages:

|            |                                                                                                  |                                               |
|------------|--------------------------------------------------------------------------------------------------|:----------------------------------------------|
| Pure.DI    | [![NuGet](https://img.shields.io/nuget/v/Pure.DI)](https://www.nuget.org/packages/Pure.DI)       | DI source code generator                      |
| Pure.DI.MS | [![NuGet](https://img.shields.io/nuget/v/Pure.DI.MS)](https://www.nuget.org/packages/Pure.DI.MS) | Add-ons for Pure.DI to work with Microsoft DI |


#### Minimal Web API

[![CSharp](https://img.shields.io/badge/C%23-code-blue.svg)](/samples/MinimalWebAPI)

This example shows how to build a Minimal Web API with Pure.DI, using generated roots for the application entry point while still allowing ASP.NET Core endpoint handlers to request services from Microsoft dependency injection.

> [!TIP]
> `Owned<Program>` is used as the application root so disposable dependencies created for that root are released deterministically. Endpoint handlers may still use `[FromServices]` for services supplied by ASP.NET Core.

The composition setup file is [Composition.cs](/samples/MinimalWebAPI/Composition.cs):

```c#
using Pure.DI;
using Pure.DI.MS;
using static Pure.DI.Lifetime;

namespace MinimalWebAPI;

partial class Composition : ServiceProviderFactory<Composition>
{
    // IMPORTANT:
    // Only composition roots (regular or anonymous) can be resolved through the `IServiceProvider` interface.
    // These roots must be registered using `Root<>(...)` or `Roots<>()` calls.
    [System.Diagnostics.Conditional("DI")]
    private void Setup() => DI.Setup()
        // Owned is used here to dispose of all disposable instances associated with the root.
        .Root<Owned<Program>>(nameof(Root))
        .Root<IClockViewModel>()

        .Bind().To<ClockViewModel>()
        .Bind().To<ClockModel>()
        .Bind().As(Singleton).To<Ticks>()

        // Infrastructure
        .Bind().To<MicrosoftLoggerAdapter<TT>>()
        .Bind().To<CurrentThreadDispatcher>();
}
```

The composition class inherits from `ServiceProviderFactory<T>`, where `T` is the composition class itself. Only registered roots can be resolved through the Microsoft `IServiceProvider`; this sample also resolves the `Owned<Program>` root directly from the composition.

The web application entry point is in the [Program.cs](/samples/MinimalWebAPI/Program.cs) file:

```c#
using MinimalWebAPI;

using var composition = new Composition();
var builder = WebApplication.CreateBuilder(args);

// Uses Composition as an alternative IServiceProviderFactory
builder.Host.UseServiceProviderFactory(composition);

var app = builder.Build();

// Creates an application composition root of type `Owned<Program>`
using var root = composition.Root;
root.Value.Run(app);

partial class Program(
    IClockViewModel clock,
    IAppViewModel appModel)
{
    private void Run(WebApplication app)
    {
        app.MapGet("/", (
            // Dependencies can be injected here as well
            [FromServices] ILogger<Program> logger) => {
            logger.LogInformation("Start of request execution");
            return new ClockResult(appModel.Title, clock.Date, clock.Time);
        });

        app.Run();
    }
}
```

The [project file](/samples/MinimalWebAPI/MinimalWebAPI.csproj) looks like this:

```xml
<Project Sdk="Microsoft.NET.Sdk.Web">
    ...
    <ItemGroup>
        <PackageReference Include="Pure.DI" Version="2.5.0">
            <PrivateAssets>all</PrivateAssets>
            <IncludeAssets>runtime; build; native; contentfiles; analyzers; buildtransitive</IncludeAssets>
        </PackageReference>
        <PackageReference Include="Pure.DI.MS" Version="2.5.0" />
    </ItemGroup>

</Project>
```

It contains additional references to NuGet packages:

|            |                                                                                                  |                                               |
|------------|--------------------------------------------------------------------------------------------------|:----------------------------------------------|
| Pure.DI    | [![NuGet](https://img.shields.io/nuget/v/Pure.DI)](https://www.nuget.org/packages/Pure.DI)       | DI source code generator                      |
| Pure.DI.MS | [![NuGet](https://img.shields.io/nuget/v/Pure.DI.MS)](https://www.nuget.org/packages/Pure.DI.MS) | Add-ons for Pure.DI to work with Microsoft DI |

#### Unity

[![CSharp](https://img.shields.io/badge/C%23-code-blue.svg)](/samples/UnityApp)

This example shows how to use Pure.DI in a [Unity](https://unity.com/) application, including object graph generation for regular services and build-up support for `MonoBehaviour` instances managed by the engine.

> [!TIP]
> Unity creates `MonoBehaviour` instances itself, so Pure.DI uses `BuildUp` methods for scene objects and regular roots for services. The sample also shows parent/child scene scopes with scoped services.

![Unity](https://cdn.sanity.io/images/fuvbjjlp/production/01c082f3046cc45548249c31406aeffd0a9a738e-296x100.png)

The definition of the composition is in [Scope.cs](/samples/UnityApp/Assets/Scripts/Scope.cs). This class sets up how the object graphs will be created for the application. Remember to define builders for types derived from `MonoBehaviour`:

```c#
internal class ClocksComposition
{
    [SerializeField] private ClockConfig clockConfig;

    void Setup() => DI.Setup(kind: CompositionKind.Internal)
        .Transient(() => clockConfig)
        .Singleton<ClockService>();
}

public partial class Scope : MonoBehaviour
{
    [SerializeField] private Scope parentScope;

    private bool isReady;

    void Setup() => DI.Setup()
        .Hint(Hint.ScopeMethodName, "SetupScope")
        .DependsOn(nameof(ClocksComposition), SetupContextKind.Members)
        .Bind<IClockSession>().As(Lifetime.Scoped).To<ClockSession>()
        .Root<ClockManager>(nameof(ClockManager))
        .Builders<MonoBehaviour>();

    public void EnsureReady()
    {
        if (isReady)
        {
            return;
        }

        if (parentScope != null && !ReferenceEquals(parentScope, this))
        {
            parentScope.EnsureReady();
            SetupScope(parentScope, this);
        }

        isReady = true;
    }

    void Awake()
    {
        EnsureReady();
    }

    void Start()
    {
        EnsureReady();
        ClockManager.Start();
    }

    void OnDestroy()
    {
        Dispose();
    }
}
```

Use `EnsureReady()` before resolving or building scene objects. It initializes the parent scope first and prevents accidental self-parenting from creating recursive scope setup.

For types derived from `MonoBehaviour`, a `BuildUp` composition method will be generated. This method looks like:

```c#
private ClockService _singletonClockService;
[SerializeField] private ClockConfig clockConfig;
    
public global::Clock BuildUp(global::Clock buildingInstance)
{
    if (buildingInstance is null) throw new global::System.ArgumentNullException(nameof(buildingInstance));
    
    if (_singletonClockService is null)
    {
        _singletonClockService = new global::ClockService(clockConfig);
        _disposables45d[_disposeIndex45d++] = _singletonClockService;
    }
    
    buildingInstance.ClockService = _singletonClockService;
    return transientClock;
}
```

A single instance of the _Composition_ class is defined as a public property `Composition.Shared`. It provides a common composition for building classes based on `MonoBehaviour`.

An [example](/samples/UnityApp/Assets/Scripts/Clock.cs) of such a class might look like this:

```c#
using Pure.DI;
using UnityEngine;

public class Clock : MonoBehaviour
{
    const float HoursToDegrees = -30f, MinutesToDegrees = -6f, SecondsToDegrees = -6f;
    [SerializeField] Scope scope;
    [SerializeField] Transform hoursPivot;
    [SerializeField] Transform minutesPivot;
    [SerializeField] Transform secondsPivot;

    [Dependency]
    public IClockService ClockService { private get; set; }

    void Awake()
    {
        scope.BuildUp(this);
    }

    void Update()
    {
        var now = ClockService.Now.TimeOfDay;
        hoursPivot.localRotation = Quaternion.Euler(0f, 0f, HoursToDegrees * (float)now.TotalHours);
        minutesPivot.localRotation = Quaternion.Euler(0f, 0f, MinutesToDegrees * (float)now.TotalMinutes);
        secondsPivot.localRotation = Quaternion.Euler(0f, 0f, SecondsToDegrees * (float)now.TotalSeconds);
    }
}
```

The Unity project should reference the NuGet package:

|         |                                                                                            |                          |
|---------|--------------------------------------------------------------------------------------------|:-------------------------|
| Pure.DI | [![NuGet](https://img.shields.io/nuget/v/Pure.DI)](https://www.nuget.org/packages/Pure.DI) | DI source code generator |

The Unity example uses the Unity editor version 6000.0.35f1

#### Uno Platform application

[![CSharp](https://img.shields.io/badge/C%23-code-blue.svg)](/samples/UnoApp)

This example shows how to build a cross-platform [Uno Platform](https://platform.uno/) application with Pure.DI. One C# and XAML project targets Windows, macOS, and Linux through the Skia Desktop runtime, while Pure.DI generates the view-model and infrastructure object graphs at compile time.

> [!TIP]
> The sample uses the minimal Uno Platform Blank preset and explicit Pure.DI roots. It does not add a runtime DI container, and `Hint.Resolve` is disabled.

The composition is defined in [Composition.cs](/samples/UnoApp/Composition.cs). Its virtual roots are available to XAML and can be replaced by the design-time composition:

```c#
using Pure.DI;
using static Pure.DI.Lifetime;
using static Pure.DI.RootKinds;

namespace UnoApp;

public partial class Composition
{
    [Conditional("DI")]
    private void Setup() => DI.Setup()
        .Hint(Hint.Resolve, "Off")

        .Root<IAppViewModel>(nameof(App), kind: Virtual)
        .Root<IClockViewModel>(nameof(Clock), kind: Virtual)

        .Bind().As(Singleton).To<ClockViewModel>()
        .Bind().To<ClockModel>()
        .Bind().As(Singleton).To<Ticks>()

        // Infrastructure
        .Bind().To<DebugLog<TT>>()
        .Bind().To<UnoDispatcher>();
}
```

[DesignTimeComposition.cs](/samples/UnoApp/DesignTimeComposition.cs) overrides the same roots with predictable data for XAML tooling:

```c#
using Pure.DI;
using static Pure.DI.RootKinds;

namespace UnoApp;

public partial class DesignTimeComposition : Composition
{
    [Conditional("DI")]
    private void Setup() => DI.Setup()
        .Hint(Hint.Resolve, "Off")

        .Root<IAppViewModel>(nameof(App), kind: Override)
        .Root<IClockViewModel>(nameof(Clock), kind: Override)

        .Bind().To<DesignTimeAppViewModel>()
        .Bind().To<DesignTimeClockViewModel>();
}
```

A shared `Composition` is declared in [App.xaml](/samples/UnoApp/App.xaml), just like any other XAML resource:

```xml
<Application x:Class="UnoApp.App"
             xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
             xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
             xmlns:app="using:UnoApp">
    <Application.Resources>
        <ResourceDictionary>
            <ResourceDictionary.MergedDictionaries>
                <XamlControlsResources xmlns="using:Microsoft.UI.Xaml.Controls" />
            </ResourceDictionary.MergedDictionaries>

            <app:Composition x:Key="Composition" />
        </ResourceDictionary>
    </Application.Resources>
</Application>
```

The application retrieves that resource when it creates the main window and disposes it when the window closes. This releases disposable singleton and scoped dependencies owned by Pure.DI:

```c#
private Window? MainWindow { get; set; }

protected override void OnLaunched(LaunchActivatedEventArgs args)
{
    if (Resources[nameof(Composition)] is not Composition composition)
    {
        throw new InvalidOperationException("The Pure.DI composition resource was not found.");
    }

    var window = MainWindow = new Window
    {
        Content = new MainPage()
    };

    var appViewModel = composition.App;
    window.Title = appViewModel.Title;

    PropertyChangedEventHandler? titleChanged = null;
    if (appViewModel is INotifyPropertyChanged propertyChanged)
    {
        titleChanged = (_, eventArgs) =>
        {
            if (eventArgs.PropertyName is null or nameof(IAppViewModel.Title))
            {
                window.Title = appViewModel.Title;
            }
        };

        propertyChanged.PropertyChanged += titleChanged;
    }

    window.Closed += (_, _) =>
    {
        if (appViewModel is INotifyPropertyChanged propertyChanged && titleChanged is not null)
        {
            propertyChanged.PropertyChanged -= titleChanged;
        }

        composition.Dispose();
    };

    window.Activate();
}
```

[MainPage.xaml](/samples/UnoApp/MainPage.xaml) uses the composition as its data context. The generated `App` and `Clock` properties are ordinary binding paths:

```xml
<Page x:Class="UnoApp.MainPage"
      xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
      xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
      xmlns:d="http://schemas.microsoft.com/expression/blend/2008"
      xmlns:mc="http://schemas.openxmlformats.org/markup-compatibility/2006"
      xmlns:app="using:UnoApp"
      mc:Ignorable="d"
      DataContext="{StaticResource Composition}"
      d:DataContext="{d:DesignInstance Type=app:DesignTimeComposition, IsDesignTimeCreatable=True}"
      FontFamily="Consolas"
      FontWeight="Bold">
    <StackPanel HorizontalAlignment="Center"
                VerticalAlignment="Center"
                DataContext="{Binding Clock}">
        <TextBlock Text="{Binding Date}" FontSize="64" HorizontalAlignment="Center" />
        <TextBlock Text="{Binding Time}" FontSize="128" HorizontalAlignment="Center" />
    </StackPanel>
</Page>
```

The [project file](/samples/UnoApp/UnoApp.csproj) uses the current single-project Uno SDK and adds Pure.DI as a source generator:

```xml
<Project Sdk="Uno.Sdk/6.5.36">
    <PropertyGroup>
        <TargetFrameworks>net10.0-desktop</TargetFrameworks>
        <UnoSingleProject>true</UnoSingleProject>
        <UnoFeatures>SkiaRenderer;</UnoFeatures>
    </PropertyGroup>

    <ItemGroup>
        <PackageReference Include="Pure.DI" Version="2.5.1">
            <PrivateAssets>all</PrivateAssets>
            <IncludeAssets>runtime; build; native; contentfiles; analyzers; buildtransitive</IncludeAssets>
        </PackageReference>
    </ItemGroup>
</Project>
```

|              |                                                                                          |                                      |
|--------------|------------------------------------------------------------------------------------------|:-------------------------------------|
| Pure.DI      | [![NuGet](https://img.shields.io/nuget/v/Pure.DI)](https://www.nuget.org/packages/Pure.DI) | DI source code generator             |
| Uno Platform | [Documentation](https://platform.uno/docs/)                                               | Cross-platform WinUI-compatible UI   |

#### Web API

[![CSharp](https://img.shields.io/badge/C%23-code-blue.svg)](/samples/WebAPI)

This example shows how to build an ASP.NET Core Web API with Pure.DI, registering controllers as generated roots while keeping compatibility with the Microsoft service-provider pipeline.

> [!TIP]
> Controller activation goes through `IServiceProvider`, so controllers have to be visible as composition roots. Pair `.Roots<ControllerBase>()` with `AddControllersAsServices()`.

The composition setup file is [Composition.cs](/samples/WebAPI/Composition.cs):

```c#
using Pure.DI;
using Pure.DI.MS;
using static Pure.DI.Lifetime;

namespace WebAPI;

partial class Composition : ServiceProviderFactory<Composition>
{
    // IMPORTANT:
    // Only composition roots (regular or anonymous) can be resolved through the `IServiceProvider` interface.
    // These roots must be registered using `Root<>(...)` or `Roots<>()` calls.
    [System.Diagnostics.Conditional("DI")]
    private static void Setup() => DI.Setup()
        .Roots<ControllerBase>()

        .Bind().As(Singleton).To<ClockViewModel>()
        .Bind().To<ClockModel>()
        .Bind().As(Singleton).To<Ticks>()

        // Infrastructure
        .Bind().To<MicrosoftLoggerAdapter<TT>>()
        .Bind().To<CurrentThreadDispatcher>();
}
```

The composition class inherits from `ServiceProviderFactory<T>`, where `T` is the composition class itself. Only registered roots can be resolved through the Microsoft `IServiceProvider`, which is why controllers are registered with `.Roots<ControllerBase>()`.

The web application entry point is in the [Program.cs](/samples/WebAPI/Program.cs) file:

```c#
var builder = WebApplication.CreateBuilder(args);

using var composition = new Composition();

// Uses Composition as an alternative IServiceProviderFactory
builder.Host.UseServiceProviderFactory(composition);

// It is required for controllers to be registered as regular services.
builder.Services.AddMvc().AddControllersAsServices();
```

The [project file](/samples/WebAPI/WebAPI.csproj) looks like this:

```xml
<Project Sdk="Microsoft.NET.Sdk.Web">
    ...
    <ItemGroup>
        <PackageReference Include="Pure.DI" Version="2.5.0">
            <PrivateAssets>all</PrivateAssets>
            <IncludeAssets>runtime; build; native; contentfiles; analyzers; buildtransitive</IncludeAssets>
        </PackageReference>
        <PackageReference Include="Pure.DI.MS" Version="2.5.0" />
    </ItemGroup>

</Project>
```

It contains additional references to NuGet packages:

|            |                                                                                                  |                                               |
|------------|--------------------------------------------------------------------------------------------------|:----------------------------------------------|
| Pure.DI    | [![NuGet](https://img.shields.io/nuget/v/Pure.DI)](https://www.nuget.org/packages/Pure.DI)       | DI source code generator                      |
| Pure.DI.MS | [![NuGet](https://img.shields.io/nuget/v/Pure.DI.MS)](https://www.nuget.org/packages/Pure.DI.MS) | Add-ons for Pure.DI to work with Microsoft DI |

#### Web application

[![CSharp](https://img.shields.io/badge/C%23-code-blue.svg)](/samples/WebApp)

This example shows how to build an ASP.NET Core MVC web application with Pure.DI, registering controllers as generated roots while keeping compatibility with the Microsoft service-provider pipeline.

> [!TIP]
> Controllers must be registered both as ASP.NET Core services and as Pure.DI roots. The sample uses `.Roots<ControllerBase>()` in the composition and `AddControllersAsServices()` in `Program.cs`.

The composition setup file is [Composition.cs](/samples/WebApp/Composition.cs):

```c#
using Pure.DI;
using Pure.DI.MS;
using static Pure.DI.Lifetime;

namespace WebApp;

partial class Composition : ServiceProviderFactory<Composition>
{
    // IMPORTANT:
    // Only composition roots (regular or anonymous) can be resolved through the `IServiceProvider` interface.
    // These roots must be registered using `Root<>(...)` or `Roots<>()` calls.
    [System.Diagnostics.Conditional("DI")]
    private static void Setup() => DI.Setup()
        .Roots<ControllerBase>()

        .Bind().As(Singleton).To<ClockViewModel>()
        .Bind().To<ClockModel>()
        .Bind().As(Singleton).To<Ticks>()

        // Infrastructure
        .Bind().To<MicrosoftLoggerAdapter<TT>>()
        .Bind().To<CurrentThreadDispatcher>();
}
```

The composition class inherits from `ServiceProviderFactory<T>`, where `T` is the composition class itself. Only registered roots can be resolved through the Microsoft `IServiceProvider`, which is why controllers are registered with `.Roots<ControllerBase>()`.

The web application entry point is in the [Program.cs](/samples/WebApp/Program.cs) file:

```c#
var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews().AddControllersAsServices();

using var composition = new Composition();

// Uses Composition as an alternative IServiceProviderFactory
builder.Host.UseServiceProviderFactory(composition);
```

The [project file](/samples/WebApp/WebApp.csproj) looks like this:

```xml
<Project Sdk="Microsoft.NET.Sdk.Web">
    ...
    <ItemGroup>
        <PackageReference Include="Pure.DI" Version="2.5.0">
            <PrivateAssets>all</PrivateAssets>
            <IncludeAssets>runtime; build; native; contentfiles; analyzers; buildtransitive</IncludeAssets>
        </PackageReference>
        <PackageReference Include="Pure.DI.MS" Version="2.5.0" />
    </ItemGroup>

</Project>
```

It contains additional references to NuGet packages:

|            |                                                                                                  |                                               |
|------------|--------------------------------------------------------------------------------------------------|:----------------------------------------------|
| Pure.DI    | [![NuGet](https://img.shields.io/nuget/v/Pure.DI)](https://www.nuget.org/packages/Pure.DI)       | DI source code generator                      |
| Pure.DI.MS | [![NuGet](https://img.shields.io/nuget/v/Pure.DI.MS)](https://www.nuget.org/packages/Pure.DI.MS) | Add-ons for Pure.DI to work with Microsoft DI |

#### WinForms application

[![CSharp](https://img.shields.io/badge/C%23-code-blue.svg)](/samples/WinFormsAppNetCore)

This example shows how to build a modern .NET WinForms application with Pure.DI. The generated composition creates the main form, view models, and infrastructure services without a runtime container.

> [!TIP]
> The main form is created through the `Program` composition root. `Hint.Resolve` is disabled because the application uses explicit roots only.

The composition definition is in the file [Composition.cs](/samples/WinFormsAppNetCore/Composition.cs). Remember to define all the necessary composition roots, for example, this could be a main form such as _FormMain_:

```c#
using Pure.DI;
using static Pure.DI.Lifetime;

namespace WinFormsAppNetCore;

partial class Composition
{
    [System.Diagnostics.Conditional("DI")]
    private void Setup() => DI.Setup()
        .Hint(Hint.Resolve, "Off")

        .Root<Program>(nameof(Root))

        .Bind().As(Singleton).To<FormMain>()
        .Bind().As(Singleton).To<ClockViewModel>()
        .Bind().To<ClockModel>()
        .Bind().As(Singleton).To<Ticks>()

        // Infrastructure
        .Bind().To<DebugLog<TT>>()
        .Bind().To<WinFormsDispatcher>();
}
```

Keep the composition in a `using` block around the WinForms message loop so Pure.DI-owned disposable services are released after the main form exits.

A single instance of the _Composition_ class is defined in the _Main_ method of the [Program.cs](/samples/WinFormsAppNetCore/Program.cs) file:

```c#
namespace WinFormsAppNetCore;

public class Program(FormMain formMain)
{
    [STAThread]
    public static void Main()
    {
        ApplicationConfiguration.Initialize();
        using var composition = new Composition();
        composition.Root.Run();
    }

    private void Run() => Application.Run(formMain);
}
```

The [project file](/samples/WinFormsAppNetCore/WinFormsAppNetCore.csproj) looks like this:

```xml
<Project Sdk="Microsoft.NET.Sdk">
    ...
    <ItemGroup>
        <PackageReference Include="Pure.DI" Version="2.5.0">
            <PrivateAssets>all</PrivateAssets>
            <IncludeAssets>runtime; build; native; contentfiles; analyzers; buildtransitive</IncludeAssets>
        </PackageReference>
    </ItemGroup>

</Project>
```

It contains an additional reference to the NuGet package:

|         |                                                                                            |                          |
|---------|--------------------------------------------------------------------------------------------|:-------------------------|
| Pure.DI | [![NuGet](https://img.shields.io/nuget/v/Pure.DI)](https://www.nuget.org/packages/Pure.DI) | DI source code generator |

#### WinForms application

[![CSharp](https://img.shields.io/badge/C%23-code-blue.svg)](/samples/WinFormsApp)

This example shows how to build a classic WinForms application with Pure.DI. The generated composition creates the main form, view models, and infrastructure services without a runtime container.

> [!TIP]
> The main form is created through the `Program` composition root. `Hint.Resolve` is disabled because the application uses explicit roots only.

The composition definition is in the file [Composition.cs](/samples/WinFormsApp/Composition.cs). Remember to define all the necessary composition roots, for example, this could be a main form such as _FormMain_:

```c#
using Pure.DI;
using static Pure.DI.Lifetime;

namespace WinFormsApp;

partial class Composition
{
    [System.Diagnostics.Conditional("DI")]
    private void Setup() => DI.Setup()
        .Hint(Hint.Resolve, "Off")

        .Root<Program>(nameof(Root))

        .Bind().As(Singleton).To<FormMain>()
        .Bind().As(Singleton).To<ClockViewModel>()
        .Bind().To<ClockModel>()
        .Bind().As(Singleton).To<Ticks>()

        // Infrastructure
        .Bind().To<DebugLog<TT>>()
        .Bind().To<WinFormsDispatcher>();
}
```

Keep the composition in a `using` block around the WinForms message loop so Pure.DI-owned disposable services are released after the main form exits.

A single instance of the _Composition_ class is defined in the _Main_ method of the [Program.cs](/samples/WinFormsApp/Program.cs) file:

```c#
namespace WinFormsApp;

public class Program(FormMain formMain)
{
    [STAThread]
    public static void Main()
    {
        Application.EnableVisualStyles();
        Application.SetCompatibleTextRenderingDefault(false);
        using var composition = new Composition();
        var root = composition.Root;
        root.Run();
    }

    private void Run() => Application.Run(formMain);
}
```

The [project file](/samples/WinFormsApp/WinFormsApp.csproj) looks like this:

```xml
<?xml version="1.0" encoding="utf-8"?>
<Project ToolsVersion="15.0" xmlns="http://schemas.microsoft.com/developer/msbuild/2003">
    ...
    <ItemGroup>
        <PackageReference Include="Pure.DI" Version="2.5.0">
            <PrivateAssets>all</PrivateAssets>
            <IncludeAssets>runtime; build; native; contentfiles; analyzers; buildtransitive</IncludeAssets>
        </PackageReference>
    </ItemGroup>

</Project>
```

It contains an additional reference to the NuGet package:

|         |                                                                                            |                          |
|---------|--------------------------------------------------------------------------------------------|:-------------------------|
| Pure.DI | [![NuGet](https://img.shields.io/nuget/v/Pure.DI)](https://www.nuget.org/packages/Pure.DI) | DI source code generator |

#### WPF application

[![CSharp](https://img.shields.io/badge/C%23-code-blue.svg)](/samples/WpfAppNetCore)

This example shows how to build a WPF application with Pure.DI, exposing generated view-model roots through XAML resources while keeping object graph validation at compile time.

> [!TIP]
> `Hint.Resolve` is disabled because XAML uses named composition roots (`App`, `Clock`) directly. This keeps the generated API small and avoids service-locator-style access.

The definition of the composition is in [Composition.cs](/samples/WpfAppNetCore/Composition.cs). This class sets up how the object graphs will be created for the application. Do not forget to define any necessary composition roots, for example, these can be view models such as _ClockViewModel_:

```c#
using Pure.DI;
using static Pure.DI.Lifetime;
using static Pure.DI.RootKinds;

namespace WpfAppNetCore;

partial class Composition
{
    [System.Diagnostics.Conditional("DI")]
    private void Setup() => DI.Setup()
        .Hint(Hint.Resolve, "Off")

        .Root<IAppViewModel>(nameof(App), kind: Virtual)
        .Root<IClockViewModel>(nameof(Clock), kind: Virtual)

        .Bind().As(Singleton).To<ClockViewModel>()
        .Bind().To<ClockModel>()
        .Bind().As(Singleton).To<Ticks>()

        // Infrastructure
        .Bind().To<DebugLog<TT>>()
        .Bind().To<WpfDispatcher>();
}
```

A design-time composition can override the same virtual roots with predictable view models for the WPF designer. The design-time setup is in [DesignTimeComposition.cs](/samples/WpfAppNetCore/DesignTimeComposition.cs):

```c#
using Pure.DI;
using static Pure.DI.RootKinds;

namespace WpfAppNetCore;

partial class DesignTimeComposition: Composition
{
    [System.Diagnostics.Conditional("DI")]
    private void Setup() => DI.Setup()
        .Hint(Hint.Resolve, "Off")

        // Overrides virtual roots with design-time view models
        .Root<IAppViewModel>(nameof(App), kind: Override)
        .Root<IClockViewModel>(nameof(Clock), kind: Override)

        .Bind().To<DesignTimeAppViewModel>()
        .Bind().To<DesignTimeClockViewModel>();
}
```

A single instance of the _Composition_ class is defined as a static resource in [App.xaml](/samples/WpfAppNetCore/App.xaml) for later use within the _XAML_ markup everywhere:

```xaml
<Application x:Class="WpfAppNetCore.App" x:ClassModifier="internal"
             xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
             xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
             xmlns:app="clr-namespace:WpfAppNetCore"
             StartupUri="/MainWindow.xaml"
             Exit="OnExit">

    <Application.Resources>
        <!--Creates a shared resource of type Composition and with key "Composition",
        which will be further used as a data context in the views.-->
        <app:Composition x:Key="Composition" />
    </Application.Resources>

</Application>
```

This markup fragment

```xml
<Application.Resources>
    <app:Composition x:Key="Composition" />
</Application.Resources>
```

creates a shared resource of type `Composition` and with key _"Composition"_, which will be further used as a data context in the views.

Dispose the shared composition from the WPF `Exit` event when the application closes, especially if singleton services implement `IDisposable`.

You can now use bindings to model views without even editing the views `.cs` code files. All previously defined composition roots are now accessible from [markup](/samples/WpfAppNetCore/MainWindow.xaml) without any effort, such as _ClockViewModel_:

```xaml
<Window xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
        xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
        xmlns:d="http://schemas.microsoft.com/expression/blend/2008"
        xmlns:mc="http://schemas.openxmlformats.org/markup-compatibility/2006"
        xmlns:app="clr-namespace:WpfAppNetCore"
        mc:Ignorable="d"
        DataContext="{StaticResource Composition}"
        d:DataContext="{d:DesignInstance app:DesignTimeComposition}"
        Title="{Binding App.Title}">
    <StackPanel HorizontalAlignment="Center" VerticalAlignment="Center" DataContext="{Binding Clock}">
        <TextBlock Text="{Binding Date}" FontSize="64" HorizontalAlignment="Center"/>
        <TextBlock Text="{Binding Time}" FontSize="128" HorizontalAlignment="Center"/>        
    </StackPanel>
</Window>
```

To use bindings in views:

- You can set a shared resource as a data context

  `DataContext="{StaticResource Composition}"`

  and `d:DataContext="{d:DesignInstance app:DesignTimeComposition}"` for the design time

- Use the bindings as usual:

  `Title="{Binding App.Title}"`

The [project file](/samples/WpfAppNetCore/WpfAppNetCore.csproj) looks like this:

```xml
<Project Sdk="Microsoft.NET.Sdk">
   ...
    <ItemGroup>
        <PackageReference Include="Pure.DI" Version="2.5.0">
            <PrivateAssets>all</PrivateAssets>
            <IncludeAssets>runtime; build; native; contentfiles; analyzers; buildtransitive</IncludeAssets>
        </PackageReference>
    </ItemGroup>

</Project>
```

It contains an additional reference to the NuGet package:

|         |                                                                                            |                          |
|---------|--------------------------------------------------------------------------------------------|:-------------------------|
| Pure.DI | [![NuGet](https://img.shields.io/nuget/v/Pure.DI)](https://www.nuget.org/packages/Pure.DI) | DI source code generator |

