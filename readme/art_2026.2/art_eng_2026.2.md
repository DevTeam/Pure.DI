# What's new in Pure.DI: union types, interface generation, and DI without unnecessary allocations

Pure.DI is a dependency injection code generator that runs at compile time. It advances the idea of "pure DI": instead of a container and reflection, you get regular C# code that creates object compositions. This article covers the new features introduced in releases 2.3.5 through 2.5.1, from union types as DI contracts to dependency injection scenarios without unnecessary allocations.

The key benefits of Pure.DI are:

- **Zero overhead**: the generator produces regular, statically typed C# code without runtime dependency lookup or reflection
- **Compile-time validation**: DI graph configuration errors, cyclic dependencies, and missing bindings are detected during compilation
- **Broad compatibility**: from .NET Framework 2.0 to modern .NET versions, Unity, Native AOT, and other platforms
- **Transparency**: you can always inspect, debug, and understand the generated code

Six releases have shipped since the previous article, so this one is divided into two main parts.

**Part 1. New features:**

- union types as DI contracts
- generating interfaces from classes (`[GenerateInterface]`)
- implementation-level attribute bindings (`[Bind]`, `[Type]`, `[Tag]`, `[Lifetime]`)
- `[Export]`: class members as dependency sources
- custom binding attributes
- scopes: `SetupScope`, per-scene Unity scopes, and `CreateScope()` for Microsoft DI
- complete nullable reference type support
- `TryBuildUp` for safe object build-up
- support for new C# 13/14 features: `OverloadResolutionPriority` and partial constructors

**Part 2. Performance - DI on hot paths:**

- `Span<T>`, `ReadOnlySpan<T>`, and ref struct dependencies
- factories for stack-only values (`allows ref struct`)
- zero-copy parsing and method injection on hot paths
- diagnostics for stack-only values
- non-boxing union results
- a catalog of high-performance examples: ArrayPool, object pools, closure-free factories, `ValueTask<T>` roots, and `ThreadSafe = Off`
- leaner generated code

Other articles about Pure.DI:

