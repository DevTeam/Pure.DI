This Markdown-formatted document contains information about working with Pure.DI

# Usage scenarios.

## Auto-bindings

Non-abstract types can be injected without any additional bindings.

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

>[!WARNING]
>This approach is not recommended if you follow the dependency inversion principle or need precise lifetime control.

Prefer injecting abstractions (for example, interfaces) and map them to implementations as in most [other examples](injections-of-abstractions.md).

## Injections of abstractions

This example shows the recommended approach: depend on abstractions and bind them to implementations.

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


## Composition roots

This example shows several ways to create a composition root.
>[!TIP]
>There is no hard limit on roots, but prefer a small number. Ideally, an application has a single composition root.

In classic DI containers, the composition is resolved dynamically via calls like `T Resolve<T>()` or `object GetService(Type type)`. The root is simply the requested type, and you can have as many as you like. In Pure.DI, each root generates a property or method at compile time, so roots are explicit and defined via `Root(string rootName)`.

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

## Resolve methods

This example shows how to resolve composition roots via `Resolve` methods, using the composition as a _Service Locator_. The `Resolve` methods are generated automatically.

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

## Simplified binding

You can call `Bind()` without type parameters. It binds the implementation type itself, and if it is not abstract, all directly implemented abstract types except special ones.

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

## Factory

Demonstrates how to use factories for manual creation and initialization. While the generator usually infers dependencies from constructors, factories provide custom creation or setup logic when needed.

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

The example creates a service that depends on a logger initialized with a date-based file name. The `Tag` attribute enables named dependencies for more complex setups.

## Injection on demand

This example creates dependencies on demand using a factory delegate. The service (`GameLevel`) needs multiple instances of `IEnemy`, so it receives a `Func<IEnemy>` that can create new instances when needed.

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

## Injections on demand with arguments

This example uses a parameterized factory so dependencies can be created with runtime arguments. The service creates sensors with specific IDs at instantiation time.

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

## Composition arguments

Use composition arguments when you need to pass state into the composition. Define them with `Arg<T>(string argName)` (optionally with tags) and use them like any other dependency. Only arguments that are used in the object graph become constructor parameters.
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

## Root arguments

Use root arguments when you need to pass state into a specific root. Define them with `RootArg<T>(string argName)` (optionally with tags) and use them like any other dependency. A root that uses at least one root argument becomes a method, and only arguments used in that root's object graph appear in the method signature. Use unique argument names to avoid collisions.
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

## Tags

Tags let you control dependency selection when multiple implementations exist:

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

## Smart tags

Large object graphs often need many tags. String tags are error-prone and easy to mistype. Prefer `Enum` values as tags, and _Pure.DI_ helps make this safe.

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

## Simplified lifetime-specific bindings

You can use the `Transient<>()`, `Singleton<>()`, `PerResolve<>()`, etc. methods. In this case binding will be performed for the implementation type itself, and if the implementation is not an abstract type or structure, for all abstract but NOT special types that are directly implemented.

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
| ✅ | `OrderManager`        | implementation type itself                        |
| ✅ | `IOrderRepository`    | directly implements                               |
| ✅ | `IOrderNotification`  | directly implements                               |
| ❌ | `IDisposable`         | special type                                      |
| ❌ | `IEnumerable<string>` | special type                                      |
| ❌ | `ManagerBase`         | non-abstract                                      |
| ❌ | `IManager`            | is not directly implemented by class OrderManager |

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

Demonstrates async disposable scope lifetime, where scoped instances are disposed asynchronously when the scope ends.

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

## Lazy

Demonstrates lazy injection using Lazy<T>, delaying instance creation until the Value property is accessed.

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

Demonstrates `ValueTask<T>` injection, which provides a more efficient alternative to `Task<T>` for scenarios where the result is often already available synchronously.

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

## Span and ReadOnlySpan

Specifying `Span<T>` and `ReadOnlySpan<T>` work the same as with the array `T[]`.

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

Demonstrates `WeakReference<T>` injection, allowing references to objects without preventing garbage collection.

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

## Func with arguments

Demonstrates how to use Func<T> with arguments for dynamic creation of instances with runtime parameters.

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

Demonstrates how to use Func<T> with tags for dynamic creation of tagged instances.

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

## Keyed service provider

Demonstrates integration with Microsoft.Extensions.DependencyInjection's keyed services feature.

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

## Generic composition roots with constraints

