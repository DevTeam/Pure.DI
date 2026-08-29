# Pure.DI Usage Patterns for Huge Composition Generation

## Purpose

This document defines the Pure.DI patterns that the `HugeComposition` sample should exercise. It is intended as an input specification for a future deterministic generator, not as an end-user tutorial.

The bulk part of the generator, before the additional feature islands, stresses a large constructor-injected graph:

- 12 application modules;
- 536 explicit bindings;
- 13 named roots;
- 1,072 service declarations;
- `Transient`, `PerBlock`, `PerResolve`, and `Singleton` lifetimes.

Future versions should preserve this large connected graph while adding representative islands that exercise different Pure.DI features. Every generated island must be reachable from at least one root unless the island intentionally tests an anonymous root or diagnostic.

The current implementation adds all core feature families described below to that bulk graph and uses separate generated compositions where a feature changes composition-wide behavior, such as `Scoped`, `RootArg`, asynchronous roots, or generation hints.

The canonical examples are in the repository [README](../../README.md#examples) and the generated [usage documentation](../../readme/auto-bindings.md).

## Generation principles

1. Generate valid application-shaped graphs rather than unrelated pairs of services.
2. Keep generation deterministic. The same seed and configuration must produce byte-identical C#.
3. Give every generated type, setup, binding, tag, argument, and root a stable unique name.
4. Prefer abstraction-to-implementation bindings for the main graph. Use auto-bindings only in cases dedicated to that feature.
5. Make every feature observable in the generated code: use a dependency at least twice when testing reuse, expose or consume every collection, and make every argument flow to a leaf.
6. Avoid intentional warnings in the default profile. Diagnostic cases should live in an explicit negative-test profile.
7. Keep cycles out of the graph. A decorator may depend on a tagged base implementation of the same contract, but it must not resolve itself.
8. Do not combine every feature in one root. Use several medium-sized feature islands connected to the shared application graph.

## Core patterns

### 1. Explicit abstraction binding

Use constructor injection with an interface or abstract contract bound to a concrete implementation. This should remain the dominant pattern in the generated graph.

```csharp
.Bind<IRepository>().To<SqlRepository>()
.Bind<IService>().To<Service>()
.Root<IService>("Service")
```

Generate chains and fan-in/fan-out shapes: services that depend on several abstractions, shared infrastructure used by several modules, and facades that aggregate use cases.

Reference: [Injections of abstractions](../../readme/injections-of-abstractions.md).

### 2. Auto-binding and simplified binding

Pure.DI can construct a concrete type without an explicit binding. `Bind().To<T>()` additionally infers the implementation type and its directly implemented non-special abstractions.

```csharp
.Bind().To<OrderProcessor>()
.Root<OrderEndpoint>("Endpoint")
```

Generate both forms in a limited portion of the graph. Do not rely on simplified binding to include inherited interfaces or special BCL contracts such as `IDisposable` and `IEnumerable<T>`.

References: [Auto-bindings](../../readme/auto-bindings.md), [Simplified binding](../../readme/simplified-binding.md).

### 3. Multiple contracts for one implementation

One implementation can satisfy several contracts. Generate consumers that request the contracts independently and together.

```csharp
.Bind<IReader, IWriter, Storage>().To<Storage>()
```

Use this pattern to increase graph connectivity without creating artificial pass-through services.

### 4. Lifetimes

Exercise every standard lifetime and make its reuse boundary observable.

| Lifetime | Required generated shape |
|---|---|
| `Transient` | Inject the contract twice and expect two independently created instances. This is the default lifetime. |
| `PerBlock` | Request the same dependency more than once inside one generated construction block. Do not describe it as a general application scope. |
| `PerResolve` | Request the dependency along multiple paths of one root resolution and expose the same dependency through two root calls. |
| `Singleton` | Share the dependency across several roots of one composition instance. Include both disposable and non-disposable variants. |
| `Scoped` | Create at least two explicit scopes, share a dependency inside each scope, and isolate it between scopes. |

```csharp
.Bind<IClock>().As(Lifetime.Singleton).To<SystemClock>()
.Bind<IUnitOfWork>().As(Lifetime.PerResolve).To<UnitOfWork>()
.Bind<IRequestCache>().As(Lifetime.Scoped).To<RequestCache>()
```

Never make a longer-lived service capture a shorter-lived dependency accidentally. If such a graph is needed to test generator behavior, isolate and label it as an intentional lifetime-capture case.

References: [Transient](../../readme/transient.md), [PerBlock](../../readme/perblock.md), [PerResolve](../../readme/perresolve.md), [Singleton](../../readme/singleton.md), [Scoped](../../readme/scoped.md).

### 5. Tagged selection

Generate several implementations of one contract and select them using stable semantic tags. Include a default/untagged path.

```csharp
.Bind<IApiClient>("public", default).To<PublicApiClient>()
.Bind<IApiClient>("internal").To<InternalApiClient>()
```

```csharp
class Gateway(
    [Tag("public")] IApiClient publicClient,
    [Tag("internal")] IApiClient internalClient,
    IApiClient defaultClient);
```

Use string constants, enums, type tags, and selected smart tags in separate cases. Reuse a small tag vocabulary instead of generating arbitrary strings.

References: [Tags](../../readme/tags.md), [Smart tags](../../readme/smart-tags.md), [Tag Type](../../readme/tag-type.md), [Tag Unique](../../readme/tag-unique.md).

### 6. Multi-binding and collections

Bind several implementations to one contract and inject all of them. At minimum cover `IEnumerable<T>` and arrays; extended profiles may add spans, dictionaries, or other supported BCL shapes.

```csharp
.Bind<IValidator>().To<FormatValidator>()
.Bind<IValidator>("business").To<BusinessValidator>()
```

```csharp
class ValidationPipeline(IEnumerable<IValidator> validators);
```

Binding order is observable for `IEnumerable<T>`, so generation order must be deterministic. Ensure every emitted binding is consumed by the collection.

References: [Enumerable](../../readme/enumerable.md), [Array](../../readme/array.md), [Dictionary](../../readme/dictionary.md).

### 7. Generic bindings

Use Pure.DI marker types such as `TT`, `TT1`, and `TT2` to describe generic bindings. Generate several closed usages from each generic binding so the feature is not equivalent to a single closed binding.

```csharp
.Bind<IRepository<TT>>().To<Repository<TT>>()
```

```csharp
class DataService(
    IRepository<Customer> customers,
    IRepository<Order> orders);
```

Extended cases should cover reordered generic arguments, constraints, generic roots, and generic root arguments. Keep marker substitutions unambiguous.

References: [Generics](../../readme/generics.md), [Complex generics](../../readme/complex-generics.md), [Generic composition roots](../../readme/generic-composition-roots.md).

## Creation patterns

### 8. Context factory

Use `To<T>(ctx => ...)` when construction needs explicit initialization or several injected values.

```csharp
.Bind<IDatabase>().To<Database>(ctx =>
{
    ctx.Inject(out Database database);
    database.Connect();
    return database;
})
```

Generate a visible initialization effect. Keep business branching out of the factory.

Reference: [Factory](../../readme/factory.md).

### 9. Simplified factory

Factory lambda parameters are injected dependencies. Generate both untagged and tagged parameters.

```csharp
.Bind<IReportWriter>().To((
    ReportWriter writer,
    [Tag("format")] string format) =>
{
    writer.Initialize(format);
    return writer;
})
```

Reference: [Simplified factory](../../readme/simplified-factory.md).

### 10. Deferred and repeated creation

Inject `Func<T>` for lazy or repeated construction. Add `Func<TArg, T>` cases when a runtime value is required for each creation.

```csharp
class WorkerPool(Func<IWorker> createWorker);
```

The expected instance reuse depends on the binding lifetime; do not assume that every factory call creates a new object.

References: [Injection on demand](../../readme/injection-on-demand.md), [Injections on demand with arguments](../../readme/injections-on-demand-with-arguments.md), [Func](../../readme/func.md).

### 11. Constructor, property, field, and method injection

Constructor injection should be the default. Add small dedicated types with `[Dependency]` members to exercise property, field, and method injection and their ordering.

```csharp
class Handler
{
    [Dependency] public ILogger Logger { get; set; } = null!;

    [Dependency(ordinal: 1)]
    public void Initialize(IOptions options) { }
}
```

Only generate writable and composition-accessible members. When several members are injected, use stable ordinals.

References: [Property injection](../../readme/property-injection.md), [Field injection](../../readme/field-injection.md), [Method injection](../../readme/method-injection.md).

### 12. Build-up and builders

Use `ctx.BuildUp(instance)` for an already-created object with injectable members. Use builder patterns when a root needs a structured creation API.

```csharp
.Bind().To(ctx =>
{
    var instance = new LegacyComponent();
    ctx.BuildUp(instance);
    return instance;
})
```

Keep these cases separate from ordinary constructor-injected types so their generated paths are easy to identify.

References: [Build up of an existing object](../../readme/build-up-of-an-existing-object.md), [Builder](../../readme/builder.md), [Builders](../../readme/builders.md).

## Inputs and entry points

### 13. Composition arguments

Use `Arg<T>` for values supplied once when the composition is constructed. Make each argument reach one or more dependencies.

```csharp
.Arg<string>("connectionString")
.Arg<string>("apiToken", "token")
```

Generate tagged arguments when several values have the same type. Unused arguments should not be emitted.

Reference: [Composition arguments](../../readme/composition-arguments.md).

### 14. Root arguments

Use `RootArg<T>` for values supplied separately on every root call. A root with arguments is generated as a method.

```csharp
.RootArg<Guid>("requestId")
.Root<IRequestHandler>("CreateHandler")
```

Generate multiple roots with disjoint argument sets as well as one root that consumes several arguments. Anonymous roots with root arguments must not depend on dynamic `Resolve` calls.

Reference: [Root arguments](../../readme/root-arguments.md).

### 15. Root shapes

Cover several public entry-point forms:

- named property roots;
- method roots created by root arguments;
- tagged roots;
- multiple roots sharing dependencies;
- anonymous roots available through generated resolve methods;
- generic, async, and static roots in extended profiles;
- root kinds such as private, internal, exported, virtual, and override where the surrounding project shape supports them.

```csharp
.Root<IApplication>("Application")
.Root<IJob>("SynchronizationJob", "sync")
.Root<ILogger>()
```

Use explicit root members for normal application flow. Dynamic `Resolve` is a separate compatibility pattern, not the primary service-locator style.

References: [Composition roots](../../readme/composition-roots.md), [Resolve methods](../../readme/resolve-methods.md), [Composition root kinds](../../readme/composition-root-kinds.md).

## Lifecycle and ownership

### 16. Disposal

Generate `IDisposable` and `IAsyncDisposable` dependencies for long-lived and operation-lived paths. The sample should verify these shapes through compilation and, when an executable verification layer is added, through disposal counters.

Include:

- a disposable singleton released with the composition;
- a disposable scoped dependency released with its scope;
- an async-disposable dependency released through `DisposeAsync`;
- several disposable dependencies created in one graph to exercise ordering.

References: [Disposable singleton](../../readme/disposable-singleton.md), [Async disposable singleton](../../readme/async-disposable-singleton.md), [Async disposable scope](../../readme/async-disposable-scope.md).

### 17. Explicit ownership

Use `Owned<T>` when a caller must release one resolved object graph independently of the main composition.

```csharp
.Root<Owned<IMessageHandler>>("Handler")
```

Also generate `Func<Owned<T>>` for repeated operation ownership. Ensure an owned graph contains at least one disposable value; otherwise the case does not test ownership.

Reference: [ArrayPool buffer](../../readme/arraypool-buffer.md).

### 18. Scopes

A scoped case must include a child scope type derived from the parent composition, a factory for that scope, and at least two consumers of the scoped dependency.

```csharp
sealed class RequestScope(Composition parent) : Composition(parent);
```

Generate at least two independent scope instances in the runtime verification profile. Do not model `Scoped` as an alias for `PerResolve`.

References: [Scope](../../readme/scope.md), [Scoped](../../readme/scoped.md), [Auto scoped](../../readme/auto-scoped.md).

## Structural and advanced patterns

### 19. Decorators and interception

Build decorator chains with tagged base bindings. The outer implementation uses the default contract while its constructor requests the tagged inner contract.

```csharp
.Bind<ICommandHandler>("core").To<CommandHandler>()
.Bind<ICommandHandler>().To<LoggingCommandHandler>()
```

Generate two- and three-layer chains. Keep tags distinct so the graph cannot recurse into the outer decorator.

References: [Decorator](../../readme/decorator.md), [Interception](../../readme/interception.md).

### 20. Attribute-declared bindings

Exercise `[Bind]` and `[Export]` on a limited set of implementation types. Include lifetime and tag metadata, and include one multi-binding group consumed as a collection.

```csharp
[Bind(typeof(IMessageWriter), Lifetime.Singleton, "console")]
class ConsoleMessageWriter : IMessageWriter;
```

Keep attribute-only cases separate from equivalent fluent bindings to avoid accidental duplicate bindings. Attributes in one group describe one binding; separate groups create separate bindings.

References: [Bind attribute](../../readme/bind-attribute.md), [Bind attribute groups](../../readme/bind-attribute-groups.md), [Export attribute](../../readme/export-attribute.md).

### 21. Dependent setups

Split selected modules into reusable setup layers with `CompositionKind.Internal` and `.DependsOn(...)`. This tests large-graph composition without forcing all declarations into one setup chain.

```csharp
DI.Setup("Infrastructure", CompositionKind.Internal)
    .Bind<IDatabase>().To<Database>();

DI.Setup(nameof(Composition))
    .DependsOn("Infrastructure")
    .Root<IApplication>("Application");
```

Keep ownership boundaries obvious: infrastructure, domain module, and application are sufficient layers for the stress sample.

Reference: [Dependent compositions](../../readme/dependent-compositions.md).

### 22. Built-in BCL shapes

Add representative consumers of built-in bindings rather than testing every supported type in every generated module.

Recommended minimum set:

- `Func<T>` and `Func<TArg, T>`;
- `Lazy<T>`;
- `IEnumerable<T>` and `T[]`;
- tuples;
- `Task<T>` and `ValueTask<T>` roots or dependencies;
- `IServiceProvider` only in a dedicated interoperability case.

Extended high-performance profiles may add `Span<T>`, `ReadOnlySpan<T>`, `ArrayPool<T>`, structs, ref structs, and async enumerables.

Reference: [Base Class Library examples](../../README.md#base-class-library).

### 23. Hints and generated hooks

Use hints only in dedicated composition variants because they change the generated API or implementation globally.

Useful stress cases include:

- `.Hint(Hint.Resolve, "Off")`;
- `.Hint(Hint.ThreadSafe, "Off")` for a documented single-thread-only composition;
- creation and injection callbacks such as `OnNewInstance` and `OnDependencyInjection`;
- generated member naming and visibility hints.

Do not mix mutually incompatible hint expectations in one composition.

Reference: [Hints](../../README.md#hints).

## Recommended generator profiles

| Profile | Contents | Purpose |
|---|---|---|
| `Core` | Explicit and simplified bindings, constructor injection, all non-scoped lifetimes, multiple roots | Preserve the existing large-graph compile-time workload. |
| `Selection` | Tags, default bindings, collections, generic bindings, decorators | Stress dependency selection and graph expansion. |
| `Creation` | Context factories, simplified factories, `Func`, composition arguments, root arguments, member injection | Stress generated creation paths and root signatures. |
| `Lifecycle` | Singleton disposal, async disposal, `Owned<T>`, explicit scopes | Stress tracking, ownership, and cleanup code. |
| `Structure` | Attribute bindings, dependent setups, root kinds, selected hints | Stress discovery and composition API generation. |
| `Extended` | Advanced BCL types, constrained generics, async/generic roots, interception | Broaden coverage without making the default workload fragile. |

The default `HugeComposition` generation should enable `Core`, `Selection`, `Creation`, and a bounded subset of `Lifecycle`. Other profiles can be enabled independently so a failure identifies a feature family.

## Suggested distribution

For every 100 generated bindings in the default profile, use an approximate distribution rather than a strict quota:

| Pattern | Approximate share |
|---|---:|
| Explicit abstraction bindings | 55% |
| Simplified bindings and auto-bindings | 10% |
| Generic bindings and closed generic uses | 10% |
| Tagged implementations and decorators | 10% |
| Multi-bindings consumed by collections | 5% |
| Factory-created bindings | 5% |
| Attribute-declared bindings | 5% |

Lifetimes are an independent dimension. A useful default split is 55% `Transient`, 20% `PerBlock`, 10% `PerResolve`, 10% `Singleton`, and 5% `Scoped` within dedicated scope islands.

These values should be configurable and deterministic. They are coverage guidance, not API requirements.

## Validation checklist

The future generator should emit enough metadata for the test harness to validate the workload:

- number of modules, declarations, bindings, roots, tags, factories, and arguments;
- number of bindings per lifetime;
- number of reachable nodes and maximum dependency depth;
- deterministic source hash for a fixed seed;
- no unexpected Pure.DI diagnostics or C# warnings;
- successful clean build of `HugeComposition.csproj`;
- generated root signatures match the requested property/method/static/async shapes;
- runtime assertions for selection, reuse, argument flow, collection order, scope isolation, and disposal when an executable verification project is available.

Each feature island should have a stable comment marker in generated C# so a failure can be traced back to its pattern and seed.

## Out of scope for the default workload

The following features are valuable but should be opt-in because they require specialized source shapes, project references, or expected diagnostics:

- Unity and UI-framework integrations;
- Microsoft `IServiceCollection` integration;
- generated composition interfaces;
- setup contexts crossing assembly boundaries;
- unsafe code and platform-specific types;
- deliberate ambiguous, cyclic, inaccessible, or unresolvable graphs;
- diagnostic severity customization;
- performance patterns requiring benchmarking rather than compilation.

Keeping these cases outside the default profile makes `HugeComposition` a stable generator stress test while leaving room for focused compatibility workloads.