- [Pure.DI helps keep DI pure](https://habr.com/ru/articles/765112/) - recommended as an introduction to the core idea
- [Pure.DI v2.1](https://habr.com/ru/articles/795809/)
- [What's new in Pure.DI at the beginning of 2024](https://habr.com/ru/articles/808297/)
- [What's new in Pure.DI at the end of 2024](https://habr.com/ru/articles/868744/)
- [Pure.DI in Unity](https://habr.com/ru/articles/876712/)

---

## Part 1. New features

### Union types as DI contracts

Preview versions of C# introduced union types: a type whose value can be one of several predefined case types. Pure.DI can now use a union as a **DI contract**. A case implementation is implicitly converted to the union, and the generator builds the entire dependency graph of the selected case behind the union contract. Unlike an interface, case types do not need to share an abstraction. They can even be classes from unrelated third-party SDKs that you cannot modify.

```csharp
DI.Setup(nameof(Composition))
    .Bind<IReceiptService>().To<ReceiptService>()
    // The composition decides which payment gateway
    // stands behind the union contract
    .Bind<PaymentGateway>().To<StripeGateway>()
    .Root<CheckoutService>("Checkout");

// The case types are independent classes with no common interface,
// and each one has its own dependencies
class StripeGateway(StripeApiClient api)
{
    public string Charge(int amountInCents) =>
        api.Post($"charge {amountInCents}");
}

class BankGateway(BankAccount account)
{
    public string Transfer(int amountInCents) =>
        $"bank transfer {amountInCents} from {account.Iban}";
}

// The case types are implicitly convertible to the union,
// and Pure.DI uses that conversion
union PaymentGateway(StripeGateway, BankGateway);

// The service depends on the union and handles each case explicitly
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

Switching providers is a one-line change: `.Bind<PaymentGateway>().To<BankGateway>()`. Important details:

- when there is no explicit union binding but exactly one registered binding can be converted to the union, Pure.DI resolves the union through that single case automatically
- when several case bindings are applicable, Pure.DI reports `DIE050` with the candidate list and binding locations
- union types do **not participate** in auto-binding, so Pure.DI cannot accidentally create an "empty" union
- the case binding retains ownership of its lifetime and disposal behavior; `Singleton`, `Scoped`, and `PerResolve` work as usual
- collections of cases are assembled through `Tag.Unique`; consumers can request `IEnumerable<PaymentGateway>`, arrays, `ReadOnlySpan<>`, and other supported collection types

[Union types: case implementations behind a union contract](https://github.com/DevTeam/Pure.DI/blob/master/readme/union-types.md)

Union type support requires a preview language version and .NET 11 Preview 5 or later, but it is already available to try.

---

### Generating interfaces from classes

The classic "class plus matching interface" pair is some of the dullest boilerplate in DI-based applications. An interface can now be generated from its implementation: declare an empty `partial interface` and mark the class with `[GenerateInterface]`. Special thanks to [Adam Hathcock](https://github.com/adamhathcock) for designing and implementing this feature.

```csharp
DI.Setup(nameof(Composition))
    .Bind().To<EmailSender>()
    .Root<App>(nameof(App));

// Pure.DI generates the interface body for this empty declaration
public partial interface IEmailSender;

[GenerateInterface]
public class EmailSender : IEmailSender
{
    public string Provider => "smtp";

    public string Send(string address) => $"sent:{address}";
}

// The consumer depends on an abstraction nobody had to write manually
public class App(IEmailSender sender);
```

The feature goes beyond simple mirroring:

- the [interface name, namespace, and accessibility are configurable](https://github.com/DevTeam/Pure.DI/blob/master/readme/customize-the-generated-interface.md) through named attribute arguments
- [one class can generate several interfaces](https://github.com/DevTeam/Pure.DI/blob/master/readme/generate-several-interfaces-from-one-class.md), with members distributed between contracts by attributes
- [`[IgnoreInterface]`](https://github.com/DevTeam/Pure.DI/blob/master/readme/ignore-members-in-the-generated-interface.md) excludes members from the contract
- [generic members, nullable annotations, and events are preserved](https://github.com/DevTeam/Pure.DI/blob/master/readme/generate-interfaces-with-generics.md), along with XML documentation comments

[Generating an interface from a class](https://github.com/DevTeam/Pure.DI/blob/master/readme/generate-an-interface-from-a-class.md)

---

### Implementation-level attribute bindings

Version 2.5.0 redesigned the attribute binding model. This is a breaking change, with migration details below. `[Bind]` can now be declared directly on the implementation type and specify its contract, lifetime, and tag. Implementations can describe their own bindings, keeping composition setup concise:

```csharp
DI.Setup(nameof(Composition))
    // The setup contains only the root; the attribute defines the binding
    .Root<IMessageWriter>("Writer", "console");

[Bind(typeof(IMessageWriter), Lifetime.Singleton, "console")]
class ConsoleMessageWriter : IMessageWriter
{
    public void Write(string message) => Console.WriteLine(message);
}
```

Attributes inside one `[...]` group are merged into a single binding, while separate groups create independent bindings. This is useful when one implementation plays several roles with different contracts, lifetimes, and tags:

```csharp
// Binding group #1: payment pipeline participant
[Bind(typeof(IPaymentProcessor), Lifetime.Singleton, "audit")]
// Binding group #2: the same adapter as an audit sink with its own lifetime
[Bind(typeof(IPaymentAuditSink), Lifetime.Transient, "operations")]
class PaymentAuditAdapter(IEventStore eventStore) :
    IPaymentProcessor,
    IPaymentAuditSink;
```

In addition to `[Bind]`, the focused `[Type]`, `[Tag]`, and new `[Lifetime]` attributes can be combined. Generic implementations support marker contracts such as `typeof(IBox<TT>)`.

[BindAttribute on an implementation](https://github.com/DevTeam/Pure.DI/blob/master/readme/bind-attribute.md)

[Bind attribute groups: several roles for one adapter](https://github.com/DevTeam/Pure.DI/blob/master/readme/bind-attribute-groups.md)

---

### `[Export]`: class members as dependency sources

Previously, member-level dependency sources used `[Bind]`. That role now belongs to the new `[Export]` attribute, making the semantics unambiguous: `[Bind]` defines bindings, while `[Export]` defines dependency sources.

```csharp
DI.Setup(nameof(Composition))
    .Bind().As(Lifetime.Singleton).To<DeviceFeatureProvider>()
    .Bind().To<PhotoService>()
    .Root<IPhotoService>("PhotoService");

class DeviceFeatureProvider
{
    // This property is a dependency source
    [Export] public IGps Gps { get; } = new Gps();

    [Export] public ICamera Camera { get; } = new Camera();
}

class PhotoService(IGps gps, Func<ICamera> cameraFactory) : IPhotoService;
```

`[Export]` accepts optional `lifetime` and `tags` values, so an exported member behaves like a binding source:

```csharp
class GraphicsAdapter
{
    [Export(lifetime: Lifetime.Singleton, tags: ["HighPerformance"])]
    public IGpu HighPerfGpu { get; } = new DiscreteGpu();
}

class RayTracer([Tag("HighPerformance")] IGpu gpu) : IRenderer;
```

Migration from 2.4.x is straightforward: replace member-level `[Bind]` with `[Export]`, and replace `RootKinds.Exposed` with `RootKinds.Exported`.

[ExportAttribute: members as dependency sources](https://github.com/DevTeam/Pure.DI/blob/master/readme/export-attribute.md)

[Export with a lifetime and tag](https://github.com/DevTeam/Pure.DI/blob/master/readme/export-attribute-with-lifetime-and-tag.md)

---

### Custom binding attributes

If you do not want to annotate implementations with Pure.DI's built-in attributes, declare a **custom** attribute and register it in the setup. `TypeAttribute<T>()`, `TagAttribute<T>()`, and the new `LifetimeAttribute<T>()` tell the generator which constructor arguments contain the contract, tag, and lifetime. The version below uses `Pure.DI.Lifetime`, so the implementation assembly still needs a reference to the Pure.DI API:

```csharp
DI.Setup(nameof(Composition))
    // Register the custom attribute as a binding metadata source
    .TypeAttribute<ServiceAttribute<TT>>()
    .LifetimeAttribute<ServiceAttribute<TT>>()
    .TagAttribute<ServiceAttribute<TT>>(1)
    .Root<IMessageWriter>("Writer", "console");

// The attribute can live with the implementations without depending
// on a particular built-in Pure.DI binding attribute
[AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
class ServiceAttribute<T> : Attribute
{
    public ServiceAttribute(
        Lifetime lifetime = Lifetime.Transient,
        object? tag = null)
    {
    }
}

[Service<IMessageWriter>(Lifetime.Singleton, "console")]
class ConsoleMessageWriter : IMessageWriter;
```

Custom attributes follow the same group merging rules as the built-in `Bind`, `Type`, `Tag`, and `Lifetime` attributes. Duplicate lifetime metadata within one attribute group is reported as an error.

[Custom binding attribute](https://github.com/DevTeam/Pure.DI/blob/master/readme/custom-bind-attribute.md)

---

### Scopes: `SetupScope`

A common requirement is a lifetime tied to a request or operation: a request context, unit of work, or telemetry scope. A new generated method, whose name is configured through `ScopeMethodName`, connects a new composition instance to its parent scope. `Scoped` instances are unique within the scope and are disposed with it, while a regular `Singleton` remains shared.

```csharp
var composition = new Composition();

// Request #1: its own scope and RequestContext
using (var request = Composition.SetupScope(composition, new Composition()))
{
    var checkout = request.RequestRoot;
    // RequestContext is shared within this scope
    // and disposed when the scope ends
}

partial class Composition
{
    static void Setup() => DI.Setup()
        .Hint(Hint.ScopeMethodName, "SetupScope")
        // Per-request lifetime
        .Bind().As(Scoped).To<RequestContext>()
        .Bind().As(Singleton).To<IdGenerator>()
        .Bind().To<CheckoutService>()
        .Root<ICheckoutService>("RequestRoot");
}
```

The method name can match the domain: `CreateScope`, `BeginRequest`, or `OpenSession`. Scopes can also be created through factory methods, and Pure.DI validates that the parent and child scopes are different, preventing accidental self-nesting.

[Scope setup method: a request scope without a wrapper class](https://github.com/DevTeam/Pure.DI/blob/master/readme/scope-setup-method.md)

---

### Platform scopes: Unity scenes and Microsoft DI

The same scope model maps naturally to platform scenarios. In Unity, each loaded scene gets its own scope: `Scoped` services are shared within the scene and isolated between scenes, while `Singleton` services are shared by the entire application. Unity creates `MonoBehaviour` objects itself, and Pure.DI builds them up through builders:

```csharp
public partial class Scope : MonoBehaviour
{
    [SerializeField] ClockConfig clockConfig;

    void Setup() => DI.Setup()
        .Hint(Hint.ScopeMethodName, "SetupScope")
        .Bind().To(() => clockConfig)
        .Bind<IClockService>().As(Singleton).To<ClockService>()
        .Bind<IClockSession>().As(Scoped).To<ClockSession>()
        // Unity creates MonoBehaviour; Pure.DI builds it up
        .Builders<MonoBehaviour>();
}
```

[Unity scene scopes: one scope per scene](https://github.com/DevTeam/Pure.DI/blob/master/readme/unity-scene-scopes.md)

For integration with `Microsoft.Extensions.DependencyInjection`, a composition can implement `IServiceScopeFactory` and `IServiceScope`. Every `composition.CreateScope()` call creates a new scope, and tags can be mapped to keyed services. This model is compatible with Microsoft DI scoped semantics for explicitly declared composition roots. Only roots registered through `Root(...)` or `RootBind()` can be resolved through `IServiceProvider`.

[Service provider with Microsoft DI-compatible scopes](https://github.com/DevTeam/Pure.DI/blob/master/readme/service-provider-with-scope.md)

---

### Nullable reference types end to end

Version 2.4.0 introduced complete support for nullable reference types. Annotations are preserved while reading contracts, building the graph, and generating code. This is a breaking change: code that relied on automatic null checks for nullable arguments may need adjustment.

```csharp
DI.Setup(nameof(Composition))
    .Bind<IDatabase>().To<Database>()
    .Bind<IReportService>().To<ReportService>()
    // Nullable arguments no longer receive null checks:
    // null is passed through as intended
    .Arg<string?>("defaultTitle", "title")
    .RootArg<string?>("connectionString", "connection")
    .Root<IReportService>("CreateReportService");

var composition = new Composition(defaultTitle: null);
var reportService = composition.CreateReportService(connectionString: null);

class ReportService(
    [Tag("title")] string? defaultTitle,
    [Tag("connection")] string? connectionString,
    IDatabase? optionalDatabase) : IReportService;
```

The practical effects are:

- a non-null binding can satisfy a nullable dependency, which is useful for optional constructor parameters, nullable factory results, and collection elements
- `T?` means that the consumer can handle null, but it does not suppress a graph error when no binding exists
- for generic contracts with nullable arguments, prefer `where T : class?` over `where T : class` to avoid compiler warnings
- new warnings highlight ambiguous nullable roots in `Resolve` methods

[Nullable reference types: examples and recommendations](https://github.com/DevTeam/Pure.DI/blob/master/readme/nullable-reference-types.md)

---

### `TryBuildUp`: safe object build-up

Builders complete objects created by external systems such as UI frameworks, serializers, or Unity. Strict `BuildUp` throws `ArgumentException` when the runtime subtype is unknown to the composition. Pure.DI now generates a safe `TryBuildUp` method alongside `BuildUp`, avoiding that exception:

```csharp
DI.Setup(nameof(Composition))
    .Bind().To(Guid.NewGuid)
    .Bind().To<PlutoniumBattery>()
    // Generate a builder for every IRobot subtype matching *Bot
    .Builders<IRobot>("BuildUp", filter: "*Bot");

var composition = new Composition();

// Known type: build-up succeeds
var cleaner = composition.BuildUp(new CleanerBot());

// Unknown subtype: graceful fallback instead of an exception
var externalRobot = new ExternalRobot();
if (!composition.TryBuildUp(externalRobot))
{
    // The object remains untouched; handle the situation here
}
```

This is especially useful when objects arrive from outside and their concrete runtime type is not known in advance: deserialization, plugins, and Unity scene objects.

[Builders and TryBuildUp](https://github.com/DevTeam/Pure.DI/blob/master/readme/builders.md)

---

### New C# 13/14 features: partial constructors and `OverloadResolutionPriority`

C# 14 allows a constructor declaration, or contract, to be separated from its body. For example, the contract with DI attributes can live in one part of a partial class while the logic lives in another, possibly generated, part. Pure.DI sees the combined constructor and handles it like a regular one:

```csharp
partial class AuditSink : AuditSinkBase
{
    // The defining declaration participates in constructor selection,
    // and parameter attributes are combined with the implementation
    public partial AuditSink(
        [Tag("durable")] IAuditTransport transport,
        AuditSinkOptions options);
}

partial class AuditSink
{
    // Place the base(...) or this(...) initializer on the implementation
    public partial AuditSink(
        IAuditTransport transport,
        AuditSinkOptions options)
        : base(transport)
    {
        BatchSize = options.BatchSize;
    }
}
```

[Partial constructor injection](https://github.com/DevTeam/Pure.DI/blob/master/readme/partial-constructor-injection.md)

Pure.DI also supports `OverloadResolutionPriorityAttribute`, introduced in C# 13. The attribute raises or lowers the priority of a constructor overload:

```csharp
[method: OverloadResolutionPriority(1)]
class BillingApiClient(ResilientHttpOptions options)
{
    // Keep the legacy constructor for existing callers while newly
    // compiled code, including Pure.DI output, prefers the primary one
    public BillingApiClient(LegacyHttpOptions options)
        : this(new ResilientHttpOptions()) { }
}
```

The rules follow C#: higher values are preferred, unannotated constructors have priority `0`, and negative values lower a constructor's priority. `[Ordinal]` remains Pure.DI's explicit, stronger selection mechanism. `DIW014` concerns a different case, method injection: it warns that a regular generated method call can resolve to another overload with a higher `OverloadResolutionPriority`.

[OverloadResolutionPriority in constructor selection](https://github.com/DevTeam/Pure.DI/blob/master/readme/overload-resolution-priority.md)

---

## Part 2. Performance: DI on hot paths

In Pure.DI, "zero overhead" means regular, statically typed code without a runtime container. Release 2.5.1 goes further: dependency injection now works in stack-oriented code with `Span<T>` and `ref struct` without requiring wrappers or runtime lookup. Potentially unsafe ways of storing stack-only values are diagnosed while the DI graph is built.

### `Span<T>` and `ReadOnlySpan<T>` as dependencies

`Span` injection works like `T[]` injection for immediate use in a constructor or method. For value types, Pure.DI generates `stackalloc`, so the dependency collection itself is assembled without a heap allocation.

```csharp
DI.Setup(nameof(Composition))
    .Bind<Point>('a').To(() => new Point(1, 1))
    .Bind<Point>('b').To(() => new Point(2, 2))
    .Bind<Point>('c').To(() => new Point(3, 3))
    .Bind<IPath>().To<Path>()
    .Root<IPath>("Path");

readonly struct Point(int x, int y);

class Path(ReadOnlySpan<Point> points) : IPath
{
    // The span is allocated on the stack and is inexpensive.
    // A ref struct cannot be stored in a field,
    // but it can be processed immediately in the constructor
    public int PointCount { get; } = points.Length;
}
```

Constructor injection of a `ref struct` into a heap type remains available for compatibility but reports `DIW012`. Method injection, described below, is the preferred approach for new stack-only code.

[Span and ReadOnlySpan](https://github.com/DevTeam/Pure.DI/blob/master/readme/span-and-readonlyspan.md)

---

### Factories for stack-only values

Starting with C# 13, the standard `Func<TArg, TResult>` can accept a `ref struct` when its generic parameters are declared with `allows ref struct`. Compatible reference assemblies from a modern BCL are also required. Pure.DI provides a safe default binding for `Func<ReadOnlySpan<char>, T>`: the span remains local to the synchronous factory invocation and is passed immediately into the object graph being created.

```csharp
DI.Setup(nameof(Composition))
    .Root<Func<ReadOnlySpan<char>, Parser>>("ParserFactory");

var composition = new Composition();
var parser = composition.ParserFactory("Hello".AsSpan());

class Parser
{
    [Ordinal]
    public void Initialize(ReadOnlySpan<char> text) { /* ... */ }
}
```

[Default Func with ReadOnlySpan](https://github.com/DevTeam/Pure.DI/blob/master/readme/default-func-with-readonlyspan.md)

Custom generic delegates with `where T : allows ref struct` are also supported. A stack-only value is passed through `ctx.Override(...)` and consumed immediately. In a thread-safe context, the override and injection stay inside `lock (ctx.Lock)`:

```csharp
delegate bool ParserFactory<in T>(T text)
    where T : allows ref struct;

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
    .Root<ParserFactory<ReadOnlySpan<char>>>("ParserFactory");
```

`allows ref struct` also works in generic roots and root arguments. `Bind<T>()`, `RootBind<T>()`, `Root<T>()`, and `DefaultLifetime<T>()` accept ref-like contracts. `Transient<T>()`, `PerResolve<T>()`, and `PerBlock<T>()` allow ref-like results that remain local while the root is being built. `Singleton<T>()` and `Scoped<T>()` can accept ref-like dependencies in a parameterized factory, but its stored result must be heap-safe; otherwise Pure.DI reports `DIE046`.

[Allows ref struct factory](https://github.com/DevTeam/Pure.DI/blob/master/readme/allows-ref-struct-factory.md)

---

### Zero-copy parsing and method injection on a hot path

When a service has long-lived stable dependencies but receives different data on every call, the per-call value can be passed as a root argument and consumed immediately by an injection method. As a C# 14 bonus, a `byte[]` root argument can satisfy a `ReadOnlySpan<byte>` injection contract. Pure.DI applies the compiler-provided span conversion, allowing the parser to read the caller-owned array without copying it.

```csharp
DI.Setup(nameof(Composition))
    .Bind<IMessageRegistry>().To<MessageRegistry>()
    .Bind<IPacketHandler>().To<PacketHandler>()
    .RootArg<byte[]>("frame")
    .Root<IPacketHandler>("Handle");

var composition = new Composition();
// The caller retains ownership of the array and can return it
// to a pool after the call completes
var packet = composition.Handle(frame);

sealed class PacketHandler(IMessageRegistry registry) : IPacketHandler
{
    [Ordinal(0)]
    public void Decode(ReadOnlySpan<byte> frame)
    {
        var messageId = BinaryPrimitives.ReadUInt16BigEndian(frame);
        // Zero-copy packet parsing...
    }
}
```

[Zero-copy network packet parsing](https://github.com/DevTeam/Pure.DI/blob/master/readme/zero-copy-network-packet-parsing.md)

When the argument itself is stack-only, for example `.RootArg<ReadOnlySpan<char>>("path")`, the generated method root gets a `scoped` parameter. The compiler guarantees that the value does not outlive the current stack frame. Typical use cases include parsers, routers, protocol decoders, and validation pipelines.

[Method injection for a hot path](https://github.com/DevTeam/Pure.DI/blob/master/readme/method-injection-for-a-hot-path.md)

---

### Diagnostics for stack-only values

Stack-oriented code makes subtle mistakes easy. Pure.DI moves these failures to compile time with a dedicated set of diagnostics:

- `DIE046` - a stack-only dependency cannot use a stored lifetime such as Singleton or Scoped
- `DIE047` - a stack-only dependency cannot be injected into a field or property
- `DIE048` - a stack-only implementation cannot be injected through an interface conversion
- `DIE049` - a stack-only dependency cannot be captured by a generated delegate or deferred factory
- `DIW012` - warns about injecting a stack-only dependency into the constructor of a heap type
- `DIW013` - warns about an unsynchronized stack-only override in a thread-safe context

Instead of an obscure C# compiler error such as `CS9244`, Pure.DI can now report a dedicated diagnostic with an explanation and documentation link.

[Pure.DI diagnostics reference](https://github.com/DevTeam/Pure.DI/blob/master/DIAGNOSTICS.md)

---

### Non-boxing union results

Returning to union types from a performance perspective, the compact `union` representation stores its value as `object`, which boxes value-type cases. Hot paths can instead use **custom** union results that store each value-type case in a dedicated field, avoiding boxing, reflection, and wrapper allocations.

```csharp
DI.Setup(nameof(Composition))
    // CacheHit is a value-type case of CacheLookupResult
    .Bind<CacheLookupResult>().To<CacheHit>()
    .Root<CacheLookupResult>("CachedProduct");

var result = new Composition().CachedProduct;
// Allocation-free access to the result
if (result.TryGetValue(out CacheHit hit)) { /* ... */ }

readonly record struct CacheHit(int ProductId, decimal Price);

readonly record struct CacheMiss(int ProductId);

// The custom union stores value-type cases directly
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

    public bool TryGetValue(out CacheHit value)
    {
        value = _hit;
        return _kind == 1;
    }

    // Value is the required general fallback and boxes a value-type case;
    // use it only for diagnostics
    public object? Value => _kind switch
    {
        1 => _hit,
        2 => _miss,
        _ => null
    };
}
```

[Non-boxing union result](https://github.com/DevTeam/Pure.DI/blob/master/readme/non-boxing-union-result.md)

---

### High-performance example catalog

The new capabilities are accompanied by a series of "DI on a hot path" examples:

- **[ArrayPool buffers](https://github.com/DevTeam/Pure.DI/blob/master/readme/arraypool-buffer.md)** - `ArrayPool<T>` is supported out of the box; request `ArrayPool<byte>` like any other dependency and return the buffer to the pool when its owner is disposed through `Owned<T>`
- **[Object pool](https://github.com/DevTeam/Pure.DI/blob/master/readme/object-pool.md)** - a Singleton pool of warm objects plus a short-lived lease for each root call, making reuse explicit and keeping leases within their scope
- **[Factories without closure capture](https://github.com/DevTeam/Pure.DI/blob/master/readme/factory-without-closure-capture.md)** - dependencies are lambda parameters, such as `.To((CurrencyFormatter currency, TaxPolicy tax) => ...)`, instead of captured values, keeping factory code deterministic and allocation-friendly
- **[Struct dependencies](https://github.com/DevTeam/Pure.DI/blob/master/readme/struct-dependency.md)** - a small value-type dependency is created directly and does not require a separate heap allocation
- **[ValueTask roots](https://github.com/DevTeam/Pure.DI/blob/master/readme/valuetask-root.md)** - a root such as `Root<ValueTask<IFeatureSnapshot>>` avoids allocating a `Task<T>` in the synchronous case
- **[ThreadSafe = Off](https://github.com/DevTeam/Pure.DI/blob/master/readme/threadsafe-off-for-single-thread-composition.md)** - when a composition is created and used on one thread, such as in a CLI tool or game-loop initialization phase, `.Hint(Hint.ThreadSafe, "Off")` removes generated synchronization

---

### Code generation optimizations

- the `_lock` field is now generated only when at least one `lock (...)` statement actually references it; previously every composition reserved space for it
- all generated code is marked with `[GeneratedCode]`, allowing analyzers, coverage tools, and code-style tools to recognize and optionally exclude it according to their settings; the generated class also contains the actual Pure.DI package version
- graph construction and generation now use caching, prefer cheaper syntax analysis where it is sufficient, and avoid some repeated graph construction attempts, reducing generator overhead during compilation

---

## Useful extras

A few smaller additions that do not need their own sections:

**New factory context properties.** [`ctx.RootName`](https://github.com/DevTeam/Pure.DI/blob/master/readme/root-name.md), [`ctx.RootType`](https://github.com/DevTeam/Pure.DI/blob/master/readme/root-type.md), and [`ctx.IsLockRequired`](https://github.com/DevTeam/Pure.DI/blob/master/readme/islockrequired.md) join `ctx.Tag`, `ctx.ConsumerType`, and `ctx.Lock`:

```csharp
// The logger knows which root is constructing it
.Bind().To(ctx => new Logger(ctx.RootName))

// Lock only when it is actually required
.Bind().To(ctx =>
{
    if (ctx.IsLockRequired)
    {
        lock (ctx.Lock) { /* ... */ }
    }
    // ...
})
```

**Dictionary injection.** `IReadOnlyDictionary<TKey, TValue>` is assembled automatically from `KeyValuePair<TKey, TValue>` bindings marked with `Tag.Unique`, making runtime implementation selection by key straightforward:

```csharp
.Bind(Tag.Unique).To((EmailChannel c) => new KeyValuePair<Channel, INotificationChannel>(Channel.Email, c))
.Bind(Tag.Unique).To((SmsChannel c) => new KeyValuePair<Channel, INotificationChannel>(Channel.Sms, c))

class NotificationService(IReadOnlyDictionary<Channel, INotificationChannel> channels);
```

[Dictionary: select a dependency by key](https://github.com/DevTeam/Pure.DI/blob/master/readme/dictionary.md)

**More BCL types out of the box.** `TimeProvider`, `TaskCompletionSource<T>`, `CultureInfo`, `IFormatProvider`, `StringComparer`, `IReadOnlySet<T>`, `RandomNumberGenerator`, and other types can be injected without additional setup. A [default binding can be overridden](https://github.com/DevTeam/Pure.DI/blob/master/readme/default-bcl-bindings.md) when necessary.

**Factories with up to 16 parameters**, and an update to Roslyn 5.6.

**Uno Platform example.** A cross-platform application using the single-project Uno SDK: a composition exposed as a XAML resource, virtual roots for view models, and `DesignTimeComposition` with override roots for the designer, all without a runtime container. [Read the detailed example](https://github.com/DevTeam/Pure.DI/blob/master/readme/UnoApp.md).

---

## Conclusion

To try the new features, start with [any example](https://github.com/DevTeam/Pure.DI?tab=readme-ov-file#examples). Each example is independent and easy to run with `dotnet run`. If something is missing, feel free to open an issue in the [Pure.DI GitHub repository](https://github.com/DevTeam/Pure.DI).

Thank you for your interest and for reading to the end!
