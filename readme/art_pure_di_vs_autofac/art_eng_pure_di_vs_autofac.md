# Pure.DI vs. Autofac: Code Generation or a Classic DI Container?

Search the web for “a powerful DI container for .NET,” and Autofac will appear on almost every list. It has a well-earned reputation as a mature, feature-rich container: lifetime scopes, modules, decorators, keyed services, factories, `Owned<T>`, interception, and an extensible resolve pipeline.

Against this background, Pure.DI is sometimes perceived as a specialized generator that wins on speed but must fall behind a classic DI container in functionality. That comparison needs qualification: the libraries solve many of the same application-level problems, but they do so at different stages of the application lifecycle.

Pure.DI handles nearly the same everyday tasks as Autofac, but moves most of the container's work to compile time. From the setup, the generator builds an object graph—that is, a model of the dependencies—and uses it to generate code. The graph itself is a model, not a set of created instances. When the application accesses a root, the generated C# code creates the corresponding object composition.

Autofac stores registrations in a container and creates objects through the universal `Resolve` method at run time. Calling `Resolve` directly from application code can easily lead to the Service Locator anti-pattern.

Pure.DI works differently. It generates ordinary statically typed C# code and validates the dependency graph at compile time. Structural setup errors in the analyzed roots are therefore detected before the application starts. The public composition API consists of explicitly declared, strongly typed roots: properties or methods of that composition.

The comparison below uses Pure.DI 2.5.2 and Autofac 9.3.1. The Autofac examples come from its official documentation. The Pure.DI examples come from its documentation and test scenarios.

## The Short Answer

When the application structure is known at compile time, Pure.DI covers the core Autofac feature set, detects structural errors in the statically analyzed graphs of declared roots, and eliminates the overhead of resolving dependencies through a classic DI container.

Autofac's main advantage appears when the dependency graph is determined at run time—for example, when plugins are loaded as assemblies while the application is running, registrations come from external configuration, or implementations are not known in advance.

| When the priority is | Preferred option |
|---|---|
| A statically known graph, build-time validation, Native AOT, and minimal object-creation cost | Pure.DI |
| Plugins, dynamic registrations, and run-time resolve pipelines | Autofac |
| Ready-made multitenancy infrastructure and the extension ecosystem of a classic DI container | Autofac |
| Typed roots and readable object-creation code | Pure.DI |

## One Task, Two Architectures

A typical Autofac registration looks like this:

```csharp
var builder = new ContainerBuilder();

builder.RegisterType<ConsoleLogger>()
    .As<ILogger>()
    .SingleInstance();

builder.RegisterType<OrderService>()
    .As<IOrderService>();

using var container = builder.Build();
var service = container.Resolve<IOrderService>();
```

At run time, `Build()` creates an Autofac container containing the registration descriptions. When `Resolve<IOrderService>()` is called, the container selects a constructor, locates dependencies, applies lifetime rules, and creates the object composition.