>[!IMPORTANT]
>``Resolve` methods cannot be used to resolve generic composition roots.

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
>The method `Inject()`cannot be used outside of the binding setup.

## Generic async composition roots with constraints

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

## Custom generic argument

Demonstrates how to create custom generic arguments for advanced generic binding scenarios.

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

## Generic root arguments

Demonstrates how to pass type arguments as parameters to generic composition roots.

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

Demonstrates complex generic root argument scenarios with multiple type parameters and constraints.

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

## Generic builder

Demonstrates how to create generic builders for build-up patterns with type parameters.

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

Demonstrates how to create generic builders for all types derived from a generic base type known at compile time.

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

Demonstrates how to create roots for all generic types that inherit from a given base type at compile time.

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

## Generic injections on demand

Demonstrates how to create generic dependencies on demand using factory delegates with generic type parameters.

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

Demonstrates how to create generic dependencies on demand with custom arguments using factory delegates.

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

Demonstrates how to create and use custom attributes for generic type arguments, enabling advanced generic binding scenarios.

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

## Bind attribute

`BindAttribute` lets you bind properties, fields, or methods declared on the bound type.

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
    // The [Bind] attribute specifies that the property is a source of dependency
    [Bind] public IGps Gps { get; } = new Gps();

    [Bind] public ICamera Camera { get; } = new Camera();
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

## Bind attribute with lifetime and tag

Demonstrates how to configure the Bind attribute with lifetime and tag parameters for more precise binding control.

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
    [Bind(lifetime: Lifetime.Singleton, tags: ["HighPerformance"])]
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
>Specifying lifetime and tag in the Bind attribute allows for fine-grained control over instance creation and binding resolution.

## Bind attribute for a generic type

Demonstrates how to use the Bind attribute to configure bindings for generic types, allowing automatic registration without explicit binding declarations.

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
    [Bind(typeof(IComments<TT>))]
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
>The Bind attribute provides a declarative way to specify bindings directly on types, reducing the need for manual composition setup.

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

For more hints, see [this](README.md#setup-hints) page.

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

For more hints, see [this](README.md#setup-hints) page.

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
For more hints, see [this](README.md#setup-hints) page.

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
For more hints, see [this](README.md#setup-hints) page.

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
For more hints, see [this](README.md#setup-hints) page.

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
For more hints, see [this](README.md#setup-hints) page.

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
For more hints, see [this](README.md#setup-hints) page.

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
For more hints, see [this](README.md#setup-hints) page.

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
For more hints, see [this](README.md#setup-hints) page.

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

For more hints, see [this](README.md#setup-hints) page.

## Composition root kinds

Demonstrates different kinds of composition roots that can be created: public methods, private partial methods, and static roots. Each kind serves different use cases for accessing composition roots with appropriate visibility and lifetime semantics.

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

## Root with name template

Demonstrates how to use name templates for composition roots, allowing dynamic generation of root names based on patterns or parameters.

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

## Tag Unique

`Tag.Unique` is useful to register a binding with a unique tag. It will not be available through the composition root or `Resolve` methods directly, but can be injected in compositions as some kind of enumeration.

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

    public required IUserRepository BackupRepository { init; get; }

    public IUserRepository FetcherRepository => fetcher.Repository;
}
```

