# Pure.DI: DI without container, without .NET type reflection, and with compile-time validation

Pure.DI is a **C# code generator for dependency injection** that builds the dependency graph **at compile time** and generates ordinary C# code for object creation. As a result, you get "pure DI": no service locator, no reflection, and problems detected at compile time.

This article covers the basic capabilities of Pure.DI. It is intended for developers who write in C#, have already encountered DI containers, and want to:

- improve DI predictability and performance;
- see the real object graph instead of a "magical" runtime container;
- detect DI configuration errors at compile time;
- use DI in environments where .NET Reflection is undesirable (AOT, Unity, older frameworks, library projects).

Details: [Pure.DI README](https://github.com/DevTeam/Pure.DI)

---

## DI: what we are actually trying to solve

Imagine a typical application scenario: a service processes an order, logs events, and interacts with an external payment gateway.

If dependencies are created "inside" services, the code quickly turns into a tangle:

```c#
// Bad: the service itself determines what and how to create
sealed class CheckoutService
{
	private readonly HttpClient _http = new();
	private readonly PaymentGatewayClient _gateway;
	private readonly ILogger _logger = new ConsoleLogger();

	public CheckoutService()
	{
		_gateway = new PaymentGatewayClient(_http, apiKey: "hardcoded");
	}

	public Task CheckoutAsync(Order order) => _gateway.PayAsync(order);
}
```

The problems here are standard:

- **Testing**: difficult to replace `PaymentGatewayClient` and `HttpClient`.
- **Configuration**: API keys and settings end up inside business code.
- **Lifetime**: who and when should release resources?
- **Tight coupling**: domain logic starts to depend on infrastructure.

DI is just a tool: *an object should not create its dependencies; they are passed to it from the outside*. Most often — through the constructor.

```c#
sealed class CheckoutService(IPaymentGateway gateway, ILogger logger)
{
	public Task CheckoutAsync(Order order) => gateway.PayAsync(order);
}
```

The question arises: **who will create `IPaymentGateway`, `ILogger`, and `CheckoutService` itself?**

The answer is *composition* of the application.

---

## Pure DI: "no container", but dependency injection exists

In the classic approach, you configure a DI container, and then at runtime the container builds the dependency graph and provides objects on request.

**Pure DI** — an approach where there is no container as a runtime entity. There are only:

- **object composition** (how to assemble the graph into a concrete object),
- **composition roots** — entry points from which the needed composition is built.

In an ideal world, you want:

- object creation to be done by **ordinary code** (without reflection and dynamic calls),
- object composition to be **transparent** and debuggable,
- errors like "missing dependency" or "cycle in graph" to be detected **before production**.

This is exactly what Pure.DI does.

---

## What is Pure.DI — in simple terms

Pure.DI is a **compile-time DI code generator**: you define the dependency graph (bindings, tags, lifetimes, roots), and the generator:

1. analyzes this dependency graph at compile time;
2. verifies that the graph is correct (no "holes", cycles, inaccessible constructors, etc.);
3. **generates** a partial composition class with ordinary properties/methods that create object compositions, with roots as "initial" objects of the composition.

The obvious key advantages of this approach:

- **Zero Overhead**: at runtime there is no container, no assembly scanning, no type reflection; exactly what you would write by hand is created — a chain of `new`.
- **Compile-Time Validation**: DI configuration errors become compilation errors/warnings.
- **Works everywhere**: no runtime dependencies — can be used even in .NET Framework 2.0+, or in AOT/Unity scenarios.
- **Transparency**: you can view and debug the generated code just like your regular code.
- **Built-in BCL Support**: many types from .NET BCL (`Func<>`, `Lazy<>`, `IEnumerable<>`, `Task`, `ValueTask`, `Span`, `Tuple`, etc.) are supported "out of the box".

---

## Why this is better than a "regular" DI container in real work

Below are only practical advantages.

### Predictable performance

If DI is reduced to generated code `new A(new B(new C()))`, then:

- no costs for reflection/dynamics;
- no hidden allocations for graph construction;
- no other "magic", which simplifies profiling and optimization.

### Errors — at compile time, not at runtime

In a classic container, you may not notice a registration error for a long time, until the execution path (possibly very rare) leads to a problematic object graph. Pure.DI builds the graph in advance — and immediately reports problems. Problematic code simply won't compile, no matter how hard you try.

### Clear composition roots instead of endless `Resolve<T>()`

In Pure.DI, composition roots are **explicit** properties or methods. This disciplines the architecture: you know exactly what objects your "container" provides externally. No Service Locator.

### Convenient for libraries and limited environments

If you are writing a library, module, plugin, Unity code, or an AOT application — the absence of runtime dependencies and reflection often becomes a decisive factor.

---

## Quick start

Before starting, it's useful to know two technical requirements (and it's important to understand that they relate to the generator, not your application):