Source: [Autofac registration concepts](https://docs.autofac.org/en/latest/register/registration.html).

The equivalent Pure.DI setup is:

```csharp
DI.Setup(nameof(Composition))
    .Bind<ILogger>().As(Lifetime.Singleton).To<ConsoleLogger>()
    .Bind<IOrderService>().To<OrderService>()
    .Root<IOrderService>("OrderService");

var composition = new Composition();
var service = composition.OrderService;
```

Source: [injection of abstractions in Pure.DI](https://github.com/DevTeam/Pure.DI/blob/2.5.2/readme/injections-of-abstractions.md).

Both snippets use the same domain types: `ILogger`, `ConsoleLogger`, `IOrderService`, and `OrderService`. The domain types are omitted because only the DI setup is being compared.

The setups look similar, but the approaches differ substantially. For the `OrderService` composition root, Pure.DI generates C# code for creating the object composition, conceptually similar to:

```csharp
public IOrderService OrderService
{
    get
    {
        _singletonLogger ??= new ConsoleLogger();
        return new OrderService(_singletonLogger);
    }
}
```

Pure.DI does not search registrations or bindings at run time. The generator has already selected the types and constructors, and an error in the dependency graph becomes a compilation error rather than a run-time error.

## Feature Matrix

| Feature | Autofac | Pure.DI |
|---|---|---|
| Constructor injection | Yes | Yes, with compile-time validation |
| Property, field, and method injection | Yes for [properties and methods](https://docs.autofac.org/en/latest/register/prop-method-injection.html) | Yes for [properties](https://github.com/DevTeam/Pure.DI/blob/2.5.2/readme/property-injection.md), [fields](https://github.com/DevTeam/Pure.DI/blob/2.5.2/readme/field-injection.md), and [methods](https://github.com/DevTeam/Pure.DI/blob/2.5.2/readme/method-injection.md) |
| `Transient` and `Singleton` | Yes | Yes |
| Lifetime scopes and `Scoped` | Yes | Yes |
| Named and matching scopes | Yes | Separate and dependent compositions, `SetupScope`, tags |
| Delegates and factories | `Func<T>`, `Func<X, Y, T>`, custom delegate factories, lambda registrations | `Func<T>`, `Func<X, Y, T>`, factories with arguments, strongly typed root methods |
| Deferred creation | `Lazy<T>` | `Lazy<T>` |
| Collections of implementations | [`IEnumerable<T>` and other relationship types](https://docs.autofac.org/en/latest/resolve/relationships.html#enumeration-ienumerable-b-ilist-b-icollection-b) | [Arrays](https://github.com/DevTeam/Pure.DI/blob/2.5.2/readme/array.md), lists, [enumerables](https://github.com/DevTeam/Pure.DI/blob/2.5.2/readme/enumerable.md), [dictionaries](https://github.com/DevTeam/Pure.DI/blob/2.5.2/readme/dictionary.md), and ranges |
| Named and keyed services | Names, keys, `IIndex<TKey,T>` | Tags, smart tags, `IReadOnlyDictionary<TKey, TValue>`, `IKeyedServiceProvider` |
| Generic bindings | Open generic type registrations | Precise generic bindings with marker types |
| Generic roots with type constraints | No dedicated API: the container resolves an already closed generic type | [Yes](https://github.com/DevTeam/Pure.DI/blob/2.5.2/readme/generic-composition-roots-with-constraints.md); the root is generated as a generic C# method |
| Decorators | Built-in decorator registration | Ordinary bindings and tags processed at compile time |
| Interception | Integration with Castle DynamicProxy | Generated interception methods; Castle DynamicProxy when needed |
| `Owned<T>` | A separate lifetime scope | A separate ownership model for the object composition being created |
| Synchronous and asynchronous disposal | [Yes](https://docs.autofac.org/en/latest/lifetime/disposal.html) | [Yes](https://github.com/DevTeam/Pure.DI/blob/2.5.2/readme/tracking-async-disposable-instances-per-a-composition-root.md) |
| Building up an existing object | `InjectProperties()` | Properties, fields, and methods through `BuildUp`/`TryBuildUp` |
| Modules | Run-time modules | Dependent, internal, and global compositions |
| [Assembly scanning](https://docs.autofac.org/en/latest/register/scanning.html) | Yes, at run time | No: types must be known at compile time |
| [Registration sources](https://docs.autofac.org/en/latest/advanced/registration-sources.html) | Yes, registrations can be supplied dynamically | No direct equivalent: the graph is fixed at compile time |
| [Resolve pipelines](https://docs.autofac.org/en/latest/advanced/pipelines.html) | Yes, middleware for services and registrations | No run-time pipeline; generated interception methods are available |
| [Registration configuration](https://docs.autofac.org/en/latest/configuration/index.html) | Modules and external run-time configuration | Setups are part of the source code and require recompilation |
| [`Meta<T>` metadata](https://docs.autofac.org/en/latest/advanced/metadata.html) | Yes, including strongly typed metadata | No direct `Meta<T>` equivalent; compile-time tags and attributes are available |
| [Multitenant containers](https://docs.autofac.org/en/latest/advanced/multitenant.html) | Ready-made run-time infrastructure | No direct equivalent; compositions can be created for individual tenants |
| [Pooled instances](https://docs.autofac.org/en/latest/advanced/pooled-instances.html) | Ready-made pooling package | No built-in lifetime; an [object-pool example](https://github.com/DevTeam/Pure.DI/blob/2.5.2/readme/object-pool.md) implements the pattern in application code |
| Compile-time validation of declared root graphs | No | [Yes](https://github.com/DevTeam/Pure.DI/blob/2.5.2/DIAGNOSTICS.md) |
| Explicit typed roots | Not the primary model | [Yes](https://github.com/DevTeam/Pure.DI/blob/2.5.2/readme/composition-roots.md) |
| Typed root arguments | No dedicated root API; `Resolve` parameters and delegate factories are available | [Yes](https://github.com/DevTeam/Pure.DI/blob/2.5.2/readme/root-arguments.md); roots are generated as ordinary methods |
| Native AOT | [Supported with limitations](https://docs.autofac.org/en/latest/advanced/native-aot-trimming.html) | Generated object-creation code does not require .NET Reflection; see the [Native AOT example](https://github.com/DevTeam/Pure.DI/blob/2.5.2/readme/ConsoleNativeAOT.md) |
| `Span<T>` and `ref struct` in the graph | Not available as universal relationship types | [Yes](https://github.com/DevTeam/Pure.DI/blob/2.5.2/readme/span-and-readonlyspan.md) |
| `ref` dependencies and stack-only root arguments | The container's universal object-based API cannot pass managed references or non-boxable `ref struct` values; external typed code is required | [Yes](https://github.com/DevTeam/Pure.DI/blob/2.5.2/readme/ref-dependencies.md), including generated root methods with [`scoped ReadOnlySpan<T>`](https://github.com/DevTeam/Pure.DI/blob/2.5.2/readme/method-injection-for-a-hot-path.md) |
| Union types as DI contracts | No | [Yes](https://github.com/DevTeam/Pure.DI/blob/2.5.2/readme/union-types.md) |
| Interface generation | No | [Yes](https://github.com/DevTeam/Pure.DI/blob/2.5.2/readme/generate-an-interface-from-a-class.md) |
| Nullable annotations as part of a DI contract | `T` and `T?` are represented by the same `System.Type` and cannot be distinguished by `Resolve(Type)` | [Yes](https://github.com/DevTeam/Pure.DI/blob/2.5.2/readme/nullable-reference-types.md), validated throughout the graph |
| Inspecting and debugging object-creation code | Internal run-time pipeline | [Ordinary generated C#](https://github.com/DevTeam/Pure.DI/blob/2.5.2/readme/composition-roots.md) |

The upper part of the table lists shared features, followed by Autofac features without a direct Pure.DI equivalent and Pure.DI features without a direct Autofac equivalent. Some rows describe different ways to solve the same application-level problem rather than direct counterparts. For example, an Autofac lifetime scope and a Pure.DI dependent composition have different semantics, although both help define boundaries for object creation and ownership.

## Registrations, Lifetimes, and Implementation Selection

### Lifetimes

Autofac supports the following instance scopes: `Instance Per Dependency`, `Single Instance`, `Instance Per Lifetime Scope`, `Instance Per Matching Lifetime Scope`, `Instance Per Request`, `Instance Per Owned`, and `Thread Scope`.

Source: [Autofac instance scopes](https://docs.autofac.org/en/latest/lifetime/instance-scope.html).

Pure.DI supports:

- `Transient`: a new instance for each injection;
- `Singleton`: one instance per composition;
- `Scoped`: one instance per scope;
- `PerResolve`: one instance per root construction;
- `PerBlock`: reuse within a block of generated code.

Sources: [Transient](https://github.com/DevTeam/Pure.DI/blob/2.5.2/readme/transient.md), [Singleton](https://github.com/DevTeam/Pure.DI/blob/2.5.2/readme/singleton.md), [Scoped](https://github.com/DevTeam/Pure.DI/blob/2.5.2/readme/scoped.md), [PerResolve](https://github.com/DevTeam/Pure.DI/blob/2.5.2/readme/perresolve.md), [PerBlock](https://github.com/DevTeam/Pure.DI/blob/2.5.2/readme/perblock.md).

Pure.DI's advantage here is that the generator knows in advance where each object is stored and creates a field or local variable for it. Lifetime violations (captive dependencies), missing bindings, and dependency cycles are detected before the program starts. If an analyzed graph contains a structural error, the application will not compile.

### Keyed Services and Tags

Autofac can register a service by name or by an arbitrary key:

```csharp
builder.RegisterType<OnlineState>()
    .Keyed<IDeviceState>(DeviceState.Online);

builder.RegisterType<OfflineState>()
    .Keyed<IDeviceState>(DeviceState.Offline);
```

For run-time selection, Autofac provides `IIndex<TKey, TValue>`.

Source: [named and keyed services in Autofac](https://docs.autofac.org/en/latest/advanced/keyed-services.html).

Pure.DI uses tags:

```csharp
DI.Setup(nameof(Composition))
    .Bind<IDeviceState>(DeviceState.Online).To<OnlineState>()
    .Bind<IDeviceState>(DeviceState.Offline).To<OfflineState>()
    .Root<IDeviceState>("Online", DeviceState.Online);
```

Source: [tags in Pure.DI](https://github.com/DevTeam/Pure.DI/blob/2.5.2/readme/tags.md).

This example uses the same `IDeviceState`, `OnlineState`, `OfflineState`, and `DeviceState` types as the Autofac snippet. A tag can be a string, enum value, type, `null`, `default`, or a [smart tag](https://github.com/DevTeam/Pure.DI/blob/2.5.2/readme/smart-tags.md).

In the example above, Pure.DI selects the root implementation by tag while generating the code. If a consumer needs to select one of the registered implementations by key at run time, it can receive an ordinary `IReadOnlyDictionary<TKey, TValue>`:

```csharp
DI.Setup(nameof(Composition))
    .Bind(Tag.Unique).To((OnlineState state) =>
        new KeyValuePair<DeviceState, IDeviceState>(DeviceState.Online, state))
    .Bind(Tag.Unique).To((OfflineState state) =>
        new KeyValuePair<DeviceState, IDeviceState>(DeviceState.Offline, state))
    .Root<DeviceStateSelector>("DeviceStateSelector");

sealed class DeviceStateSelector(
    IReadOnlyDictionary<DeviceState, IDeviceState> states)
{
    public IDeviceState this[DeviceState state] => states[state];
}
```

Pure.DI generates the code that builds the dictionary in advance and validates the graph of every value, while the specific element is selected by key at run time. This contract does not couple the application class to a DI tool's API and serves a purpose similar to Autofac's `IIndex<TKey, TValue>`.

Source: [dictionary injection in Pure.DI](https://github.com/DevTeam/Pure.DI/blob/2.5.2/readme/dictionary.md).

When the standard .NET API is required, a Pure.DI composition can also implement [`IKeyedServiceProvider`](https://github.com/DevTeam/Pure.DI/blob/2.5.2/readme/keyed-service-provider.md).

### Generic Bindings

Autofac registers an open generic type:

```csharp
builder.RegisterGeneric(typeof(Repository<>))
    .As(typeof(IRepository<>));
```

Source: [open generic components in Autofac](https://docs.autofac.org/en/latest/register/registration.html#open-generic-components).

Pure.DI uses a marker type:

```csharp
DI.Setup(nameof(Composition))
    .Bind<IRepository<TT>>().To<Repository<TT>>();
```

Source: [generic types in Pure.DI](https://github.com/DevTeam/Pure.DI/blob/2.5.2/readme/generics.md).

The binding registration looks similar, but Pure.DI creates concrete code separately for `Repository<User>` and `Repository<Order>`. This allows it to account for generic constraints, nested generic types, and tags—in other words, to determine the precise object type at compile time. A successful build confirms the structural correctness of the generated graph, but it does not rule out application exceptions in constructors, factories, interceptors, or resource-disposal code.

The distinction matters for Native AOT. The official Autofac documentation explicitly notes that an open generic type closed over a value type requires dynamic code at run time and can cause a `DependencyResolutionException`. In Pure.DI, the concrete closed types are known at compile time, so this issue does not arise.

Source: [Native AOT and trimming in Autofac](https://docs.autofac.org/en/latest/advanced/native-aot-trimming.html).

### Generic Roots

Classic DI containers support registrations of open generic types, but resolve an already closed type such as `IRepository<Order>`. Pure.DI can make the composition root itself generic and transfer the constraints of its type parameters into the public API:

```csharp
DI.Setup(nameof(Composition))
    .Hint(Hint.Resolve, "Off")
    .Bind().To<StreamSource<TTDisposable>>()
    .Bind().To<DataProcessor<TTDisposable, TTS>>()
    .Root<IDataProcessor<TTDisposable, TTS>>("GetProcessor");

var composition = new Composition();
var processor = composition.GetProcessor<Stream, double>();

interface IStreamSource<T>
    where T : IDisposable;

sealed class StreamSource<T> : IStreamSource<T>
    where T : IDisposable;

interface IDataProcessor<T, TOptions>
    where T : IDisposable
    where TOptions : struct;

sealed class DataProcessor<T, TOptions>(IStreamSource<T> source)
    : IDataProcessor<T, TOptions>
    where T : IDisposable
    where TOptions : struct;
```

For this root, Pure.DI generates an ordinary generic method:

```csharp
public IDataProcessor<T, TOptions> GetProcessor<T, TOptions>()
    where T : IDisposable
    where TOptions : struct;
```

The calling code selects `T` and `TOptions`, and the C# compiler validates them. The universal `Resolve` method of a classic DI container cannot express this contract because it accepts an already constructed closed type. An equivalent would require a separate, manually written generic factory outside the container API.

A type parameter can determine not only the return type but also the type of a root argument. For example, combining `RootArg<TT>("model")` with `Root<IPresenter<TT>>("GetPresenter")` creates a method in which the model is supplied at run time and type correspondence is checked by the compiler:

```csharp
public IPresenter<T> GetPresenter<T>(T model);
```

A root can also be asynchronous, accept ordinary run-time arguments, and preserve type constraints:

```csharp
public Task<IQuery<TConnection, TResult>>
    GetDataQueryAsync<TConnection, TResult>(
        CancellationToken cancellationToken)
    where TConnection : IDisposable
    where TResult : struct;
```

These are not new variations of `Resolve`: Pure.DI generates typed methods whose signatures become part of the composition API.

Sources: [Pure.DI generic roots with type constraints](https://github.com/DevTeam/Pure.DI/blob/2.5.2/readme/generic-composition-roots-with-constraints.md), [generic root arguments](https://github.com/DevTeam/Pure.DI/blob/2.5.2/readme/generic-root-arguments.md), and [asynchronous generic roots with constraints](https://github.com/DevTeam/Pure.DI/blob/2.5.2/readme/generic-async-composition-roots-with-constraints.md).

## Factories and Relationship Types

Autofac calls `Lazy<T>`, `Owned<T>`, `Func<T>`, `IEnumerable<T>`, `Meta<T>`, and `IIndex<TKey,TValue>` relationship types. They can be combined—for example, as `IEnumerable<Func<Owned<T>>>`.

Source: [implicit relationship types in Autofac](https://docs.autofac.org/en/latest/resolve/relationships.html).

Pure.DI supports the same core scenarios:

- [Lazy](https://github.com/DevTeam/Pure.DI/blob/2.5.2/readme/lazy.md);
- [Func](https://github.com/DevTeam/Pure.DI/blob/2.5.2/readme/func.md);
- [Func with arguments](https://github.com/DevTeam/Pure.DI/blob/2.5.2/readme/func-with-arguments.md);
- [enumerables](https://github.com/DevTeam/Pure.DI/blob/2.5.2/readme/enumerable.md);
- [dictionaries](https://github.com/DevTeam/Pure.DI/blob/2.5.2/readme/dictionary.md);
- [composition arguments](https://github.com/DevTeam/Pure.DI/blob/2.5.2/readme/composition-arguments.md);
- [root arguments](https://github.com/DevTeam/Pure.DI/blob/2.5.2/readme/root-arguments.md).

For example, both libraries automatically inject `Func<IEnemy>` into the same consumer:

```csharp
sealed class GameLevel(Func<IEnemy> enemyFactory) : IGameLevel
{
    public IEnemy CreateEnemy() => enemyFactory();
}
```

Autofac registration:

```csharp
builder.RegisterType<Enemy>().As<IEnemy>();
builder.RegisterType<GameLevel>().As<IGameLevel>();
```

Pure.DI registration and root:

```csharp
DI.Setup(nameof(Composition))
    .Bind().To<Enemy>()
    .Bind().To<GameLevel>()
    .Root<IGameLevel>("GameLevel");
```

Source: [on-demand injection in Pure.DI](https://github.com/DevTeam/Pure.DI/blob/2.5.2/readme/injection-on-demand.md).

In Autofac, the delegate is a relationship type. In Pure.DI, it is generated as a strongly typed object-creation call. The application-level `GameLevel` does not depend on the choice of DI tool.

Pure.DI also supports `Func<...>` with arguments whose values are determined dynamically at run time:

```csharp
DI.Setup(nameof(Composition))
    .Bind().As(Lifetime.Singleton).To<Clock>()
    .Bind().To<Person>()
    .Bind().To<Team>()
    .Root<Team>("Team");
```

The consumer receives a factory with two dynamic arguments:

```csharp
sealed class Team(Func<int, string, IPerson> personFactory)
{
    public IPerson CreatePerson(int id, string name) =>
        personFactory(id, name);
}
```

The `id` and `name` values are supplied when the factory is called. Pure.DI obtains the `IClock` dependency, which `Person` also needs, from the composition. Statically known dependencies are therefore validated at compile time, while dynamic data is passed at run time.

Source: [`Func` with arguments in Pure.DI](https://github.com/DevTeam/Pure.DI/blob/2.5.2/readme/func-with-arguments.md).

Autofac also supports `Func<X, Y, T>` with run-time arguments:

```csharp
builder.RegisterType<Clock>().As<IClock>().SingleInstance();
builder.RegisterType<Person>().As<IPerson>();

using var container = builder.Build();
var personFactory = container.Resolve<Func<int, string, IPerson>>();
var person = personFactory(10, "Nik");
```

For the standard `Func<X, Y, T>`, Autofac matches arguments to constructor parameters by type and obtains the remaining dependencies from the current lifetime scope. Such a `Func` does not support multiple arguments of the same type. For that case, Autofac provides custom delegate factories, whose parameters are matched by name.

Sources: [`Func<X, Y, T>` in Autofac](https://docs.autofac.org/en/latest/resolve/relationships.html#parameterized-instantiation-func-x-y-b) and [delegate factories](https://docs.autofac.org/en/latest/advanced/delegate-factories.html).

## `Owned<T>`: Same Name, Different Implementation

Both libraries allow a consumer to explicitly own part of the object composition being created and dispose of it independently of the container or main composition. For example, the consumer can use a dependency such as `Func<Owned<IMessageHandler>>`:

```csharp
sealed class MessagePump
{
    private readonly Func<Owned<IMessageHandler>> handlerFactory;

    public MessagePump(Func<Owned<IMessageHandler>> handlerFactory) =>
        this.handlerFactory = handlerFactory;

    public void Go()
    {
        while (true)
        {
            var message = NextMessage();

            using var handler = handlerFactory();
            handler.Value.Handle(message);
        }
    }
}
```

This is a form of the official example from the Autofac documentation on [owned instances](https://docs.autofac.org/en/latest/advanced/owned-instances.html).

### Autofac Setup

```csharp
builder.RegisterType<MessageHandler>().As<IMessageHandler>();
// MessagePump receives Func<Owned<IMessageHandler>>.
builder.RegisterType<MessagePump>();

using var container = builder.Build();
var pump = container.Resolve<MessagePump>();
```

For each `Owned<T>`, Autofac creates a separate `ILifetimeScope`. Disposing `Owned<T>` ends the scope and disposes dependencies created within it that are not shared.

In Autofac 9.3.1, `Owned<T>` is a class. It replaces the lifetime-scope reference through `Interlocked.Exchange`, supports `Dispose()` and `DisposeAsync()`, and then delegates disposal to the lifetime scope.

Source: [`Owned.cs` from Autofac 9.3.1](https://github.com/autofac/Autofac/blob/v9.3.1/src/Autofac/Features/OwnedInstances/Owned.cs).

### Pure.DI Setup

```csharp
DI.Setup(nameof(Composition))
    .Bind<IMessageHandler>().To<MessageHandler>()
    // MessagePump receives Func<Owned<IMessageHandler>>.
    .Bind().To<MessagePump>()
    .Root<MessagePump>("MessagePump");

var composition = new Composition();
var pump = composition.MessagePump;
```

Neither setup requires `Owned<T>` to be registered separately. Autofac supplies it as a relationship type, while Pure.DI recognizes `Func<Owned<IMessageHandler>>` in the `MessagePump` constructor and generates the factory together with disposal code for the object composition being created.

Sources: [tracking disposable instances in Pure.DI delegates](https://github.com/DevTeam/Pure.DI/blob/2.5.2/readme/tracking-disposable-instances-in-delegates.md) and [OwnedTests.cs](https://github.com/DevTeam/Pure.DI/blob/2.5.2/tests/Pure.DI.IntegrationTests/OwnedTests.cs).

Internally, however, there is no specialized lifetime scope as in Autofac:

- `Owned<T>` is a `readonly struct` containing the value and a reference to the disposal mechanism;
- the generator knows all disposable objects in the specific dependency graph in advance;
- a graph without resources uses a shared empty owner without a separate resource accumulator;
- when the number of resources is known, the accumulator is created with an appropriate capacity, optimizing the list used to store resources;
- with [`ThreadSafe = Off`](https://github.com/DevTeam/Pure.DI/blob/2.5.2/readme/threadsafe-off-for-single-thread-composition.md), the generator does not add unnecessary synchronization;
- synchronous and asynchronous disposal are generated with the target .NET platform in mind.

The implementation handles synchronous and asynchronous disposal, reverse resource order, repeated and concurrent calls, nested owners, shared lifetimes, and rollback when graph creation fails. These scenarios are covered by comparative integration tests against Autofac.

Autofac provides a similar outcome in equivalent scenarios. Pure.DI's advantage here is that lifetime management is implemented in the generated code that creates the object composition. This approach does not require a special lifetime scope like Autofac and enables compile-time optimizations that are unavailable to a classic DI container.

## Property and Method Injection, and Building Up Objects

Autofac supports required properties, `PropertiesAutowired()`, manual property assignment, and injection into an existing object through `InjectProperties()`. Method injection is usually configured with a lambda registration.

Source: [property and method injection in Autofac](https://docs.autofac.org/en/latest/register/prop-method-injection.html).

Pure.DI supports injection through properties, fields, and methods directly in the generated code:

```csharp
sealed class Navigator : INavigator
{
    public IMap? CurrentMap { get; private set; }

    [Dependency]
    public void LoadMap(IMap map) => CurrentMap = map;
}
```

Source: [method injection in Pure.DI](https://github.com/DevTeam/Pure.DI/blob/2.5.2/readme/method-injection.md).

Build-Up is available for an existing object:

```csharp
.Bind().To(ctx =>
{
    var person = new Person();
    ctx.BuildUp(person);
    return person;
})
```

`TryBuildUp` lets you safely check whether an object can be built up.

Sources: [building up an existing object](https://github.com/DevTeam/Pure.DI/blob/2.5.2/readme/build-up-of-an-existing-object.md) and [builders with TryBuildUp](https://github.com/DevTeam/Pure.DI/blob/2.5.2/readme/builders.md).

Because the members are known to the generator at compile time, objects are completed directly by ordinary C# code and do not require .NET Reflection at run time.

## Decorators and Interception

Autofac has a dedicated API:

```csharp
builder.RegisterType<TextWidget>()
    .As<IWidget>();

builder.RegisterDecorator<BoxWidget, IWidget>();
```

Source: [Autofac adapters and decorators](https://docs.autofac.org/en/latest/advanced/adapters-decorators.html).

In Pure.DI, the same chain is configured as follows:

```csharp
DI.Setup(nameof(Composition))
    .Bind("base").To<TextWidget>()
    .Bind().To<BoxWidget>()
    .Root<IWidget>("Widget");
```

Both cases use `TextWidget` and `BoxWidget`. The only difference in the DI setup is that Pure.DI explicitly tags the decorated service:

```csharp
sealed class BoxWidget([Tag("base")] IWidget inner) : IWidget
{
    public string Render() => $"[ {inner.Render()} ]";
}
```

The `"base"` tag is chosen arbitrarily for its meaning; it is not a “magic” value.

Source: [decorators in Pure.DI](https://github.com/DevTeam/Pure.DI/blob/2.5.2/readme/decorator.md).

This approach requires no special decorator logic at run time: decorators become part of the ordinary object composition, and the corresponding dependency graph is validated at compile time. For example, a single decorator class can decorate several objects of different types at once.

For interception, Autofac integrates with Castle DynamicProxy.

Source: [type interception in Autofac](https://docs.autofac.org/en/latest/advanced/interceptors.html).

Pure.DI provides the generated partial methods `OnNewInstance` and `OnDependencyInjection`. They can replace or wrap an instance at a selected point in the object composition without sacrificing performance. When a dynamic proxy object is genuinely required, Castle DynamicProxy can be integrated into the generated object-composition code.

Sources: [interception in Pure.DI](https://github.com/DevTeam/Pure.DI/blob/2.5.2/readme/interception.md) and [advanced interception](https://github.com/DevTeam/Pure.DI/blob/2.5.2/readme/advanced-interception.md).

Autofac is more convenient when interception must be configured entirely at run time. Pure.DI is stronger when performance, transparency, and generation-time type filtering matter.

## Modules and Large Compositions

Autofac modules encapsulate a set of registrations:

```csharp
public sealed class RepositoryModule : Module
{
    protected override void Load(ContainerBuilder builder)
    {
        builder.RegisterType<SqlRepository>()
            .As<IRepository>();
    }
}
```

Source: [Autofac modules](https://docs.autofac.org/en/latest/configuration/modules.html).

Pure.DI splits the setup of large compositions into several parts:

```csharp
DI.Setup("Infrastructure", CompositionKind.Internal)
    .Bind<IDatabase>().To<SqlDatabase>();

DI.Setup(nameof(Composition))
    .DependsOn("Infrastructure")
    .Bind<IUserService>().To<UserService>()
    .Root<IUserService>("UserService");
```

Sources: [dependent compositions](https://github.com/DevTeam/Pure.DI/blob/2.5.2/readme/dependent-compositions.md), [global compositions](https://github.com/DevTeam/Pure.DI/blob/2.5.2/readme/global-compositions.md), [composition inheritance](https://github.com/DevTeam/Pure.DI/blob/2.5.2/readme/inheritance-of-compositions.md), [setup context](https://github.com/DevTeam/Pure.DI/blob/2.5.2/readme/dependent-compositions-with-setup-context.md), and [scope setup](https://github.com/DevTeam/Pure.DI/blob/2.5.2/readme/scope-setup-method.md).

Compositions may be declared `public`, `internal`, or `global`. Composition inheritance, setup contexts, and separate scope setups are also available. These features make configuration reusable, and everything is processed at compile time.

Dynamic Autofac modules are better suited to assembling an application from a set of plugins that is not known in advance. Pure.DI compositions are better suited to modules defined at compile time: the graphs of declared roots are validated during the build.

## Where Pure.DI Is Particularly Strong

### A Graph Error Becomes a Build Error

`Build()` completes component registration and prepares the Autofac container, but does not perform full graph validation. Because registrations, parameters, modules, and factories can be dynamic, many errors are discovered only by a specific `Resolve()` call. A classic DI container therefore cannot always confirm in advance that every object composition created at run time will be valid.

Source: [why registration analysis is not built into Autofac](https://docs.autofac.org/en/latest/faq/container-analysis.html).

Pure.DI analyzes every declared root and reports:

- missing bindings;
- ambiguous implementation selection;
- cyclic dependencies;
- violations of generic type constraints;
- an inaccessible constructor or other type member;
- an incompatible tag;
- nullable annotation issues;
- invalid use of stack-only types;
- and much more.

The developer gets structurally validated object-composition code before the application starts. Application code in constructors, factories, and interceptors retains its ordinary behavior and can still throw an exception at run time.

### Composition Roots

In Autofac, the typical object-composition scenario is based on calling `Resolve<T>()`. In Pure.DI:

```csharp
var report = composition.CreateReport(userId);
var worker = composition.Worker;
```

A composition root can be a property or a method with typed arguments and a typed result. Anonymous roots remain available through `Resolve` methods when needed, just as in Autofac.

Sources: [composition roots](https://github.com/DevTeam/Pure.DI/blob/2.5.2/readme/composition-roots.md) and [typed root arguments](https://github.com/DevTeam/Pure.DI/blob/2.5.2/readme/root-arguments.md).

### Native AOT Without Reflection in Generated Code

Autofac 9 supports trimming and Native AOT for basic scenarios, but documents the following limitations:

- assembly scanning requires preserving types found through .NET Reflection;
- an open generic type closed over a value type can produce a run-time error;
- strongly typed metadata views require run-time expression compilation;
- generated factories, delegate factories, and custom property selectors can produce `RequiresDynamicCode` or `RequiresUnreferencedCode`;
- the absence of a warning for some relationship types does not guarantee full AOT compatibility.

Source: [Native AOT and trimming in Autofac](https://docs.autofac.org/en/latest/advanced/native-aot-trimming.html).

Pure.DI generates ordinary `new` calls. To the trimmer, this is normal statically linked C# code: the generator does not need to preserve constructors, methods, properties, and fields “just in case,” because everything required is already used directly. Application compatibility with Native AOT and trimming still depends on custom factories, interceptors, and third-party libraries.

Example: [a Pure.DI console application with Native AOT](https://github.com/DevTeam/Pure.DI/blob/2.5.2/readme/ConsoleNativeAOT.md).

### Modern C# Becomes Part of the Object Composition

Pure.DI supports scenarios that cannot be represented through the universal object-based API of most classic DI containers:

- `Span<T>` and `ReadOnlySpan<T>` as dependencies;
- `ref struct` and factories with `allows ref struct`;
- `ref` dependencies that preserve managed-reference semantics;
- stack-only root arguments and generated `scoped ReadOnlySpan<T>` parameters;
- `ValueTask<T>` roots;
- nullable annotations as part of a DI contract;
- union types as DI contracts.

In particular, `T` and `T?` have the same run-time type. A universal `Resolve(Type)` therefore cannot select between them as two separate contracts. Pure.DI analyzes nullable annotations in source code and preserves the distinction while validating and generating the graph.

An object-based API also cannot accept `ReadOnlySpan<T>` through `object` or `params object[]`: a stack-only value cannot be boxed. Pure.DI can instead generate a typed root:

```csharp
public IRouteMatcher CreateMatcher(
    scoped ReadOnlySpan<char> path);
```

Managed references are also preserved when dependencies are injected:

```csharp
[Ordinal]
public void Initialize(ref Data data);
```

Such an API can be written manually as an external factory, but it cannot be expressed through the universal object-based API of a classic DI container.

Sources: [Span and ReadOnlySpan](https://github.com/DevTeam/Pure.DI/blob/2.5.2/readme/span-and-readonlyspan.md), [`ref` dependencies](https://github.com/DevTeam/Pure.DI/blob/2.5.2/readme/ref-dependencies.md), [a factory with `allows ref struct`](https://github.com/DevTeam/Pure.DI/blob/2.5.2/readme/allows-ref-struct-factory.md), [method injection on a hot path](https://github.com/DevTeam/Pure.DI/blob/2.5.2/readme/method-injection-for-a-hot-path.md), [a factory with `ReadOnlySpan<T>`](https://github.com/DevTeam/Pure.DI/blob/2.5.2/readme/default-func-with-readonlyspan.md), [a `ValueTask<T>` root](https://github.com/DevTeam/Pure.DI/blob/2.5.2/readme/valuetask-root.md), [nullable reference types](https://github.com/DevTeam/Pure.DI/blob/2.5.2/readme/nullable-reference-types.md), [union types](https://github.com/DevTeam/Pure.DI/blob/2.5.2/readme/union-types.md), and [a non-boxing union result](https://github.com/DevTeam/Pure.DI/blob/2.5.2/readme/non-boxing-union-result.md).

Pure.DI can also [generate interfaces from a class](https://github.com/DevTeam/Pure.DI/blob/2.5.2/readme/generate-an-interface-from-a-class.md), preserving generic types, nullable annotations, events, and XML documentation.

## Performance: The Overhead of a Classic DI Container

The BenchmarkDotNet suite in the repository compares creation of the same object composition by hand, through Pure.DI, and through Autofac—a classic DI container.

The following rows come from saved results produced with BenchmarkDotNet 0.15.8 and .NET SDK 10.0.102 on Windows 10 with an AMD Ryzen 9 5900X. The table shows the mean time per operation and the amount of allocated managed memory:

| Scenario | Hand-written code | Pure.DI root | Autofac | Pure.DI memory | Autofac memory |
|---|---:|---:|---:|---:|---:|
| Transient | 2.820 ns | 3.088 ns | 13,207.209 ns | 24 B | 30,424 B |
| Singleton | 2.745 ns | 2.805 ns | 8,143.869 ns | 24 B | 22,048 B |
| `Func<T>` | 3.089 ns | 2.774 ns | 4,800.897 ns | 24 B | 13,128 B |
| Array | 49.80 ns | 51.32 ns | 13,300.06 ns | 408 B | 26,496 B |

Source: [Pure.DI performance benchmarks](https://github.com/DevTeam/Pure.DI/tree/91f5c2ee57dbccf2efa21959d7d8743ac1e9937e/benchmarks/data/results). The result snapshot was saved in commit `91f5c2ee57dbccf2efa21959d7d8743ac1e9937e` on February 14, 2026.

In this run, creating this particular transient graph through a generated Pure.DI root was approximately 4,276 times faster and allocated about 1/1,268 as much memory as resolving the same root through Autofac. These results apply to a specific object graph and benchmark environment. The ratio may change with a different graph structure, .NET version, hardware, or way of obtaining the root.

The difference follows from the architecture of the tools. Pure.DI generates specialized graph-creation code in advance, close to hand-written code. Autofac resolves dependencies through the universal mechanism of a classic DI container and applies lifetime rules while the program is running. Pure.DI's advantage is therefore not a particular result from one benchmark, but minimal overhead when creating a statically known graph.

## Where Autofac Is Objectively Stronger

### Assembly Scanning

Autofac can load an assembly, find suitable implementations, and register them at run time.

Source: [assembly scanning in Autofac](https://docs.autofac.org/en/latest/register/scanning.html).

This is useful for plugins and extensions. The trade-offs are reliance on .NET Reflection, less explicit configuration, and additional trimming requirements.

### Registration Sources and Resolve Pipelines

Autofac can dynamically provide a registration for an unknown type and add middleware to the resolve pipeline.

Sources: [registration sources](https://docs.autofac.org/en/latest/advanced/registration-sources.html) and [resolve pipelines](https://docs.autofac.org/en/latest/advanced/pipelines.html).

Frameworks and infrastructure libraries need this flexibility. In Pure.DI, similar behavior is usually implemented through a factory, an interception method, or specialized code.

### Ready-Made Multitenancy Infrastructure

Autofac has a dedicated multitenant package and tenant-specific containers.

Source: [multitenant applications in Autofac](https://docs.autofac.org/en/latest/advanced/multitenant.html).

Pure.DI can create separate compositions for each tenant using tags, factories, composition arguments, or root arguments, but does not provide identical run-time capabilities.

If an application genuinely uses these capabilities, Autofac may be the better choice. Most applications, however, do not need a dynamic dependency graph to create object compositions.

## Different Extensibility Models

Autofac describes features in container terms:

- registration sources;
- resolve pipelines;
- service middleware;
- lifetime scopes;
- module scanning.

Pure.DI often solves the same application-level problems with ordinary C# code, although these are not always direct equivalents of Autofac features:

- generic bindings or factories replace run-time registration sources;
- generated partial methods replace resolve middleware;
- an explicit typed decorator chain in the object composition replaces decorator discovery;
- a lifetime scope for a unit of work is replaced by a generated `Owned<T>` optimized for a specific object composition;
- a dependent composition replaces a run-time module;
- ordinary properties or methods serve as composition roots instead of `Resolve<T>()`. [`Resolve<T>()` methods](https://github.com/DevTeam/Pure.DI/blob/2.5.2/readme/resolve-methods.md) are also supported and are highly efficient.

## Conclusion

Autofac is indeed a powerful classic DI container. Its maturity, ecosystem, and dynamic run-time configuration capabilities deserve recognition.

That does not mean Pure.DI is less capable. For a .NET application with a dependency graph known in advance, the picture is different:

- Pure.DI has counterparts for the core Autofac DI scenarios;
- the dependency graph for every used root is validated during the build;
- generated object-creation code does not require Reflection or dynamic code generation;
- composition roots are strongly typed;
- generated code can be read and debugged like ordinary C#;
- generated code does not need to box values for the sake of a classic DI container's universal API;
- `Owned<T>` gets a specialized, thoroughly tested resource-ownership model;
- performance is close to hand-written code following the Pure DI paradigm.

A more precise formulation of the distinction is therefore:

> Pure.DI implements the core scenarios of a classic DI container through code generation and validates the structure of statically known graphs during the build.

Autofac is justified when the composition structure must change at run time. When it is known in advance, Pure.DI builds and validates the graph, then generates the code. That work happens once, while the application is being built.
