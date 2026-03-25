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

## Factory

Demonstrates how to use factories for manual creation and initialization when constructor injection alone is not enough.
Use factory bindings for custom setup, external APIs, or controlled object state during creation.

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

Demonstrates how to create factories with lifetime-specific bindings, providing a concise way to define factories with proper lifetime semantics.

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
    .Builders<IRobot>("BuildUp");

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
```

To run the above code, the following NuGet packages must be added:
 - [Pure.DI](https://www.nuget.org/packages/Pure.DI)
 - [Shouldly](https://www.nuget.org/packages/Shouldly)

Important Notes:
- The default builder method name is `BuildUp`
- The first argument to the builder method is always the instance to be built

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

Demonstrates how to create static composition roots that don't require instantiation of the composition class.

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
>Static roots are useful when you want to access services without creating a composition instance.

## Async Root

Demonstrates how to define asynchronous composition roots that return Task or Task<T>, enabling async operations during composition.

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

Demonstrates how to use `ref` and `out` parameters in dependency injection for scenarios where you need to pass values by reference.

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
>`ref` dependencies are useful for scenarios where you need to return multiple values or modify parameters during injection.

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

Demonstrates how to create roots for types that match specific filter criteria, allowing selective exposure of implementations.

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

## Generics

Generic types are also supported.
>[!IMPORTANT]
>Instead of open generic types, as in classical DI container libraries, regular generic types with `marker` types as type parameters are used here. Such "marker" types allow to define dependency graph more precisely.

For the case of `IDependency<TT>`, `TT` is a `marker` type, which allows the usual `IDependency<TT>` to be used instead of an open generic type like `IDependency<>`. This makes it easy to bind generic types by specifying `marker` types such as `TT`, `TT1`, etc. as parameters of generic types:

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

Actually, the property `Root` looks like:
```c#
public IService Root
{
  get
  {
    return new Service(new Dependency<int>(), new Dependency<string>());
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

## Member ordinal attribute

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

    [Ordinal(2)]
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

The `Ordinal` attribute is part of the API, but you can define your own in any assembly or namespace.

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

## Advanced interception

This approach of interception maximizes performance by precompiling the proxy object factory.

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
    ImmutableArray.Create(
        "Process returns Processed",
        "get_DataService returns Castle.Proxies.IDataServiceProxy",
        "Count returns 55"));

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
    private readonly List<string> _log = [];
    private static readonly IProxyBuilder ProxyBuilder = new DefaultProxyBuilder();
    private readonly IInterceptor[] _interceptors = [];

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
