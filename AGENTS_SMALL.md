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

## PerBlock

The `PerBlock` lifetime does not guarantee that there will be a single dependency instance for each instance of the composition root (as for the `PerResolve` lifetime), but is useful for reducing the number of instances of a type.

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
>`PerBlock` lifetime provides a balance between `PerResolve` and `Transient`, reducing instance count within a resolution block.

## Scope

Demonstrates scoped lifetime with `Hint(Hint.ScopeFactory, "on")` where scopes are represented by generated `Scope` objects created via `CreateScope()`.

```c#
using Shouldly;
using Pure.DI;
using static Pure.DI.Lifetime;

var composition = new Composition(desc: "Checkout");
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
sealed class CheckoutService(
    string description,
    IRequestContext context)
    : ICheckoutService
{
    public IRequestContext Context => context;
}

// Represents a scope
class Scope(Composition composition): IDisposable
{
    private readonly Composition _scope = composition.CreateScope();

    public ICheckoutService Checkout => _scope.RequestRoot;

    public void Dispose() => _scope.Dispose();
}

partial class Composition
{
    static void Setup() =>

        DI.Setup()
            .Hint(Hint.ScopeFactoryName, "CreateScope")
            .Arg<string>("desc")
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
>This approach is useful when you need runtime scope creation without deriving a child composition type.

## Scope factory

Demonstrates scoped lifetime with `Hint(Hint.ScopeFactory, "on")` where scopes are represented by generated `Scope` objects created via `CreateScope()`.

```c#
using Shouldly;
using Pure.DI;
using static Pure.DI.Lifetime;

var composition = new Composition();
IRequestContext ctx1;
IRequestContext ctx2;

// Request #1
using (var request1 = composition.CreateScope())
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
using (var request2 = composition.CreateScope())
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
            .Hint(Hint.ScopeFactoryName, "CreateScope")
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

>[!NOTE]
>This approach is useful when you need runtime scope creation without deriving a child composition type.

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

Demonstrates how to set a default lifetime that is used when no specific lifetime is specified for a binding. This is useful when a particular lifetime is used more often than others.

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