To run the above code, the following NuGet packages must be added:
 - [Pure.DI](https://www.nuget.org/packages/Pure.DI)
 - [Shouldly](https://www.nuget.org/packages/Shouldly)

>[!WARNING]
>Each potentially injectable argument, property, or field contains an additional tag. This tag can be used to specify what can be injected there. This will only work if the binding type and the tag match. So while this approach can be useful for specifying what to enter, it can be more expensive to maintain and less reliable, so it is recommended to use attributes like `[Tag(...)]` instead.

## Tag on a constructor argument

The wildcards `*` and `?` are supported.

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

The wildcards `*` and `?` are supported.

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
    public required IPaymentGateway Gateway { init; get; }
}
```

To run the above code, the following NuGet packages must be added:
 - [Pure.DI](https://www.nuget.org/packages/Pure.DI)
 - [Shouldly](https://www.nuget.org/packages/Shouldly)

>[!WARNING]
>Each potentially injectable argument, property, or field contains an additional tag. This tag can be used to specify what can be injected there. This will only work if the binding type and the tag match. So while this approach can be useful for specifying what to enter, it can be more expensive to maintain and less reliable, so it is recommended to use attributes like `[Tag(...)]` instead.

## Tag on a method argument

The wildcards `*` and `?` are supported.

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

## Tag on injection site with wildcards

The wildcards `*` and `?` are supported.

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

    public required ISensor OutdoorSensor { init; get; }

    public ISensor ClimateSensor => climateControl.Sensor;
}
```

To run the above code, the following NuGet packages must be added:
 - [Pure.DI](https://www.nuget.org/packages/Pure.DI)
 - [Shouldly](https://www.nuget.org/packages/Shouldly)

>[!WARNING]
>Each potentially injectable argument, property, or field contains an additional tag. This tag can be used to specify what can be injected there. This will only work if the binding type and the tag match. So while this approach can be useful for specifying what to enter, it can be more expensive to maintain and less reliable, so it is recommended to use attributes like `[Tag(...)]` instead.

## Dependent compositions

The `Setup` method has an additional argument `kind`, which defines the type of composition:
- `CompositionKind.Public` - will create a normal composition class, this is the default setting and can be omitted, it can also use the `DependsOn` method to use it as a dependency in other compositions
- `CompositionKind.Internal` - the composition class will not be created, but that composition can be used to create other compositions by calling the `DependsOn` method with its name
- `CompositionKind.Global` - the composition class will also not be created, but that composition will automatically be used to create other compositions

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

## Inheritance of compositions

Demonstrates how composition classes can inherit from each other, allowing reuse of bindings and composition roots across multiple related compositions.

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

## Accumulators

Accumulators allow you to accumulate instances of certain types and lifetimes.

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

A partial class can contain setup code.

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

## Thread-safe overrides

Demonstrates how to create thread-safe overrides in compositions, ensuring that override operations work correctly in multi-threaded scenarios.

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

>[!IMPORTANT]
>Thread-safe overrides are essential when composition instances are shared across multiple threads or when parallel resolution is required.

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

## Consumer types

`ConsumerTypes` is used to get the list of consumer types of a given dependency. It contains an array of types and guarantees that it will contain at least one element. The use of `ConsumerTypes` is demonstrated on the example of [Serilog library](https://serilog.net/):

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


## Tracking disposable instances per a composition root

Demonstrates how disposable instances are tracked per composition root and disposed when the composition is disposed.

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

Demonstrates how disposable instances created within delegate factories are tracked and disposed properly when the composition is disposed.

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

## Tracking disposable instances with different lifetimes

Demonstrates how disposable instances with different lifetimes are tracked and disposed correctly according to their respective lifetime scopes.

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

## Tracking async disposable instances per a composition root

Demonstrates how async disposable instances are tracked per composition root and disposed asynchronously when the composition is disposed.

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

Demonstrates how async disposable instances created within delegate factories are tracked and disposed properly when the composition is disposed.

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

## Exposed roots

Composition roots from other assemblies or projects can be used as a source of bindings. When you add a binding to a composition from another assembly or project, the roots of the composition with the `RootKind.Exposed` type will be used in the bindings automatically. For example, in some assembly a composition is defined as:
```c#
public partial class CompositionInOtherProject
{
    private static void Setup() =>
        DI.Setup()
            .Bind().As(Lifetime.Singleton).To<MyDependency>()
            .Bind().To<MyService>()
            .Root<IMyService>("MyService", kind: RootKinds.Exposed);
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

## Exposed roots with tags

Composition roots from other assemblies or projects can be used as a source of bindings. When you add a binding to a composition from another assembly or project, the roots of the composition with the `RootKind.Exposed` type will be used in the bindings automatically. For example, in some assembly a composition is defined as:
```c#
public partial class CompositionWithTagsInOtherProject
{
    private static void Setup() =>
        DI.Setup()
            .Bind().As(Lifetime.Singleton).To<MyDependency>()
            .Bind("Some tag").To<MyService>()
            .Root<IMyService>("MyService", "Some tag", RootKinds.Exposed);
}
```

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


## Exposed roots via arg

Composition roots from other assemblies or projects can be used as a source of bindings passed through composition arguments. When you add a binding to a composition from another assembly or project, the roots of the composition with the `RootKind.Exposed` type will be used in the bindings automatically. For example, in some assembly a composition is defined as:
```c#
public partial class CompositionInOtherProject
{
    private static void Setup() =>
        DI.Setup()
            .Bind().As(Lifetime.Singleton).To<MyDependency>()
            .Bind().To<MyService>()
            .Root<IMyService>("MyService", kind: RootKinds.Exposed);
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

## Exposed roots via root arg

Composition roots from other assemblies or projects can be used as a source of bindings passed through root arguments. When you add a binding to a composition from another assembly or project, the roots of the composition with the `RootKind.Exposed` type will be used in the bindings automatically. For example, in some assembly a composition is defined as:
```c#
public partial class CompositionInOtherProject
{
    private static void Setup() =>
        DI.Setup()
            .Bind().As(Lifetime.Singleton).To<MyDependency>()
            .Bind().To<MyService>()
            .Root<IMyService>("MyService", kind: RootKinds.Exposed);
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

## Exposed generic roots

Composition roots from other assemblies or projects can be used as a source of bindings. When you add a binding to a composition from another assembly or project, the roots of the composition with the `RootKind.Exposed` type will be used in the bindings automatically. For example, in some assembly a composition is defined as:
```c#
public partial class CompositionInOtherProject
{
    private static void Setup() =>
    DI.Setup()
        .Hint(Hint.Resolve, "Off")
        .Bind().To(() => 99)
        .Bind().As(Lifetime.Singleton).To<MyDependency>()
        .Bind().To<MyGenericService<TT>>()
        .Root<IMyGenericService<TT>>("GetMyService", kind: RootKinds.Exposed);
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

## Exposed generic roots with args

Composition roots from other assemblies or projects can be used as a source of bindings. When you add a binding to a composition from another assembly or project, the roots of the composition with the `RootKind.Exposed` type will be used in the bindings automatically. For example, in some assembly a composition is defined as:
```c#
public partial class CompositionWithGenericRootsAndArgsInOtherProject
{
    private static void Setup() =>
        DI.Setup()
            .Hint(Hint.Resolve, "Off")
            .RootArg<int>("id")
            .Bind().As(Lifetime.Singleton).To<MyDependency>()
            .Bind().To<MyGenericService<TT>>()
            .Root<IMyGenericService<TT>>("GetMyService", kind: RootKinds.Exposed);
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

## AutoMapper

Demonstrates integration with AutoMapper library, showing how Pure.DI can work alongside object mapping solutions.

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
>AutoMapper integration enables clean separation between DI composition concerns and object mapping logic.

## JSON serialization

Demonstrates how to handle JSON serialization scenarios with Pure.DI, showing integration with serialization libraries.

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

>[!NOTE]
>Proper DI integration with serialization requires careful handling of object creation and property injection.

## Serilog

Demonstrates integration with _Serilog_ logging library, showing how to inject logger instances with context information.

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
>Proper logging integration with DI enables context-aware logging throughout the application with minimal configuration.

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

Demonstrates basic integration with Unity game engine, showing how Pure.DI can be used for dependency injection in Unity projects.

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
>Unity integration requires special considerations due to Unity's component-based architecture and lifecycle management.

## Unity with prefabs

Demonstrates advanced Unity integration showing how Pure.DI works with Unity prefabs and component lifecycle.

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
>Prefab integration with DI requires careful handling of Unity's instantiation and component initialization phases.

# Examples of using Pure.DI for different types of .NET projects.

#### Avalonia application

[![CSharp](https://img.shields.io/badge/C%23-code-blue.svg)](/samples/AvaloniaApp)

This example demonstrates the creation of an [Avalonia](https://avaloniaui.net/) application in the pure DI paradigm using the Pure.DI code generator.

> [!NOTE]
> [Another example](samples/SingleRootAvaloniaApp) with Avalonia shows how to create an application with a single composition root.

The definition of the composition is in [Composition.cs](/samples/AvaloniaApp/Composition.cs). This class sets up how the object graphs will be created for the application. Do not forget to define any necessary composition roots, for example, these can be view models such as _ClockViewModel_:

```c#
using Pure.DI;
using static Pure.DI.Lifetime;
using static Pure.DI.RootKinds;

namespace AvaloniaApp;

partial class Composition
{
    private void Setup() => DI.Setup()
        .Root<IAppViewModel>(nameof(App), kind: Virtual)
        .Root<IClockViewModel>(nameof(Clock), kind: Virtual)

        .OrdinalAttribute<InitializableAttribute>()

        .Bind().As(Singleton).To<ClockViewModel>()
        .Bind().To<ClockModel>()
        .Bind().As(Singleton).To<Ticks>()

        // Infrastructure
        .Bind().To<DebugLog<TT>>()
        .Bind().To<AvaloniaDispatcher>();
}
```

Advantages over classical DI container libraries:
- No performance impact or side effects when creating object graphs.
- All logic for analyzing the graph of objects, constructors and methods takes place at compile time. Pure.DI notifies the developer at compile time of missing or cyclic dependencies, cases when some dependencies are not suitable for injection, etc.
- Does not add dependencies to additional assemblies.
- Since the generated code uses primitive language constructs to create object graphs and does not use any libraries, you can easily debug the object graph code as regular code in your application.

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

Advantages over classical DI container libraries:
- No explicit initialization of data contexts is required. Data contexts are configured directly in `.axaml` files according to the MVVM approach.
- The code is simpler, more compact, and requires less maintenance effort.
- The main window is created in a pure DI paradigm, and it can be easily supplied with all necessary dependencies via DI as regular types.

You can now use bindings and the code-behind-free approach. All previously defined composition roots are now available from [markup](/samples/AvaloniaApp/Views/MainWindow.xaml) without any effort, e.g. _Clock_:

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

Advantages over classical DI container libraries:
- The code-behind `.cs` files for views are free of any logic.
- This approach works just as well during design time.
- You can easily use different view models in a single view.
- Bindings depend on properties through abstractions, which additionally ensures weak coupling of types in application. This is in line with the basic principles of DI.

The [project file](/samples/AvaloniaApp/AvaloniaApp.csproj) looks like this:

```xml
<Project Sdk="Microsoft.NET.Sdk">
    ...
    <ItemGroup>
        <PackageReference Include="Pure.DI" Version="2.3.5">
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

This example demonstrates the creation of a [Blazor Server](https://learn.microsoft.com/en-us/aspnet/core/blazor/hosting-models#blazor-server) application in the pure DI paradigm using the Pure.DI code generator.

The composition setup file is [Composition.cs](/samples/BlazorServerApp/Composition.cs):

```c#
using Pure.DI;
using Pure.DI.MS;
using static Pure.DI.Lifetime;

namespace BlazorServerApp;

partial class Composition : ServiceProviderFactory<Composition>
{
    private void Setup() => DI.Setup()
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

The composition class inherits from `ServiceProviderFactory<T>`, where `T` is the composition class itself.

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
        <PackageReference Include="Pure.DI" Version="2.3.5">
            <PrivateAssets>all</PrivateAssets>
            <IncludeAssets>runtime; build; native; contentfiles; analyzers; buildtransitive</IncludeAssets>
        </PackageReference>
        <PackageReference Include="Pure.DI.MS" Version="2.3.5" />
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

This example demonstrates the creation of a [Blazor WebAssembly](https://learn.microsoft.com/en-us/aspnet/core/blazor/hosting-models#blazor-webassembly) application in the pure DI paradigm using the Pure.DI code generator.

The composition setup file is [Composition.cs](/samples/BlazorWebAssemblyApp/Composition.cs):

```c#
using Pure.DI;
using Pure.DI.MS;
using static Pure.DI.Lifetime;

namespace BlazorWebAssemblyApp;

partial class Composition : ServiceProviderFactory<Composition>
{
    private void Setup() => DI.Setup()
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

The composition class inherits from `ServiceProviderFactory<T>`, where `T` is the composition class itself.

The web application entry point is in the [Program.cs](/samples/BlazorWebAssemblyApp/Program.cs) file:

```c#
var builder = WebAssemblyHostBuilder.CreateDefault(args);

// Uses Composition as an alternative IServiceProviderFactory
using var composition = new Composition();
builder.ConfigureContainer(composition);
```

The [project file](/samples/BlazorWebAssemblyApp/BlazorWebAssemblyApp.csproj) looks like this:

```xml
<Project Sdk="Microsoft.NET.Sdk.Web">
    ...
    <ItemGroup>
        <PackageReference Include="Pure.DI" Version="2.3.5">
            <PrivateAssets>all</PrivateAssets>
            <IncludeAssets>runtime; build; native; contentfiles; analyzers; buildtransitive</IncludeAssets>
        </PackageReference>
        <PackageReference Include="Pure.DI.MS" Version="2.3.5" />
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

This example is very similar to the [simple console application](ConsoleTemplate.md), except that this is a [native AOT](https://learn.microsoft.com/en-us/dotnet/core/deploying/native-aot/) application.

The [project file](/samples/ShroedingersCatNativeAOT/ShroedingersCatNativeAOT.csproj) looks like this:

```xml
<Project Sdk="Microsoft.NET.Sdk">
    ...
    <ItemGroup>
        <PackageReference Include="Pure.DI" Version="2.3.5">
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

This example demonstrates the creation of a simple console application in the pure DI paradigm using the Pure.DI code generator. All code is in [one file](/samples/ShroedingersCat/Program.cs) for easy reading:

```c#
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

public class CardboardBox<T>(T content) : IBox<T>
{
    public T Content { get; } = content;

    public override string ToString() => $"[{Content}]";
}

public class ShroedingersCat(Lazy<State> superposition) : ICat
{
    // The decoherence of the superposition
    // at the time of observation via an irreversible process
    public State State => superposition.Value;

    public override string ToString() => $"{State} cat";
}

// Let's glue it all together

internal partial class Composition
{
    // In fact, this code is never run, and the method can have any name or be a constructor, for example,
    // and can be in any part of the compiled code because this is just a hint to set up an object graph.
    // Here the setup is part of the generated class, just as an example.
    void Setup() => DI.Setup()
        // Models a random subatomic event that may or may not occur
        .Bind().As(Singleton).To<Random>()
        // Represents a quantum superposition of 2 states: Alive or Dead
        .Bind().To((Random random) => (State)random.Next(2))
        // Represents Schrodinger's cat
        .Bind().To<ShroedingersCat>()
        // Represents a cardboard box with any content
        .Bind().To<CardboardBox<TT>>()
        // Composition Root
        .Root<Program>("Root");
}

// Time to open boxes!

public class Program(IBox<ICat> box)
{
    // Composition Root, a single place in an application
    // where the composition of the object graphs for an application takes place
    public static void Main() => new Composition().Root.Run();

    private void Run() => Console.WriteLine(box);
}
```

The [project file](/samples/ShroedingersCat/ShroedingersCat.csproj) looks like this:

```xml
<Project Sdk="Microsoft.NET.Sdk">
    ...
    <ItemGroup>
        <PackageReference Include="Pure.DI" Version="2.3.5">
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

This example is very similar to the [simple console application](ConsoleTemplate.md), except that the composition is [defined](/samples/ShroedingersCatTopLevelStatements/Program.cs) as top-level statements and looks a little less verbose:

```c#
using Pure.DI;
using static Pure.DI.Lifetime;

// Composition root
new Composition().Root.Run();

// In fact, this code is never run, and the method can have any name or be a constructor, for example,
// and can be in any part of the compiled code because this is just a hint to set up an object graph.
DI.Setup("Composition")
    // Models a random subatomic event that may or may not occur
    .Bind().As(Singleton).To<Random>()
    // Represents a quantum superposition of 2 states: Alive or Dead
    .Bind().To((Random random) => (State)random.Next(2))
    // Represents Schrodinger's cat
    .Bind().To<ShroedingersCat>()
    // Represents a cardboard box with any content
    .Bind().To<CardboardBox<TT>>()
    // Composition Root
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

public class CardboardBox<T>(T content) : IBox<T>
{
    public T Content { get; } = content;

    public override string ToString() => $"[{Content}]";
}

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
        <PackageReference Include="Pure.DI" Version="2.3.5">
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

This example demonstrates the creation of an Entity Framework application in the pure DI paradigm using the Pure.DI code generator.

The composition setup file is [Composition.cs](/samples/EF/Composition.cs):

```c#
using System.Diagnostics;
using Pure.DI;
using Pure.DI.MS;

namespace EF;

partial class Composition : ServiceProviderFactory<Composition>
{
    private void Setup() => DI.Setup()
        .Root<Program>(nameof(Root))

        .Bind().As(Lifetime.PerResolve).To<PersonService>()
        .Bind().As(Lifetime.Singleton).To<ContactService>();
}
```

The composition class inherits from `ServiceProviderFactory<T>`, where `T` is the composition class itself.

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

The [project file](/samples/EF/EF.csproj) looks like this:

```xml
<Project Sdk="Microsoft.NET.Sdk.Web">
    ...
    <ItemGroup>
        <PackageReference Include="Pure.DI" Version="2.3.5">
            <PrivateAssets>all</PrivateAssets>
            <IncludeAssets>runtime; build; native; contentfiles; analyzers; buildtransitive</IncludeAssets>
        </PackageReference>
        <PackageReference Include="Pure.DI.MS" Version="2.3.5" />
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

This example demonstrates the creation of a gRPC service in the pure DI paradigm using the Pure.DI code generator.

The composition setup file is [Composition.cs](/samples/GrpcService/Composition.cs):

```c#
using Pure.DI;
using Pure.DI.MS;
using static Pure.DI.Lifetime;

namespace GrpcService;

partial class Composition : ServiceProviderFactory<Composition>
{
    private void Setup() => DI.Setup()
        .Root<ClockService>()

        .Bind().As(Singleton).To<ClockViewModel>()
        .Bind().To<ClockModel>()
        .Bind().As(Singleton).To<Ticks>()

        // Infrastructure
        .Bind().To<MicrosoftLoggerAdapter<TT>>()
        .Bind().To<CurrentThreadDispatcher>();
}
```

The composition class inherits from `ServiceProviderFactory<T>`, where `T` is the composition class itself.

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
        <PackageReference Include="Pure.DI" Version="2.3.5">
            <PrivateAssets>all</PrivateAssets>
            <IncludeAssets>runtime; build; native; contentfiles; analyzers; buildtransitive</IncludeAssets>
        </PackageReference>
        <PackageReference Include="Pure.DI.MS" Version="2.3.5" />
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

This example demonstrates the creation of a [MAUI application](https://learn.microsoft.com/en-us/dotnet/maui/what-is-maui) in the pure DI paradigm using the Pure.DI code generator.

The definition of the composition is in [Composition.cs](/samples/MAUIApp/Composition.cs). Do not forget to define any necessary composition roots, for example, these can be view models such as _ClockViewModel_:

```c#
using Pure.DI;
using Pure.DI.MS;
using static Pure.DI.Lifetime;

namespace MAUIApp;

partial class Composition : ServiceProviderFactory<Composition>
{
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

The composition class inherits from `ServiceProviderFactory<T>`, where `T` is the composition class itself.

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

All previously defined composition roots are now accessible from [markup](/samples/MAUIApp/MainWindow.xaml) without any effort:

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
        <PackageReference Include="Pure.DI" Version="2.3.5">
            <PrivateAssets>all</PrivateAssets>
            <IncludeAssets>runtime; build; native; contentfiles; analyzers; buildtransitive</IncludeAssets>
        </PackageReference>
        <PackageReference Include="Pure.DI.MS" Version="2.3.5" />
    </ItemGroup>

</Project>
```

It contains additional references to NuGet packages:

|            |                                                                                                  |                                              |
|------------|--------------------------------------------------------------------------------------------------|:---------------------------------------------|
| Pure.DI    | [![NuGet](https://img.shields.io/nuget/v/Pure.DI)](https://www.nuget.org/packages/Pure.DI)       | DI source code generator                     |


#### Minimal Web API

[![CSharp](https://img.shields.io/badge/C%23-code-blue.svg)](/samples/MinimalWebAPI)

This example demonstrates the creation of a Minimal Web API application in the pure DI paradigm using the Pure.DI code generator.

The composition setup file is [Composition.cs](/samples/MinimalWebAPI/Composition.cs):

```c#
using Pure.DI;
using Pure.DI.MS;
using static Pure.DI.Lifetime;

namespace MinimalWebAPI;

partial class Composition : ServiceProviderFactory<Composition>
{
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

The composition class inherits from `ServiceProviderFactory<T>`, where `T` is the composition class itself.

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
        <PackageReference Include="Pure.DI" Version="2.3.5">
            <PrivateAssets>all</PrivateAssets>
            <IncludeAssets>runtime; build; native; contentfiles; analyzers; buildtransitive</IncludeAssets>
        </PackageReference>
        <PackageReference Include="Pure.DI.MS" Version="2.3.5" />
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

This example demonstrates the creation of a [Unity](https://unity.com/) application in the pure DI paradigm using the Pure.DI code generator.

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
    void Setup() => DI.Setup()
        .DependsOn(nameof(ClocksComposition), SetupContextKind.Members)
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

Advantages over classical DI container libraries:
- No performance impact or side effects when creating object graphs.
- All logic for analyzing the graph of objects, constructors and methods takes place at compile time. Pure.DI notifies the developer at compile time of missing or cyclic dependencies, cases when some dependencies are not suitable for injection, etc.
- Does not add dependencies to additional assemblies.
- Since the generated code uses primitive language constructs to create object graphs and does not use any libraries, you can debug the object graph code as regular code in your application.

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

#### Web API

[![CSharp](https://img.shields.io/badge/C%23-code-blue.svg)](/samples/WebAPI)

This example demonstrates the creation of a Web API application in the pure DI paradigm using the Pure.DI code generator.

The composition setup file is [Composition.cs](/samples/WebAPI/Composition.cs):

```c#
using Pure.DI;
using Pure.DI.MS;
using static Pure.DI.Lifetime;

namespace WebAPI;

partial class Composition : ServiceProviderFactory<Composition>
{
    private void Setup() => DI.Setup()
        .Roots<ControllerBase>()

        .Bind().As(Singleton).To<ClockViewModel>()
        .Bind().To<ClockModel>()
        .Bind().As(Singleton).To<Ticks>()

        // Infrastructure
        .Bind().To<MicrosoftLoggerAdapter<TT>>()
        .Bind().To<CurrentThreadDispatcher>();
}
```

The composition class inherits from `ServiceProviderFactory<T>`, where `T` is the composition class itself.

The web application entry point is in the [Program.cs](/samples/WebAPI/Program.cs) file:

```c#
var builder = WebApplication.CreateBuilder(args);

using var composition = new Composition();

// Uses Composition as an alternative IServiceProviderFactory
builder.Host.UseServiceProviderFactory(composition);
```

The [project file](/samples/WebAPI/WebAPI.csproj) looks like this:

```xml
<Project Sdk="Microsoft.NET.Sdk.Web">
    ...
    <ItemGroup>
        <PackageReference Include="Pure.DI" Version="2.3.5">
            <PrivateAssets>all</PrivateAssets>
            <IncludeAssets>runtime; build; native; contentfiles; analyzers; buildtransitive</IncludeAssets>
        </PackageReference>
        <PackageReference Include="Pure.DI.MS" Version="2.3.5" />
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

This example demonstrates the creation of a Web application in the pure DI paradigm using the Pure.DI code generator.

The composition setup file is [Composition.cs](/samples/WebApp/Composition.cs):

```c#
using Pure.DI;
using Pure.DI.MS;
using static Pure.DI.Lifetime;

namespace WebApp;

partial class Composition : ServiceProviderFactory<Composition>
{
    private void Setup() => DI.Setup()
        .Roots<ControllerBase>()

        .Bind().As(Singleton).To<ClockViewModel>()
        .Bind().To<ClockModel>()
        .Bind().As(Singleton).To<Ticks>()

        // Infrastructure
        .Bind().To<MicrosoftLoggerAdapter<TT>>()
        .Bind().To<CurrentThreadDispatcher>();
}
```

The composition class inherits from `ServiceProviderFactory<T>`, where `T` is the composition class itself.

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
        <PackageReference Include="Pure.DI" Version="2.3.5">
            <PrivateAssets>all</PrivateAssets>
            <IncludeAssets>runtime; build; native; contentfiles; analyzers; buildtransitive</IncludeAssets>
        </PackageReference>
        <PackageReference Include="Pure.DI.MS" Version="2.3.5" />
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

This example demonstrates the creation of a WinForms application in the pure DI paradigm using the Pure.DI code generator.

The composition definition is in the file [Composition.cs](/samples/WinFormsAppNetCore/Composition.cs). Remember to define all the necessary composition roots, for example, this could be a main form such as _FormMain_:

```c#
using Pure.DI;
using static Pure.DI.Lifetime;

namespace WinFormsAppNetCore;

partial class Composition
{
    private void Setup() => DI.Setup()
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
        <PackageReference Include="Pure.DI" Version="2.3.5">
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

This example demonstrates the creation of a WinForms application in the pure DI paradigm using the Pure.DI code generator.

The composition definition is in the file [Composition.cs](/samples/WinFormsApp/Composition.cs). Remember to define all the necessary composition roots, for example, this could be a main form such as _FormMain_:

```c#
using Pure.DI;
using static Pure.DI.Lifetime;

namespace WinFormsApp;

partial class Composition
{
    private void Setup() => DI.Setup()
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
        <PackageReference Include="Pure.DI" Version="2.3.5">
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

This example demonstrates the creation of a WPF application in the pure DI paradigm using the Pure.DI code generator.

The definition of the composition is in [Composition.cs](/samples/WpfAppNetCore/Composition.cs). This class sets up how the object graphs will be created for the application. Do not forget to define any necessary composition roots, for example, these can be view models such as _ClockViewModel_:

```c#
using Pure.DI;
using static Pure.DI.Lifetime;

namespace WpfAppNetCore;

partial class Composition
{
    private void Setup() => DI.Setup()
        .Root<IAppViewModel>(nameof(App))
        .Root<IClockViewModel>(nameof(Clock))

        .Bind().As(Singleton).To<ClockViewModel>()
        .Bind().To<ClockModel>()
        .Bind().As(Singleton).To<Ticks>()

        // Infrastructure
        .Bind().To<DebugLog<TT>>()
        .Bind().To<WpfDispatcher>();
}
```

Advantages over classical DI container libraries:
- No performance impact or side effects when creating object graphs.
- All logic for analyzing the graph of objects, constructors and methods takes place at compile time. Pure.DI notifies the developer at compile time of missing or cyclic dependencies, cases when some dependencies are not suitable for injection, etc.
- Does not add dependencies to any additional assembly.
- Since the generated code uses primitive language constructs to create object graphs and does not use any libraries, you can easily debug the object graph code as regular code in your application.

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

Advantages over classical DI container libraries:
- No explicit initialization of data contexts is required. Data contexts are configured directly in `\.xaml` files according to the MVVM approach.
- The code is simpler, more compact, and requires less maintenance effort.
- The main window is created in a pure DI paradigm, and it can be easily supplied with all necessary dependencies via DI as regular types.

You can now use bindings to model views without even editing the views `.cs` code files. All previously defined composition roots are now accessible from [markup](/samples/WpfAppNetCore/Views/MainWindow.xaml) without any effort, such as _ClockViewModel_:

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

Advantages over classical DI container libraries:
- The code-behind `.cs` files for views are free of any logic.
- This approach works just as well during design time.
- You can easily use different view models in a single view.
- Bindings depend on properties through abstractions, which additionally ensures weak coupling of types in application. This is in line with the basic principles of DI.

The [project file](/samples/WpfAppNetCore/WpfAppNetCore.csproj) looks like this:

```xml
<Project Sdk="Microsoft.NET.Sdk">
   ...
    <ItemGroup>
        <PackageReference Include="Pure.DI" Version="2.3.5">
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