- **.NET SDK 6.0.4+** is required for compilation (however, projects can target older platforms, down to .NET Framework 2.0+);
- **C# 8+** is required only for projects where the Pure.DI source generator is enabled (other projects in the solution can be on any version of C#).

What packages exist (most often you only need the first one):

- [Pure.DI](https://www.nuget.org/packages/Pure.DI) — DI code generator;
- [Pure.DI.Abstractions](https://www.nuget.org/packages/Pure.DI.Abstractions) — common abstractions/attributes;
- [Pure.DI.MS](https://www.nuget.org/packages/Pure.DI.MS) — additions for integration with Microsoft DI;
- [Pure.DI.Templates](https://www.nuget.org/packages/Pure.DI.Templates) — templates for creating projects from the command line.

Minimal example: there is a service that sends emails, and we want to create it through DI.

1) Add the package:

- [Pure.DI on NuGet](https://www.nuget.org/packages/Pure.DI)

2) Describe bindings and composition root:

```c#
using Pure.DI;

DI.Setup(nameof(Composition))
	.Bind<IClock>().To<SystemClock>()
	.Bind<IEmailSender>().To<SmtpEmailSender>()
	.Bind<INotificationService>().To<NotificationService>()
	// Composition root: public entry point into the graph
	.Root<INotificationService>("Notifications");

var composition = new Composition();
composition.Notifications.SendWelcome("dev@company.com");

interface IClock
{
	DateTimeOffset Now { get; }
}

sealed class SystemClock : IClock
{
	public DateTimeOffset Now => DateTimeOffset.UtcNow;
}

interface IEmailSender
{
	void Send(string to, string subject, string body);
}

sealed class SmtpEmailSender(IClock clock) : IEmailSender
{
	public void Send(string to, string subject, string body)
	{
		// Real sending would be here
		Console.WriteLine($"[{clock.Now:u}] -> {to}: {subject}");
	}
}

interface INotificationService
{
	void SendWelcome(string email);
}

sealed class NotificationService(IEmailSender sender) : INotificationService
{
	public void SendWelcome(string email) =>
		sender.Send(email, "Добро пожаловать!", "Рады видеть вас в системе.");
}
```

At this step, the generator will create a partial class `Composition` and a property `Notifications` that returns the assembled graph. It's very simple.

See also real examples in the repository:

- [Example with Schrödinger's cat](https://github.com/DevTeam/Pure.DI?tab=readme-ov-file#schr%C3%B6dingers-cat-demonstrates-how-it-all-works-)
- [Example of binding abstractions to implementations](https://github.com/DevTeam/Pure.DI/blob/master/readme/injections-of-abstractions.md)
- [Example of automatic binding (auto‑bindings)](https://github.com/DevTeam/Pure.DI/blob/master/readme/auto-bindings.md)
- [How composition roots work](https://github.com/DevTeam/Pure.DI/blob/master/readme/composition-roots.md)

---

## Composition class

From an architectural perspective, `Composition` is the place where:

- **bindings** are defined: "use `SmtpEmailSender` instead of `IEmailSender`";
- **roots** are defined: "externally we expose only `INotificationService Notifications`";
- dependency **lifetimes** are configured;
- (optionally) **hints** are set for the generator.

An important point: Pure.DI does not "hide" somewhere at runtime. A **regular class** appears in the project that can be opened, debugged, and studied in detail.

See also:

- [Example of composition roots and Resolve methods](https://github.com/DevTeam/Pure.DI/blob/master/readme/resolve-methods.md)
- [How to disable Resolve method generation](https://github.com/DevTeam/Pure.DI/blob/master/readme/resolve-hint.md)

---

## Bindings: how to link interfaces with implementations

Basic binding form:

```c#
.Bind<IContract>().As(Lifetime).To<Implementation>()
```

This is exactly what you're used to seeing in DI containers, but the result will be generated code.

Real scenario: two delivery methods — courier and parcel locker. Business code depends on `IDeliveryService`, and the specific implementation is chosen at the composition level - infrastructure specialized for creating objects of specific types.

```c#
using Pure.DI;

DI.Setup(nameof(Composition))
	.Bind<IDeliveryService>().To<CourierDelivery>()
	.Bind<IOrderService>().To<OrderService>()
	.Root<IOrderService>("Orders");

var composition = new Composition();
composition.Orders.PlaceOrder();

interface IDeliveryService
{
	void Ship();
}

sealed class CourierDelivery : IDeliveryService
{
	public void Ship() => Console.WriteLine("Едет курьер");
}

interface IOrderService
{
	void PlaceOrder();
}

sealed class OrderService(IDeliveryService delivery) : IOrderService
{
	public void PlaceOrder()
	{
		// business logic...
		delivery.Ship();
	}
}
```

See also:

- [Example of binding abstractions to implementations](https://github.com/DevTeam/Pure.DI/blob/master/readme/injections-of-abstractions.md)

---

## Automatic bindings: convenient, but be vigilant

Pure.DI can create **non-abstract** types without explicit bindings. This is convenient for small applications, utilities, and demos: you declare a root, and dependencies are "pulled" automatically.

```c#
using Pure.DI;

DI.Setup(nameof(Composition))
	.Root<ReportService>("Reports");

var composition = new Composition();
var service = composition.Reports;

sealed class FileSystem;

sealed class ReportService(FileSystem fs);
```

However, in a real application, automatic binding quickly hits architectural limitations:

- harder to follow the dependency inversion principle (you start depending on concrete classes);
- harder to manage different implementations of the same abstractions and decorators.

Therefore, the typical recommendation is to **depend on abstractions** and explicitly bind them to implementations.

See also:

- [Example of automatic binding](https://github.com/DevTeam/Pure.DI/blob/master/readme/auto-bindings.md)

---

## Factories: when you need more than just calling a constructor

Sometimes an object cannot be created only through a constructor:

- manual initialization is needed (connect to DB, warm up cache, assemble config);
- object is created through a third-party API;
- additional verification/configuration is needed.

In Pure.DI, there is a binding to a factory for this:

```c#
using Pure.DI;

DI.Setup(nameof(Composition))
	.Bind<IDbConnection>().To<DbConnection>(ctx =>
	{
		// Inject() creates DbConnection with all dependencies (if any)
		ctx.Inject(out DbConnection conn);
		conn.Open();
		return conn;
	})
	.Bind<IRepository>().To<Repository>()
	.Root<IRepository>("Repo");

var composition = new Composition();
var repo = composition.Repo;

interface IDbConnection;

sealed class DbConnection : IDbConnection
{
	public void Open() { /* ... */ }
}

interface IRepository;

sealed class Repository(IDbConnection connection) : IRepository;
```

And if the factory is simple, you can describe it briefly — lambda parameters will be injected automatically:

```c#
using Pure.DI;

DI.Setup(nameof(Composition))
	.Bind().To(() => DateTimeOffset.UtcNow)
	.Bind<ILogFileName>().To((DateTimeOffset now) =>
		new LogFileName($"app-{now:yyyy-MM-dd}.log"))
	.Root<ILogFileName>("LogName");

var composition = new Composition();
Console.WriteLine(composition.LogName.Value);

interface ILogFileName
{
	string Value { get; }
}

sealed record LogFileName(string Value) : ILogFileName;
```

See also:

- [Factory with manual initialization](https://github.com/DevTeam/Pure.DI/blob/master/readme/factory.md)
- [Simplified factory (dependencies as lambda parameters)](https://github.com/DevTeam/Pure.DI/blob/master/readme/simplified-factory.md)

---

## Simplified bindings: when the contract can be determined automatically

In large projects, the routine is not DI as a concept, but the number of lines of its configuration. Pure.DI allows simplifying the records.

`Bind().To<Implementation>()` binds **the type itself** and its **non-special** abstractions that it directly implements.

```
DI.Setup(nameof(Composition))
	.Bind().To<SmtpEmailSender>()
	.Root<IEmailSender>("Sender");

var composition = new Composition();
composition.EmailSender.Send("...");

interface IEmailSender { ... }

class SmtpEmailSender : IEmailSender { ... }
```

This is useful when a class implements one or more "regular" contracts, and you don't want to list them manually.

There are variants of "binding by lifetime" through methods `Singleton<>()`, `PerResolve<>()`, etc.

```
DI.Setup(nameof(Composition))
	.Singleton<SystemClock>()
	.Transient<EmailSender>()
	.Root<IEmailSender>("Sender");

var composition = new Composition();
composition.EmailSender.Send("...");

interface ISystemClock;

sealed class SystemClock: ISystemClock;

interface IEmailSender { ... }

class EmailSender(ISystemClock clock): IEmailSender { ... }
```

See also:

- [Simplified bindings without specifying a contract](https://github.com/DevTeam/Pure.DI/blob/master/readme/simplified-binding.md)
- [Short binding methods with lifetime in the name](https://github.com/DevTeam/Pure.DI/blob/master/readme/simplified-lifetime-specific-bindings.md)

---

## Tags: multiple implementations of one contract

In a real application, there are often several implementations of one interface:

- different logging methods (file, console, telemetry);
- different API clients (public/internal);
- different payment providers (credit card/bank transfer/gift certificates).

Tags allow you to select an implementation **explicitly** without creating additional interfaces:

```c#
using Pure.DI;

DI.Setup(nameof(Composition))
	.Bind<IPaymentClient>("Sandbox").To<SandboxPaymentClient>()
	.Bind<IPaymentClient>("Prod").To<ProdPaymentClient>()
	.Bind<CheckoutService>().To<CheckoutService>()
	.Root<CheckoutService>("Checkout");

var composition = new Composition();
var root = composition.Checkout;

interface IPaymentClient;

sealed class SandboxPaymentClient : IPaymentClient;

sealed class ProdPaymentClient : IPaymentClient;

sealed class CheckoutService(
	[Tag("Prod")] IPaymentClient client)
{
	// ...
}
```

See also:

- [Example of using tags](https://github.com/DevTeam/Pure.DI/blob/master/readme/tags.md)
- [Smart tags (without strings and typos)](https://github.com/DevTeam/Pure.DI/blob/master/readme/smart-tags.md)

---

## Lifetimes

Pure.DI supports familiar lifetimes. Important: this is not a "DI feature", but a tool for resource management and state isolation.

Below is a simplified cheat sheet (in terms of behavior):

| Lifetime | When created | When the same instance is reused |
|---|---|---|
| `Transient` | from scratch each time | never |
| `Singleton` | once per `Composition` instance | everywhere within a single composition |
| `PerResolve` | on each access to a root | within one root |
| `PerBlock` | inside a construction block | allows reducing the number of instances (details depend on the graph) |
| `Scoped` | on a scope-composition | within one scope |

Practical guideline:

- `Transient` — safe default for most stateless services.
- `Singleton` — for caches/pools/metadata, but requires **thread safety** and carefulness.
- `Scoped` — for "request resources": DbContext/UnitOfWork/RequestTelemetry.

See also:

- [Transient: new object each time](https://github.com/DevTeam/Pure.DI/blob/master/readme/transient.md)
- [Singleton: one object per Composition](https://github.com/DevTeam/Pure.DI/blob/master/readme/singleton.md)
- [PerResolve: one object per root](https://github.com/DevTeam/Pure.DI/blob/master/readme/perresolve.md)
- [PerBlock: reduces number of instances](https://github.com/DevTeam/Pure.DI/blob/master/readme/perblock.md)
- [Scope/Scoped: "per request"](https://github.com/DevTeam/Pure.DI/blob/master/readme/scope.md)

---

## Composition arguments: how to pass external state without global static variables

DI combines poorly with passing additional external state (some data) to the created object. But in Pure.DI, **Composition arguments** turn external state into dependencies available in the graph like any others:

```c#
using Pure.DI;

DI.Setup(nameof(Composition))
	.Bind<IApiClient>().To<ApiClient>()
	.Root<IApiClient>("Api")
	.Arg<string>("baseUrl")
	.Arg<string>("token", "api token");

var composition = new Composition(
	baseUrl: "https://api.company.com",
	token: "secret");

var api = composition.Api;

interface IApiClient;

sealed class ApiClient(
	string baseUrl,
	[Tag("api token")] string token) : IApiClient;
```

See also:

- [Example of composition arguments](https://github.com/DevTeam/Pure.DI/blob/master/readme/composition-arguments.md)

---

## Root arguments: when parameters are needed only at one entry point

Sometimes values should be passed **not to Composition**, but to a specific root: for example, a command handler receives `userId`, and the rest of the composition does not depend on it.

For this, there are **Root arguments**. Such a root becomes a method:

```c#
using Pure.DI;

DI.Setup(nameof(Composition))
	// RootArg is incompatible with Resolve methods (better to disable)
	.Hint(Hint.Resolve, "Off")
	.RootArg<Guid>("userId")
	.Bind<IUserService>().To<UserService>()
	.Root<IUserService>("CreateUserService");

var composition = new Composition();
var service = composition.CreateUserService(userId: Guid.NewGuid());

interface IUserService;

sealed class UserService(Guid userId) : IUserService;
```

See also:

- [Example of root arguments](https://github.com/DevTeam/Pure.DI/blob/master/readme/root-arguments.md)

---

## Generation and usage: roots as properties (and why this is convenient)

A key feature of Pure.DI — composition roots become **regular** class members, properties or methods.

This changes the usage style:

- the root is easy to substitute in UI-binding (WPF/MAUI/Avalonia), because it's a property/method;
- IDE and compiler help with navigation;
- easy to document;
- a dependency only participates in composition if you explicitly declared a root for it.

If desired, you can resolve dependencies through `Resolve`, but it's better to perceive this as "familiar, but not always rational approach".

See also:

- [Resolve methods and their limitations](https://github.com/DevTeam/Pure.DI/blob/master/readme/resolve-methods.md)

---

## Resolve methods: convenient, but it's Service Locator

Pure.DI can generate `Resolve<T>()` and `Resolve(Type)` methods — this is sometimes useful, for example, for integration with code that prefers to work with classic DI containers. But essentially this is **Service Locator**, with all the classic drawbacks:

- API allows "getting anything" — this is loss of control;
- there is a risk of runtime exceptions, for example, when there is no corresponding binding to a specific (non-abstract) type;
- code becomes harder to analyze and test.

If you want a strict and clean architecture, `Resolve` is usually disabled:

```c#
using Pure.DI;

DI.Setup(nameof(Composition))
	.Hint(Hint.Resolve, "Off")
	// ...
	.Root<App>("App");
```

See also:

- [Resolve hint (enable/disable)](https://github.com/DevTeam/Pure.DI/blob/master/readme/resolve-hint.md)

---

## Dependency injection methods

In most projects, **constructor injection** is sufficient — it's simpler, safer, and well-supported by analyzers/IDE.

But sometimes other options are convenient (for example, when "building up" an object created externally).

Pure.DI supports:

- injection through constructor;
- injection through properties;
- injection through fields;
- injection through methods.

Properties, fields, and methods just need to be marked with the `[Dependency]` attribute or others like `[Ordinal]`, `[Tag]`, `[Type]`, `[Inject]`. But you can always easily extend them with your own attributes, thereby making your code completely independent of DI.

See also:

- [Injection through properties](https://github.com/DevTeam/Pure.DI/blob/master/readme/property-injection.md)
- [Injection through fields](https://github.com/DevTeam/Pure.DI/blob/master/readme/field-injection.md)
- [Injection through methods](https://github.com/DevTeam/Pure.DI/blob/master/readme/method-injection.md)

---

## Builders (BuildUp): when it's not possible to control object creation

Sometimes an object appears "from outside":

- deserialization (JSON → object);
- plugins/scripts;
- game entities created by the engine;
- UI elements created by the framework.

In such cases, the **BuildUp** pattern is useful: you already have an instance, and you want to "add" dependencies to it through fields/properties/methods marked with injection attributes mentioned above.

Pure.DI can generate **builders** for types derived from a base `T`, known at compile time.

See also:

- [Builders: building up existing objects](https://github.com/DevTeam/Pure.DI/blob/master/readme/builders.md)
- [Builder with arguments](https://github.com/DevTeam/Pure.DI/blob/master/readme/builder-with-arguments.md)

---

## Generics: why Pure.DI offers marker types instead of "open generics"

Classic DI containers often register "open generics" like `IRepository<> → Repository<>`. This is convenient, but in complex graphs, ambiguity arises: how exactly to match type arguments, especially when interfaces and implementations use different orders or names of parameters.

In Pure.DI, instead of "open generics", an approach with **marker types** (for example, `TT`, `TT1`, `TT2`) is used. This makes matching absolutely **precise**.

In practice, it looks like this:

```c#
using Pure.DI;

DI.Setup(nameof(Composition))
	.Bind<IRepository<TT>>().To<Repository<TT>>()
	.Bind<IDataService>().To<DataService>()
	.Root<IDataService>("Data");

var composition = new Composition();
var data = composition.Data;

interface IRepository<T>;

sealed class Repository<T> : IRepository<T>;

sealed record User;

sealed record Order;

interface IDataService;

sealed class DataService(
	IRepository<User> users,
	IRepository<Order> orders) : IDataService;
```

See also:

- [Generics and marker types](https://github.com/DevTeam/Pure.DI/blob/master/readme/generics.md)
- [Complex generics](https://github.com/DevTeam/Pure.DI/blob/master/readme/complex-generics.md)

---

## On-demand injection: Func, Lazy, and factories

Sometimes a service needs to create dependencies **not immediately**, but as needed:

- a "heavy" dependency (driver initialization, data warming);
- many instances of one type (list items, game entities);
- dependencies with runtime parameters.

Pure.DI supports factory delegates (`Func<T>`, `Func<TArg, T>`, `Func<TArg1, TArg2, ..., T>`) and `Lazy<T>` out of the box.

Example: a service generates several one-time tokens, each time creating a new object:

```c#
using Pure.DI;

DI.Setup(nameof(Composition))
	.Bind<ITokenGenerator>().To<TokenGenerator>()
	.Bind<ITokenService>().To<TokenService>()
	.Root<ITokenService>("Tokens");

var composition = new Composition();
composition.Tokens.IssueBatch(3);

interface ITokenGenerator
{
	string Next();
}

sealed class TokenGenerator : ITokenGenerator
{
	public string Next() => Guid.NewGuid().ToString("N");
}

interface ITokenService
{
	void IssueBatch(int count);
}

sealed class TokenService(Func<ITokenGenerator> generatorFactory) : ITokenService
{
	public void IssueBatch(int count)
	{
		for (var i = 0; i < count; i++)
		{
			var gen = generatorFactory(); // new instance on demand
			Console.WriteLine(gen.Next());
		}
	}
}
```

See also:

- [Injection on demand (Func<T>)](https://github.com/DevTeam/Pure.DI/blob/master/readme/injection-on-demand.md)
- [Injection on demand with arguments (Func<TArg, T>)](https://github.com/DevTeam/Pure.DI/blob/master/readme/injections-on-demand-with-arguments.md)
- [BCL: Func<T>](https://github.com/DevTeam/Pure.DI/blob/master/readme/func.md)
- [BCL: Lazy<T>](https://github.com/DevTeam/Pure.DI/blob/master/readme/lazy.md)

---

## BCL support: when standard types "just work"

In traditional containers, many tasks are solved by special extensions. In Pure.DI, a significant part of useful types from the .NET Base Class Library (BCL) is supported "out of the box".

From frequently used:

- `Func<T>`, `Func<TArg1, T>`, `Func<TArg1, TArg2, ..., T>` — factories;
- `Lazy<T>` — lazy creation;
- `IEnumerable<T>`, arrays — collections of dependencies;
- `Task<T>`, `ValueTask<T>` — for async roots;
- `IServiceProvider`/Service collection scenarios (when integration with the Microsoft DI ecosystem is needed).

See also:

- [BCL: Enumerable](https://github.com/DevTeam/Pure.DI/blob/master/readme/enumerable.md)
- [BCL: Enumerable generics](https://github.com/DevTeam/Pure.DI/blob/master/readme/enumerable-generics.md)
- [BCL: Task](https://github.com/DevTeam/Pure.DI/blob/master/readme/task.md)
- [BCL: ValueTask](https://github.com/DevTeam/Pure.DI/blob/master/readme/valuetask.md)
- [BCL: Service provider](https://github.com/DevTeam/Pure.DI/blob/master/readme/service-provider.md)
- [BCL: Service collection](https://github.com/DevTeam/Pure.DI/blob/master/readme/service-collection.md)

---

## Decorators and interception: adding logging and metrics without rewriting code

Interception in Pure.DI in the basic variant works well with the **Decorator** pattern: we "wrap" the implementation in another implementation of the same interface.

Real example: wrap `IOrderService` in a logging decorator.

```c#
using Pure.DI;

DI.Setup(nameof(Composition))
	// "base" — base implementation
	.Bind("base").To<OrderService>()
	// Decorator uses "base"
	.Bind<IOrderService>().To<LoggingOrderService>()
	.Bind<ILogger>().To<ConsoleLogger>()
	.Root<IOrderService>("Orders");

var composition = new Composition();
composition.Orders.PlaceOrder("ORD-42");

interface ILogger
{
	void Info(string message);
}

sealed class ConsoleLogger : ILogger
{
	public void Info(string message) => Console.WriteLine(message);
}

interface IOrderService
{
	void PlaceOrder(string id);
}

sealed class OrderService : IOrderService
{
	public void PlaceOrder(string id) =>
		Console.WriteLine($"Order {id} placed");
}

sealed class LoggingOrderService(
	ILogger log,
	[Tag("base")] IOrderService inner) : IOrderService
{
	public void PlaceOrder(string id)
	{
		log.Info($"Начинаем оформление {id}");
		inner.PlaceOrder(id);
		log.Info($"Завершили оформление {id}");
	}
}
```

See also:

- [Decorator: basic interception scenario](https://github.com/DevTeam/Pure.DI/blob/master/readme/decorator.md)
- [Interception: additional capabilities](https://github.com/DevTeam/Pure.DI/blob/master/readme/interception.md)

---

## Hints: additional generation settings that help during development

Hints are fine-tuned generator settings of "how exactly to generate code". For basic startup, it's enough to know three:

### ToString: see the dependency graph

You can enable the generation of `ToString()`, which returns a diagram in mermaid format — convenient for review and discussing architecture.

See also:

- [Hint ToString: graph diagram](https://github.com/DevTeam/Pure.DI/blob/master/readme/tostring-hint.md)

### ThreadSafe: disable thread safety if you're sure

By default, generation takes multithreading into account. But sometimes object composition is built strictly in one thread (for example, when starting the application), and you can get slightly higher performance.

See also:

- [Hint ThreadSafe](https://github.com/DevTeam/Pure.DI/blob/master/readme/threadsafe-hint.md)

### OnDependencyInjection: point for "dynamic interception"

If it's necessary to centrally "track" or modify the injection process (logging, metrics, control), you can enable the generation of the partial method `OnDependencyInjection`.

See also:

- [OnDependencyInjection (wildcard)](https://github.com/DevTeam/Pure.DI/blob/master/readme/ondependencyinjection-wildcard-hint.md)
- [OnDependencyInjection (regexp)](https://github.com/DevTeam/Pure.DI/blob/master/readme/ondependencyinjection-regular-expression-hint.md)

---

## Practical recommendations for implementing Pure.DI in a project

For successful implementation, the following sequence is recommended:

1) **Define composition roots**. Usually these are application "entry points": `App`, `MainController`, `MessageHandler`, `BackgroundWorker`. Minimize their number, ideally to one root.
2) **Reduce object creation to composition**. In other modules, let only business code and contracts remain. The fewer objects are created manually, the better.
3) **Start with constructor injection**. Use other types of injection as a tool for build-up and integration.
4) **Use auto-binding with caution**: excellent for demos, often unnecessary for good architecture.
5) **Don't abuse Singleton**. And if it's necessary — ensure thread safety and correct resource release. Remember about dependency capture - if a Singleton uses some dependency with a different lifetime, that dependency will also be a Singleton.
6) **Don't make using `Resolve()` methods the main model**. Composition roots are your path to clean architecture and calm updates in production environment.
7) **Keep constructors simple**: without heavy logic and I/O operations. This will allow you not to worry about composition size and not force you to use various tricks like delayed object creation. If necessary — use factories or separate initialization logic.
8) **Don't get carried away with factories**: factories are always additional logic that requires support. Use regular bindings to implementations.

---

## Conclusion

Pure.DI makes DI predictable and transparent:

- the compiler guarantees the correctness of object composition;
- object creation turns into readable code;
- lifetimes and tags are described declaratively;
- the application remains as fast and predictable as possible.

If the idea of "Pure DI" is close to you, not "runtime DI container", Pure.DI is worth trying on at least one service or module — usually after that, it's hard to return to the "black DI box".
