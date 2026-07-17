### Applications
- Console
  - [Schrodinger's cat](readme/Console.md)
  - [Top-level statements](readme/ConsoleTopLevelStatements.md)
  - [Native AOT](readme/ConsoleNativeAOT.md)
  - [Entity Framework](readme/EntityFramework.md)
- [Unity](readme/Unity.md)
- UI
  - [MAUI](readme/Maui.md)
  - [WPF](readme/Wpf.md)
  - [Avalonia](readme/Avalonia.md)
  - [Uno Platform](readme/UnoApp.md)
  - [Win Forms Net Core](readme/WinFormsAppNetCore.md)
  - [Win Forms](readme/WinFormsApp.md)
- Web
  - [Web](readme/WebApp.md)
  - [Minimal Web API](readme/MinimalWebAPI.md)
  - [Web API](readme/WebAPI.md)
  - [gRPC service](readme/GrpcService.md)
  - [Blazor Server](readme/BlazorServerApp.md)
  - [Blazor WebAssembly](readme/BlazorWebAssemblyApp.md)
    - [https://devteam.github.io/Pure.DI/](https://devteam.github.io/Pure.DI/)
- GitHub repos with examples
  - [Schrodinger's cat](https://github.com/DevTeam/Pure.DI.Example)
  - [How to use Pure.DI to create and test libraries](https://github.com/DevTeam/Pure.DI.Solution)

> [!TIP]
> Examples that host an application on the Microsoft DI infrastructure (Web, MAUI, WPF, Avalonia, etc.) use the [Pure.DI.MS](https://www.nuget.org/packages/Pure.DI.MS) add-on package, which allows a Pure.DI composition to provide dependencies through `IServiceCollection`.

## Project template

The [Pure.DI.Templates](https://www.nuget.org/packages/Pure.DI.Templates) package adds `dotnet new` templates for creating a ready-to-run application with Pure.DI from the command line.

<details>
<summary>Creating and running a project</summary>

Install the templates:

```shell
dotnet new install Pure.DI.Templates
```

Create a "Sample" console application from the __di__ template:

```shell
dotnet new di -o ./Sample
```

Run it:

```shell
dotnet run --project Sample
```

For more information about the template, please see [this page](https://github.com/DevTeam/Pure.DI/wiki/Project-templates).

</details>

## Composition setup

This section explains how to configure a composition: declare bindings, choose lifetimes, define composition roots, and pass values into the object graph. The topics below are arranged in the order in which they are usually needed — expand the ones you are interested in.

Each composition is configured via `DI.Setup(string compositionTypeName)`:

```c#
DI.Setup("Composition")
    .Bind<IDependency>().To<Dependency>()
    .Bind<IService>().To<Service>()
    .Root<IService>("Root");
```

Based on this setup, Pure.DI generates a partial class with the specified name at compile time — see [Generated code](#generated-code) for what exactly is produced.

<details>
<summary>Setup arguments</summary>

The first parameter is used to specify the name of the composition class. All sets with the same name will be combined to create one composition class. Alternatively, this name may contain a namespace, e.g. a composition class is generated for `Sample.Composition`:

```c#
namespace Sample
{
    partial class Composition
    {
        ...
    }
}
```

The _compositionTypeName_ parameter can be omitted:

- If the setup is performed inside a partial class, the composition will be created for that partial class.
- For a class with composition kind `CompositionKind.Global`, see [this example](readme/global-compositions.md).

The second optional parameter may have multiple values to determine the kind of composition.

### CompositionKind.Public

This value is used by default. If this value is specified, a normal composition class will be created.

### CompositionKind.Internal

If you specify this value, the class will not be generated, but this setup can be used by others as a base setup. For example:

```c#
DI.Setup("BaseComposition", CompositionKind.Internal)
    .Bind().To<Dependency>();

DI.Setup("Composition").DependsOn("BaseComposition")
    .Bind().To<Service>();
```

If the _CompositionKind.Public_ flag is set in the composition setup, it can also serve as the base for other compositions, as in the example above.

### CompositionKind.Global

No composition class will be created when this value is specified, but this setup is the base setup for all setups in the current project, and `DependsOn(...)` is not required.

</details>

<details>
<summary>Bindings</summary>

Bindings are the core mechanism of Pure.DI, used to define how types are created and which contracts they fulfill.

### Overview

#### For Implementations

To bind a contract to a specific implementation:

```c#
.Bind<Contract1>(tags).Bind<ContractN>(tags)
    .Tags(tags)
    .As(lifetime)
    .To<Implementation>()
```

Alternatively, you can bind multiple contracts at once:

```c#
.Bind<Contract1, Contract2>(tags)
    .To<Implementation>()
```

Example:

```c#
.Bind<IService>().To<Service>()
```

#### For Factories

To use a custom factory logic via `IContext`:

```c#
.Bind<Contract1>(tags).Bind<ContractN>(tags)
    .Tags(tags)
    .As(Lifetime)
    .To(ctx => {
        ctx.Inject(out Dependency Dependency)
        return new Implementation(dependency);
    })
```

Example:

```c#
.Bind<IService>().To(ctx => {
    ctx.Inject(out IDependency dependency);
    return new Service(dependency);
})
```

#### Override Depth in Factories

Use `Let` to keep an override at the current injection level:

```c#
DI.Setup(nameof(Composition))
    .Bind().To<int>(_ => 7)
    .Bind().To<Dependency>()
    .Bind().To<Service>(ctx =>
    {
        // Override only the immediate injection
        ctx.Let(42);
        ctx.Inject(out Service service);
        return service;
    })
    .Root<Service>("Service");
```

Override precedence:
- The nearest override wins for nested dependencies.
- If multiple overrides target the same type and tag in one factory, the last call wins.
- `Let` applies only to the current injection level.

#### For Simplified Factories

When you only need to inject specific dependencies without accessing the full context:

```c#
.Bind<Contract1>(tags).Bind<ContractN>(tags)
    .Tags(tags)
    .As(lifetime)
    .To<Implementation>((Dependency1 dep1, Dependency2 dep2) => new Implementation(dep1, dep2))
```

Example:

```c#
.Bind<IService>().To((IDependency dep) => new Service(dep))
```

### Implementation bindings

Implementation bindings allow for a more concise syntax where the implementation type itself serves as the contract or where you want the binder to automatically infer suitable base types and interfaces.

#### For Implementations

```c#
// Infers all suitable base types and interfaces automatically
.Bind(tags).Tags(tags).As(Lifetime).To<Implementation>()
```

Alternatively, you can use the implementation type as the contract:

```c#
.Bind().To<Implementation>()
```

Example:

```c#
.Bind().To<Service>()
```

#### For Factories

```c#
.Bind(tags).Tags(tags).To(ctx => new Implementation())
```

Example:

```c#
.Bind().To(ctx => new Service())
```

#### For Simplified Factories

```c#
.Bind(tags).Tags(tags).To((Dependency dep) => new Implementation(dep))
```

Example:

```c#
.Bind().To((IDependency dep) => new Service(dep))
```

### Implementation-level binding attributes

Bindings can also be declared on implementation types with registered binding metadata attributes such as `Bind`, `Type`, `Tag`, and `Lifetime`.

```c#
[Bind(typeof(IService)), Tag("main"), Lifetime(Lifetime.Singleton)]
class Service : IService;
```

The square-bracket attribute group is the binding boundary:

- attributes inside one `[ ... ]` group form one binding;
- contracts and tags in the same group are merged into that binding;
- lifetime can be specified only once in the same group;
- repeated lifetime metadata inside one group is a compilation error;
- separate `Bind` attribute groups create separate bindings for the same implementation type.

For example, the following declaration creates one singleton binding that can be resolved by either tag:

```c#
[Bind(typeof(IService)), Tag("main"), Tag("secondary"), Lifetime(Lifetime.Singleton)]
class Service : IService;
```

To create two independent bindings for the same implementation, place `Bind` attributes in separate square-bracket groups:

```c#
[Bind(typeof(IService), Lifetime.Singleton, "main")]
[Bind(typeof(IService), Lifetime.Singleton, "secondary")]
class Service : IService;
```

### Nullable reference type contracts

When nullable reference types are enabled, Pure.DI treats nullable annotations as part of the dependency contract while it builds the graph and generates code. This makes `T` and `T?` different contracts for bindings, arguments, factories, and generic contracts.

```c#
DI.Setup("Composition")
    .Bind<string>().To(_ => "required")
    .Bind<string?>().To(_ => (string?)null)
    .Bind<IBox<TT>>().To<Box<TT>>()
    .Bind<IBox<TT?>>().To<NullableBox<TT>>()
    .Root<IBox<string>>("RequiredBox")
    .Root<IBox<string?>>("OptionalBox");
```

Matching rules:
- An exact nullable match wins when both `T` and `T?` bindings exist.
- A non-null binding `T` can satisfy a nullable dependency `T?`.
- A nullable binding `T?` is not used for a non-null dependency `T`.
- The same rules apply to factory bindings, `ctx.Inject(...)`, `Override`, `Let`, composition arguments, root arguments, and open generic contracts such as `IBox<T>` and `IBox<T?>`.

For nullable generic arguments, make sure the generic type accepts nullable reference arguments. For example, prefer `where T : class?` when a contract such as `IBox<string?>` is valid.

`Resolve<T>()` preserves the nullable contract in generated code. Runtime methods `Resolve(Type)` and `Resolve(Type, tag)` receive only `System.Type`, so they cannot distinguish `T` from `T?`; Pure.DI reports warning `DIW011` when nullable and non-nullable roots have the same runtime type while Resolve methods are generated.

### Special types will not be added to bindings

By default, Pure.DI avoids binding to special types during auto-inference to prevent polluting the container with unintended bindings for types like `IDisposable`, `IEnumerable`, or `object`. Special types will not be added to bindings by default:

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

If you want to add your own special type, use the `SpecialType<T>()` call, for example:

```c#
.SpecialType<MonoBehaviour>()
.Bind().To<MyMonoBehaviourImplementation>()
// Now MonoBehaviour will not be added to the contracts
```

</details>

<details>
<summary>Lifetimes</summary>

Lifetimes control how long an object lives and how it is reused:

| Lifetime | One instance per | Disposal of disposable instances | Example |
|----------|------------------|----------------------------------|---------|
| `Transient` (default) | injection | not tracked — own it explicitly, e.g. via `Owned<T>` | [Transient](readme/transient.md) |
| `Singleton` | composition | disposed together with the composition | [Singleton](readme/singleton.md) |
| `Scoped` | scope | disposed together with the scope | [Scoped](readme/scoped.md) |
| `PerResolve` | composition root call (or a `Resolve`/`ResolveByTag` call) | not tracked — own it explicitly, e.g. via `Owned<T>` | [PerResolve](readme/perresolve.md) |
| `PerBlock` | code block — an allocation optimization, no strict uniqueness guarantee | not tracked | [PerBlock](readme/perblock.md) |

How to choose:
- Start with `Transient` and promote a binding to `Singleton` only for genuinely shared state (caches, configuration, connection pools).
- Use `Scoped` when the natural unit of sharing is a request, a session, or a unit of work — one `DbContext` per web request is the classic case.
- Use `PerResolve` when several consumers within one object graph must observe the same instance, but different graphs must not share it.
- Treat `PerBlock` as an optimization that reduces allocations by reusing an instance within one initialization block; do not rely on instance identity with it.

For tracking and disposing of `Transient`/`PerResolve` disposables, see [Tracking disposable instances per a composition root](readme/tracking-disposable-instances-per-a-composition-root.md).

### Scopes

A scope defines a boundary within which `Scoped` instances are shared: a scope is represented by a composition instance created from a parent composition, and disposing the scope disposes its scoped instances. See the [Scope](readme/scope.md), [Scope setup method](readme/scope-setup-method.md), and [Auto scoped](readme/auto-scoped.md) examples.

### Default lifetimes

You can set a default lifetime for all subsequent bindings in a setup:

```c#
.DefaultLifetime(Lifetime.Singleton)
// This will be a Singleton
.Bind<IInterface>().To<Implementation>()
```

Alternatively, you can set a default lifetime for a specific contract type:

```c#
.DefaultLifetime<IDisposable>(Lifetime.Singleton)
```

### Simplified lifetime-specific bindings

Pure.DI provides syntactic sugar for common lifetimes. These methods combine `Bind()`, `.Tags(tags)`, `As(Lifetime)`, and `To()` into a single call.

#### For Implementations

```c#
// Equivalent to Bind<T, T1, ...>(tags).As(Lifetime.Transient).To<Implementation>()
.Transient<T>(tags)
// or multiple types at once
.PerResolve<T, T1, ...>(tags)
```

Example:

```c#
.Transient<Service>()
.Singleton<Service2, Service3, Service4>()
```

#### For Factories

```c#
// Equivalent to Bind(tags).As(Lifetime.Singleton).To(ctx => ...)
.Singleton<Implementation>(ctx => new Implementation(), tags)
```

Example:

```c#
.Singleton<IService>(ctx => new Service())
```

#### For Simplified Factories

```c#
// Equivalent to Bind(tags).As(Lifetime.PerResolve).To((Dependency dep) => ...)
.PerResolve((Dependency dep) => new Implementation(dep), tags)
```

Example:

```c#
.PerResolve((IDependency dep) => new Service(dep))
```

Equivalent shortcuts exist for all lifetimes:

- `Transient<T>(...)`
- `Singleton<T>(...)`
- `Scoped<T>(...)`
- `PerResolve<T>(...)`
- `PerBlock<T>(...)`

</details>

<details>
<summary>Tags</summary>

Tags allow you to distinguish between multiple implementations of the same contract.

- Use `.Bind<T>(tags)` or `.Tags(tags)` to apply tags to a binding.
- Use the `[Tag(tag)]` attribute or `ctx.Resolve<T>(tag)` to consume a tagged dependency.

Example:

```c#
.Bind<IService>("MyTag").To<Service>()
```

See also:
- [Tags](readme/tags.md)
- [Smart tags](readme/smart-tags.md)
- [Tag attribute](readme/tag-attribute.md)

</details>

<details>
<summary>Composition roots</summary>

### Regular composition roots

To create an object graph quickly and conveniently, a set of properties (or methods) is formed. These properties/methods are here called composition roots. The type of a property/method is the type of the root object created by the composition. Accordingly, each invocation of a property/method leads to the creation of an object graph with a root element of this type.

```c#
DI.Setup("Composition")
    .Bind<IService>().To<Service>()
    .Root<IService>("MyService");

var composition = new Composition();
var service = composition.MyService;
service = composition.Resolve<IService>();
service = composition.Resolve(typeof(IService));
```

In this case, the property for the _IService_ type will be named _MyService_ and will be available for direct use. The result of its use will be the creation of an object graph with the root of _IService_ type:

```c#
public IService MyService
{
    get
    {
        ...
        return new Service(...);
    }
}
```

This is the [recommended way](https://blog.ploeh.dk/2011/07/28/CompositionRoot/) to create a composition root. A composition class can contain any number of roots.

In addition, the composition roots can be resolved using the `Resolve`/`ResolveByTag` methods:

```c#
service = composition.Resolve<IService>();
service = composition.Resolve(typeof(IService));
```

> [!TIP]
>- There is no limit to the number of roots, but you should consider limiting the number of roots. Ideally, an application should have a single composition root.
>- The name of the composition root is arbitrarily chosen depending on its purpose, but should follow C# property naming conventions since it becomes a property name in the composition class.
>- It is recommended that composition roots be resolved using normal properties or methods instead of `Resolve`/`ResolveByTag` methods.

### Anonymous composition roots

If the root name is empty, an anonymous composition root with a random name is created:

```c#
private IService RootM07D16di_0001
{
    get { ... }
}
```

These properties (or methods) have an arbitrary name and access modifier `private` and cannot be used directly from the code. Do not attempt to use them, as their names can change between builds. Anonymous composition roots can be resolved by `Resolve`/`ResolveByTag` methods:

```c#
DI.Setup("Composition")
    .Bind<IService>().To<Service>()
    .Root<IService>();

var composition = new Composition();
var service = composition.Resolve<IService>();
service = composition.Resolve(typeof(IService));
```

### Root arguments

When a root needs data that changes per call — not shared across the whole composition — use `RootArg<T>(name)`. The generated root becomes a **method** instead of a property, and the argument is passed at every call site:

```c#
DI.Setup("Composition")
    // RootArg is incompatible with Resolve methods — disable them when using RootArg
    .Hint(Hint.Resolve, "Off")
    .RootArg<Guid>("userId")
    .Bind<IUserService>().To<UserService>()
    .Root<IUserService>("GetUserService");

var composition = new Composition();
var service = composition.GetUserService(userId: Guid.NewGuid());
```

> [!NOTE]
> Because `RootArg` binds a value only for a specific root call, it is incompatible with `Resolve`/`ResolveByTag` methods. Disable them with `.Hint(Hint.Resolve, "Off")` when using root arguments.

See also: [Root arguments example](readme/root-arguments.md)

</details>

<details>
<summary>Passing values at runtime</summary>

Several mechanisms deliver a runtime value into the object graph. Choose by the moment the value becomes known and by how far it must travel:

| Mechanism | The value is known | The value is visible to | Example |
|-----------|--------------------|-------------------------|---------|
| `Arg<T>(name)` | when the composition is created | the whole composition | [Composition arguments](readme/composition-arguments.md) |
| `RootArg<T>(name)` | at each root call (the root becomes a method) | one root call | [Root arguments](readme/root-arguments.md) |
| `Func<TArg, T>` | at each factory call inside the graph | the instance created by that call | [Func with arguments](readme/func-with-arguments.md) |
| `ctx.Override(value)` | inside a factory | the dependency subtree created by that factory | [Overrides](readme/overrides.md) |

Rules of thumb:
- Configuration that is fixed for the lifetime of the application → `Arg<T>`.
- Per-call data for an entry point, such as a user or request id → `RootArg<T>`.
- A service creates many instances with different parameters → inject `Func<TArg, T>`.
- A factory must customize how nested dependencies are built → `ctx.Override(...)`.

</details>

<details>
<summary>Injection selection and execution priorities</summary>

Pure.DI treats constructor injection and member injection differently. Exactly one constructor is selected for an instance, while every eligible injection method, property, and field is applied. If the dependency graph for a preferred constructor cannot be resolved, Pure.DI continues with the next constructor candidate.

#### Constructor selection

Constructor candidates are considered in the following priority order:

| Priority | Rule                                                                                                                                                                                                                                          |
|:--------:|-----------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------|
|    1     | If at least one accessible constructor has [`OrdinalAttribute`](readme/constructor-ordinal-attribute.md), only constructors marked with `Ordinal` participate. Lower ordinal values are tried first.                                          |
|    2     | Otherwise, constructors with a higher [`OverloadResolutionPriorityAttribute`](readme/overload-resolution-priority.md) value are preferred. A constructor without the attribute has priority `0`; negative values de-prioritize a constructor. |
|    3     | A constructor with more injection parameters is preferred.                                                                                                                                                                                    |
|    4     | A more accessible constructor is preferred: `public` before `internal`.                                                                                                                                                                       |
|    5     | When neither `Ordinal` mode nor an explicit `OverloadResolutionPriorityAttribute` is active, a primary constructor is preferred as the final tie-breaker.                                                                                     |

`OrdinalAttribute` is the explicit Pure.DI override and takes precedence over `OverloadResolutionPriorityAttribute`. Constructors with the same ordinal use the number of injection parameters and accessibility as secondary criteria; `OverloadResolutionPriorityAttribute` and the implicit primary-constructor preference remain disabled in this mode. For a primary constructor, constructor attributes use the `method:` target, for example `[method: Ordinal(0)]` or `[method: OverloadResolutionPriority(1)]`. If candidates are still equal, do not rely on their declaration or Roslyn symbol order; use distinct ordinal values when the choice affects behavior.

#### Method, property, and field injection

A member participates in injection when it is accessible and is marked by a recognized injection attribute such as `Ordinal`, `Tag`, or `Type`. An injection method can be selected by an attribute on the method or on one of its parameters. A method-level ordinal takes precedence; otherwise, the lowest ordinal specified on its parameters becomes the method ordinal. Mutable `required` properties and fields participate automatically. A selected `init` property is assigned in the object initializer.

Injection is generated in this execution order:

| Priority | Injection stage                                                                                                |
|:--------:|----------------------------------------------------------------------------------------------------------------|
|    1     | Constructor arguments are resolved and the selected constructor is invoked.                                    |
|    2     | Required fields are assigned in the object initializer, ordered by ascending `Ordinal`.                        |
|    3     | Required or selected `init` properties are assigned in the object initializer, ordered by ascending `Ordinal`. |
|    4     | Remaining fields, properties, and methods are processed together in ascending `Ordinal` order.                 |

Lower ordinal values therefore run earlier, and negative values are valid. A member selected only by `Tag` or `Type`, and a `required` member without an explicit ordinal, receives the default ordinal `int.MaxValue` and is processed after explicitly ordered members in the same stage. For equal ordinals, regular members are processed deterministically: fields first, then properties, then methods; declaration order is preserved within each kind. Members of the same kind declared on a derived type are processed before members inherited from its base types. Equal ordinals are valid and do not produce a diagnostic.

These rules control when the generated assignment or method call is performed. Pure.DI may construct the member dependencies earlier while building the object graph, so do not use `Ordinal` to order dependency-constructor side effects. Put order-sensitive work in the member setter or injection method itself.

</details>

<details>
<summary>Generics and marker types</summary>

Instead of open generics, Pure.DI uses marker types such as `TT`, `TT1`, `TT2`, or `TTS` to define bindings for generic types. A marker is a placeholder that is replaced with an actual type argument while the object graph is being built, so every closed generic graph is still verified and generated at compile time:

```c#
DI.Setup(nameof(Composition))
    // Binds IBox<T> for any type argument
    .Bind<IBox<TT>>().To<CardboardBox<TT>>()
    // Binds IHolder<T> for any value type argument
    .Bind<IHolder<TTS>>().To<StructHolder<TTS>>()
    .Root<Program>("Root");
```

Numbered markers (`TT1`, `TT2`, ...) represent different type parameters within one binding. Constrained markers such as `TTS` (value types) or `TTDisposable` (types implementing `IDisposable`) narrow the applicability of a binding. Custom marker types with arbitrary constraints can be registered using the `GenericTypeArgumentAttribute` — see [Custom generic argument](readme/custom-generic-argument.md).

See also the [Generics](#generics) examples, in particular [Generics](readme/generics.md), [Generic composition roots](readme/generic-composition-roots.md), and [Generic roots](readme/generic-roots.md).

</details>

<details>
<summary>Interface generation</summary>

Pure.DI can generate interfaces from concrete classes, so a class can remain the implementation while consumers depend on a generated contract. Declare a partial interface and mark the implementation or individual members with `GenerateInterface`:

```c#
public partial interface IEmailSender;

[GenerateInterface]
public class EmailSender : IEmailSender
{
    public string Provider => "smtp";

    public string Send(string address) => $"sent:{address}";
}

public class App(IEmailSender sender);
```

The generated interface preserves the public contract surface, including properties, methods, events, nullable annotations, generic members, and generic constraints. Members marked with `IgnoreInterface` are excluded from every generated interface:

```c#
[GenerateInterface]
public class ApiClient : IApiClient
{
    public string Endpoint => "https://api.contoso.com";

    [IgnoreInterface]
    public string GetAccessToken() => "internal-token";
}
```

The generated contract can also be customized with `namespaceName` and `interfaceName`, or split into several interfaces from one implementation:

```c#
public class Gateway : IReadGateway, IWriteGateway
{
    [GenerateInterface(interfaceName: nameof(IReadGateway))]
    public string Get(string path) => $"GET:{path}";

    [GenerateInterface(interfaceName: nameof(IWriteGateway))]
    public void Post(string path) {}
}
```

See also:
- [Generate an interface from a class](readme/generate-an-interface-from-a-class.md)
- [Ignore members in the generated interface](readme/ignore-members-in-the-generated-interface.md)
- [Generate interfaces with generics](readme/generate-interfaces-with-generics.md)
- [Customize the generated interface](readme/customize-the-generated-interface.md)
- [Generate several interfaces from one class](readme/generate-several-interfaces-from-one-class.md)
- [Control generated interfaces by members](readme/control-generated-interfaces-by-members.md)

</details>

<details>
<summary>Comments</summary>

Pure.DI can copy comments from setup calls into generated documentation comments for the composition class, composition arguments, and composition roots.
When no user comment is provided, Pure.DI generates documentation for the generated API so the composition can be inspected from IntelliSense.

Generated comments describe:

- The composition roots exposed by the generated composition class.
- The root contract, implementation type, tag, and lifetime.
- Whether a root is a property or a method.
- Composition constructor parameters created from used `Arg<T>(...)` values.
- Root method parameters created from used `RootArg<T>(...)` values.
- `Resolve`/`ResolveByTag` limitations for roots that require root arguments.
- `Dispose`/`DisposeAsync` behavior for tracked singleton and scoped disposable instances.

Use regular `//` comments before API calls when you want Pure.DI to include the text in the generated documentation:

```c#
DI.Setup("Composition")
    .Bind<IService>().To<Service>()
    // Provides the main service.
    .Root<IService>("Service");
```

For `Setup(...)` and composition roots, XML documentation comments written with `///` replace the automatically generated documentation:

```c#
/// <summary>
/// Application composition.
/// </summary>
DI.Setup("Composition")
    .Bind<IService>().To<Service>()
    /// <summary>
    /// Provides the main service.
    /// </summary>
    .Root<IService>("Service");
```

The generated root member keeps the user-defined XML documentation:

```c#
/// <summary>
/// Provides the main service.
/// </summary>
public IService Service
{
    get { ... }
}
```

For other setup calls, such as `Arg<T>(...)`, comments are used as documentation text in the generated constructor documentation. Use regular `//` comments there unless you want XML markup to be shown as text.

To suppress generated documentation comments, turn comments off for the setup:

```c#
DI.Setup("Composition")
    .Hint(Hint.Comments, "Off")
    .Bind<IService>().To<Service>()
    .Root<IService>("Service");
```

</details>

<details>
<summary>Setup hints</summary>

Hints are per-setup switches and filters that control how the composition code is generated (for example, whether `Resolve` methods are emitted, thread-safety, diagnostics hooks, or `ToString()` diagrams). Think of them as generator settings that let you trade off features, diagnostics, and compile-time cost.

Guidelines:
- Prefer the fluent API for discoverability and refactoring safety; use comment directives for quick local overrides.
- Hints affect only the `DI.Setup(...)` they are attached to (unless you use a global composition).
- If you set the same hint multiple times, the last value wins.

```c#
DI.Setup("Composition")
    .Hint(Hint.Resolve, "Off")
    .Hint(Hint.ThreadSafe, "Off")
    .Hint(Hint.ToString, "On")
    ...
```

As an alternative to the fluent API, you can place comment directives before the _Setup_ method in the form `hint = value`. For example:

```c#
// Resolve = Off
// ThreadSafe = Off
DI.Setup("Composition")
    ...
```

Both approaches can be mixed when it improves readability:

```c#
// Resolve = Off
DI.Setup("Composition")
    .Hint(Hint.ThreadSafe, "Off")
    ...
```

| Hint                                                                                                                                 | Values                                     | C# version | Default   |
|--------------------------------------------------------------------------------------------------------------------------------------|--------------------------------------------|------------|-----------|
| [Resolve](#resolve-hint)                                                                                                             | _On_ or _Off_                              |            | _On_      |
| [OnNewInstance](#onnewinstance-hint)                                                                                                 | _On_ or _Off_                              | 9.0        | _Off_     |
| [OnNewInstancePartial](#onnewinstancepartial-hint)                                                                                          | _On_ or _Off_                              |            | _On_      |
| [OnNewInstanceImplementationTypeNameRegularExpression](#onnewinstanceimplementationtypenameregularexpression-hint)                   | Regular expression                         |            | .+        |
| [OnNewInstanceImplementationTypeNameWildcard](#onnewinstanceimplementationtypenamewildcard-hint)                                     | Wildcard                                   |            | *         |
| [OnNewInstanceTagRegularExpression](#onnewinstancetagregularexpression-hint)                                                         | Regular expression                         |            | .+        |
| [OnNewInstanceTagWildcard](#onnewinstancetagwildcard-hint)                                                                           | Wildcard                                   |            | *         |
| [OnNewInstanceLifetimeRegularExpression](#onnewinstancelifetimeregularexpression-hint)                                               | Regular expression                         |            | .+        |
| [OnNewInstanceLifetimeWildcard](#onnewinstancelifetimewildcard-hint)                                                                 | Wildcard                                   |            | *         |
| [OnDependencyInjection](#ondependencyinjection-hint)                                                                                 | _On_ or _Off_                              | 9.0        | _Off_     |
| [OnDependencyInjectionPartial](#ondependencyinjectionpartial-hint)                                                                   | _On_ or _Off_                              |            | _On_      |
| [OnDependencyInjectionImplementationTypeNameRegularExpression](#ondependencyinjectionimplementationtypenameregularexpression-hint)   | Regular expression                         |            | .+        |
| [OnDependencyInjectionImplementationTypeNameWildcard](#ondependencyinjectionimplementationtypenamewildcard-hint)                     | Wildcard                                   |            | *         |
| [OnDependencyInjectionContractTypeNameRegularExpression](#ondependencyinjectioncontracttypenameregularexpression-hint)               | Regular expression                         |            | .+        |
| [OnDependencyInjectionContractTypeNameWildcard](#ondependencyinjectioncontracttypenamewildcard-hint)                                 | Wildcard                                   |            | *         |
| [OnDependencyInjectionTagRegularExpression](#ondependencyinjectiontagregularexpression-hint)                                         | Regular expression                         |            | .+        |
| [OnDependencyInjectionTagWildcard](#ondependencyinjectiontagwildcard-hint)                                                           | Wildcard                                   |            | *         |
| [OnDependencyInjectionLifetimeRegularExpression](#ondependencyinjectionlifetimeregularexpression-hint)                               | Regular expression                         |            | .+        |
| [OnDependencyInjectionLifetimeWildcard](#ondependencyinjectionlifetimewildcard-hint)                                                 | Wildcard                                   |            | *         |
| [OnCannotResolve](#oncannotresolve-hint)                                                                                             | _On_ or _Off_                              | 9.0        | _Off_     |
| [OnCannotResolvePartial](#oncannotresolvepartial-hint)                                                                               | _On_ or _Off_                              |            | _On_      |
| [OnCannotResolveContractTypeNameRegularExpression](#oncannotresolvecontracttypenameregularexpression-hint)                           | Regular expression                         |            | .+        |
| [OnCannotResolveContractTypeNameWildcard](#oncannotresolvecontracttypenamewildcard-hint)                                             | Wildcard                                   |            | *         |
| [OnCannotResolveTagRegularExpression](#oncannotresolvetagregularexpression-hint)                                                     | Regular expression                         |            | .+        |
| [OnCannotResolveTagWildcard](#oncannotresolvetagwildcard-hint)                                                                       | Wildcard                                   |            | *         |
| [OnCannotResolveLifetimeRegularExpression](#oncannotresolvelifetimeregularexpression-hint)                                           | Regular expression                         |            | .+        |
| [OnCannotResolveLifetimeWildcard](#oncannotresolvelifetimewildcard-hint)                                                             | Wildcard                                   |            | *         |
| [OnNewRoot](#onnewroot-hint)                                                                                                         | _On_ or _Off_                              |            | _Off_     |
| [OnNewRootPartial](#onnewrootpartial-hint)                                                                                           | _On_ or _Off_                              |            | _On_      |
| [ToString](#tostring-hint)                                                                                                           | _On_ or _Off_                              |            | _Off_     |
| [ThreadSafe](#threadsafe-hint)                                                                                                       | _On_ or _Off_                              |            | _On_      |
| [ResolveMethodModifiers](#resolvemethodmodifiers-hint)                                                                               | Method modifier                            |            | _public_  |
| [ResolveMethodName](#resolvemethodname-hint)                                                                                         | Method name                                |            | _Resolve_ |
| [ResolveByTagMethodModifiers](#resolvebytagmethodmodifiers-hint)                                                                     | Method modifier                            |            | _public_  |
| [ResolveByTagMethodName](#resolvebytagmethodname-hint)                                                                               | Method name                                |            | _Resolve_ |
| [ObjectResolveMethodModifiers](#objectresolvemethodmodifiers-hint)                                                                   | Method modifier                            |            | _public_  |
| [ObjectResolveMethodName](#objectresolvemethodname-hint)                                                                             | Method name                                |            | _Resolve_ |
| [ObjectResolveByTagMethodModifiers](#objectresolvebytagmethodmodifiers-hint)                                                         | Method modifier                            |            | _public_  |
| [ObjectResolveByTagMethodName](#objectresolvebytagmethodname-hint)                                                                   | Method name                                |            | _Resolve_ |
| [DisposeMethodModifiers](#disposemethodmodifiers-hint)                                                                               | Method modifier                            |            | _public_  |
| [DisposeAsyncMethodModifiers](#disposeasyncmethodmodifiers-hint)                                                                     | Method modifier                            |            | _public_  |
| [FormatCode](#formatcode-hint)                                                                                                       | _On_ or _Off_                              |            | _Off_     |
| [SeverityOfNotImplementedContract](#severityofnotimplementedcontract-hint)                                                           | _Error_ or _Warning_ or _Info_ or _Hidden_ |            | _Error_   |
| [Comments](#comments-hint)                                                                                                           | _On_ or _Off_                              |            | _On_      |
| [SkipDefaultConstructor](#skipdefaultconstructor-hint)                                                                               | _On_ or _Off_                              |            | _Off_     |
| [SkipDefaultConstructorImplementationTypeNameRegularExpression](#skipdefaultconstructorimplementationtypenameregularexpression-hint) | Regular expression                         |            | .+        |
| [SkipDefaultConstructorImplementationTypeNameWildcard](#skipdefaultconstructorimplementationtypenamewildcard-hint)                   | Wildcard                                   |            | *         |
| [SkipDefaultConstructorLifetimeRegularExpression](#skipdefaultconstructorlifetimeregularexpression-hint)                             | Regular expression                         |            | .+        |
| [SkipDefaultConstructorLifetimeWildcard](#skipdefaultconstructorlifetimewildcard-hint)                                               | Wildcard                                   |            | *         |
| [DisableAutoBinding](#disableautobinding-hint)                                                                                       | _On_ or _Off_                              |            | _Off_     |
| [DisableAutoBindingImplementationTypeNameRegularExpression](#disableautobindingimplementationtypenameregularexpression-hint)         | Regular expression                         |            | .+        |
| [DisableAutoBindingImplementationTypeNameWildcard](#disableautobindingimplementationtypenamewildcard-hint)                           | Wildcard                                   |            | *         |
| [DisableAutoBindingLifetimeRegularExpression](#disableautobindinglifetimeregularexpression-hint)                                     | Regular expression                         |            | .+        |
| [DisableAutoBindingLifetimeWildcard](#disableautobindinglifetimewildcard-hint)                                                       | Wildcard                                   |            | *         |
| [LightweightAnonymousRoot](#lightweightanonymousroot-hint)                                                                           | _On_ or _Off_                              |            | _On_      |
| [SystemThreadingLock](#systemthreadinglock-hint)                                                                                     | _On_ or _Off_                              |            | _On_      |
| [ScopeMethodName](#scopemethodname-hint)                                                                                             | Method name                                |            |           |

The list of hints will be gradually expanded to meet the needs and desires for fine-tuning code generation. Please feel free to add your ideas.

### Resolve Hint

Controls whether [_Resolve_ and _ResolveByTag_ methods](#resolve-and-resolvebytag-methods) are generated. By default they are enabled. Turn this _Off_ to reduce generated code size and compilation time, but note that anonymous roots are not created and only explicitly named roots are available. When disabled, always define public roots via `Root<T>(string name)`.

```c#
// Resolve = Off
DI.Setup("Composition")
    .Bind<IService>().To<Service>()
    .Root<IService>("Root");
```

### OnNewInstance Hint

Enables the `OnNewInstance` callback to observe or replace newly created instances (for example, logging, diagnostics, or decoration). It is _Off_ by default to avoid overhead.

```c#
internal partial class Composition
{
    partial void OnNewInstance<T>(ref T value, object? tag, object lifetime) =>
        Console.WriteLine($"'{typeof(T)}'('{tag}') created.");
}
```

You can also replace the created instance with a `T` type, where `T` is the actual type of the created instance. To minimize performance loss when calling _OnNewInstance_, use the three hints below.

### OnNewInstancePartial Hint

Controls whether the `OnNewInstance` partial method signature is generated when `OnNewInstance` is enabled. Turn it _Off_ if you want to enable filtering hints without emitting the partial method.

```c#
// OnNewInstance = On
// OnNewInstancePartial = Off
DI.Setup("Composition")
    .Bind<IService>().To<Service>();
```

### OnNewInstanceImplementationTypeNameRegularExpression Hint

This is a regular expression for filtering by instance type name. This hint is useful when _OnNewInstance_ is in _On_ state and it is necessary to limit the set of types for which the _OnNewInstance_ method will be called.

```c#
// OnNewInstance = On
// OnNewInstanceImplementationTypeNameRegularExpression = .*Service
DI.Setup("Composition")
    .Bind<IService>().To<Service>();
```

### OnNewInstanceImplementationTypeNameWildcard Hint

This is a Wildcard for filtering by instance type name. This hint is useful when _OnNewInstance_ is in _On_ state and it is necessary to limit the set of types for which the _OnNewInstance_ method will be called.

```c#
// OnNewInstance = On
// OnNewInstanceImplementationTypeNameWildcard = *Service
DI.Setup("Composition")
    .Bind<IService>().To<Service>();
```

### OnNewInstanceTagRegularExpression Hint

This is a regular expression for filtering by _tag_. This hint is also useful when _OnNewInstance_ is in _On_ state and it is necessary to limit the set of _tags_ for which the _OnNewInstance_ method will be called.

```c#
// OnNewInstance = On
// OnNewInstanceTagRegularExpression = Internal|Public
DI.Setup("Composition")
    .Bind<IService>("Internal").To<Service>();
```

### OnNewInstanceTagWildcard Hint

This is a wildcard for filtering by _tag_. This hint is also useful when _OnNewInstance_ is in _On_ state and it is necessary to limit the set of _tags_ for which the _OnNewInstance_ method will be called.

```c#
// OnNewInstance = On
// OnNewInstanceTagWildcard = *Internal
DI.Setup("Composition")
    .Bind<IService>("Internal").To<Service>();
```

### OnNewInstanceLifetimeRegularExpression Hint

This is a regular expression for filtering by _lifetime_. This hint is also useful when _OnNewInstance_ is in _On_ state and it is necessary to restrict the set of _lifetimes_ for which the _OnNewInstance_ method will be called.

```c#
// OnNewInstance = On
// OnNewInstanceLifetimeRegularExpression = Singleton|Scoped
DI.Setup("Composition")
    .Bind<IService>().As(Lifetime.Singleton).To<Service>();
```

### OnNewInstanceLifetimeWildcard Hint

This is a wildcard for filtering by _lifetime_. This hint is also useful when _OnNewInstance_ is in _On_ state and it is necessary to restrict the set of _lifetimes_ for which the _OnNewInstance_ method will be called.

```c#
// OnNewInstance = On
// OnNewInstanceLifetimeWildcard = *Singleton
DI.Setup("Composition")
    .Bind<IService>().As(Lifetime.Singleton).To<Service>();
```

### OnDependencyInjection Hint

Enables the `OnDependencyInjection` callback that can intercept dependency injection and return an alternative instance. Use for advanced scenarios like interception, conditional injection, or diagnostics. It is _Off_ by default.

```c#
// OnDependencyInjection = On
// OnDependencyInjectionPartial = Off
// OnDependencyInjectionContractTypeNameRegularExpression = ICalculator[\d]{1}
// OnDependencyInjectionTagRegularExpression = Abc
DI.Setup("Composition")
    ...
```

### OnDependencyInjectionPartial Hint

Controls whether the `OnDependencyInjection` partial method is generated. Because it returns a value, the method must be implemented when generated. Turn it _Off_ to rely on filters without emitting the method body.

```c#
// OnDependencyInjection = On
// OnDependencyInjectionContractTypeNameRegularExpression = ICalculator[\d]{1}
// OnDependencyInjectionTagRegularExpression = Abc
DI.Setup("Composition")
    ...
```

To minimize performance loss when calling _OnDependencyInjection_, use the three tips below.

### OnDependencyInjectionImplementationTypeNameRegularExpression Hint

This is a regular expression for filtering by instance type name. This hint is useful when _OnDependencyInjection_ is in _On_ state and it is necessary to restrict the set of types for which the _OnDependencyInjection_ method will be called.

```c#
// OnDependencyInjection = On
// OnDependencyInjectionImplementationTypeNameRegularExpression = .*Service
DI.Setup("Composition")
    .Bind<IService>().To<Service>();
```

### OnDependencyInjectionImplementationTypeNameWildcard Hint

This is a wildcard for filtering by instance type name. This hint is useful when _OnDependencyInjection_ is in _On_ state and it is necessary to restrict the set of types for which the _OnDependencyInjection_ method will be called.

```c#
// OnDependencyInjection = On
// OnDependencyInjectionImplementationTypeNameWildcard = *Service
DI.Setup("Composition")
    .Bind<IService>().To<Service>();
```

### OnDependencyInjectionContractTypeNameRegularExpression Hint

This is a regular expression for filtering by the name of the resolving type. This hint is also useful when _OnDependencyInjection_ is in _On_ state and it is necessary to limit the set of permissive types for which the _OnDependencyInjection_ method will be called.

```c#
// OnDependencyInjection = On
// OnDependencyInjectionContractTypeNameRegularExpression = I.*Service
DI.Setup("Composition")
    .Bind<IService>().To<Service>();
```

### OnDependencyInjectionContractTypeNameWildcard Hint

This is a wildcard for filtering by the name of the resolving type. This hint is also useful when _OnDependencyInjection_ is in _On_ state and it is necessary to limit the set of permissive types for which the _OnDependencyInjection_ method will be called.

```c#
// OnDependencyInjection = On
// OnDependencyInjectionContractTypeNameWildcard = I*Service
DI.Setup("Composition")
    .Bind<IService>().To<Service>();
```

### OnDependencyInjectionTagRegularExpression Hint

This is a regular expression for filtering by _tag_. This hint is also useful when _OnDependencyInjection_ is in the _On_ state and you want to limit the set of _tags_ for which the _OnDependencyInjection_ method will be called.

```c#
// OnDependencyInjection = On
// OnDependencyInjectionTagRegularExpression = Internal|Public
DI.Setup("Composition")
    .Bind<IService>("Internal").To<Service>();
```

### OnDependencyInjectionTagWildcard Hint

This is a wildcard for filtering by _tag_. This hint is also useful when _OnDependencyInjection_ is in the _On_ state and you want to limit the set of _tags_ for which the _OnDependencyInjection_ method will be called.

```c#
// OnDependencyInjection = On
// OnDependencyInjectionTagWildcard = *Internal
DI.Setup("Composition")
    .Bind<IService>("Internal").To<Service>();
```

### OnDependencyInjectionLifetimeRegularExpression Hint

This is a regular expression for filtering by _lifetime_. This hint is also useful when _OnDependencyInjection_ is in _On_ state and it is necessary to restrict the set of _lifetime_ for which the _OnDependencyInjection_ method will be called.

```c#
// OnDependencyInjection = On
// OnDependencyInjectionLifetimeRegularExpression = Singleton|Scoped
DI.Setup("Composition")
    .Bind<IService>().As(Lifetime.Singleton).To<Service>();
```

### OnDependencyInjectionLifetimeWildcard Hint

This is a wildcard for filtering by _lifetime_. This hint is also useful when _OnDependencyInjection_ is in _On_ state and it is necessary to restrict the set of _lifetime_ for which the _OnDependencyInjection_ method will be called.

```c#
// OnDependencyInjection = On
// OnDependencyInjectionLifetimeWildcard = *Singleton
DI.Setup("Composition")
    .Bind<IService>().As(Lifetime.Singleton).To<Service>();
```

### OnCannotResolve Hint

Enables the `OnCannotResolve<T>(...)` callback, allowing you to provide a fallback instance or custom error handling when a root cannot be resolved. It is _Off_ by default. The generated method must be implemented because it returns a value.

```c#
// OnCannotResolve = On
// OnCannotResolveContractTypeNameRegularExpression = string|DateTime
// OnDependencyInjectionTagRegularExpression = null
DI.Setup("Composition")
    ...
```

To avoid missing failed bindings by mistake, use the two relevant hints below.

### OnCannotResolvePartial Hint

Controls whether the `OnCannotResolve<T>(...)` partial method is generated when `OnCannotResolve` is enabled. Turn it _Off_ to enable filters without emitting the partial method.

```c#
// OnCannotResolve = On
// OnCannotResolvePartial = Off
// OnCannotResolveContractTypeNameRegularExpression = string|DateTime
// OnDependencyInjectionTagRegularExpression = null
DI.Setup("Composition")
    ...
```

To avoid missing failed bindings by mistake, use the two relevant hints below.

### OnNewRoot Hint

Enables the static `OnNewRoot<TContract, T>(...)` callback that runs when a new root is registered. This is useful for logging or custom validation, but it bypasses some dependency resolution checks.

```c#
// OnNewRoot = On
DI.Setup("Composition")
    ...
```

Be careful, this hint disables checks for the ability to resolve dependencies!

### OnNewRootPartial Hint

Controls whether the `OnNewRoot<TContract, T>(...)` partial method is generated when `OnNewRoot` is enabled.

```c#
// OnNewRootPartial = Off
DI.Setup("Composition")
    ...
```

### OnCannotResolveContractTypeNameRegularExpression Hint

This is a regular expression for filtering by the name of the resolving type. This hint is also useful when _OnCannotResolve_ is in _On_ state and it is necessary to limit the set of resolving types for which the _OnCannotResolve_ method will be called.

```c#
// OnCannotResolve = On
// OnCannotResolveContractTypeNameRegularExpression = string|DateTime
DI.Setup("Composition")
    .Bind<IService>().To<Service>();
```

### OnCannotResolveContractTypeNameWildcard Hint

This is a wildcard for filtering by the name of the resolving type. This hint is also useful when _OnCannotResolve_ is in _On_ state and it is necessary to limit the set of resolving types for which the _OnCannotResolve_ method will be called.

```c#
// OnCannotResolve = On
// OnCannotResolveContractTypeNameWildcard = *Service
DI.Setup("Composition")
    .Bind<IService>().To<Service>();
```

### OnCannotResolveTagRegularExpression Hint

This is a regular expression for filtering by _tag_. This hint is also useful when _OnCannotResolve_ is in _On_ state and it is necessary to limit the set of _tags_ for which the _OnCannotResolve_ method will be called.

```c#
// OnCannotResolve = On
// OnCannotResolveTagRegularExpression = Internal|Public
DI.Setup("Composition")
    .Bind<IService>("Internal").To<Service>();
```

### OnCannotResolveTagWildcard Hint

This is a wildcard for filtering by _tag_. This hint is also useful when _OnCannotResolve_ is in _On_ state and it is necessary to limit the set of _tags_ for which the _OnCannotResolve_ method will be called.

```c#
// OnCannotResolve = On
// OnCannotResolveTagWildcard = *Internal
DI.Setup("Composition")
    .Bind<IService>("Internal").To<Service>();
```

### OnCannotResolveLifetimeRegularExpression Hint

This is a regular expression for filtering by _lifetime_. This hint is also useful when _OnCannotResolve_ is in the _On_ state and it is necessary to restrict the set of _lifetimes_ for which the _OnCannotResolve_ method will be called.

```c#
// OnCannotResolve = On
// OnCannotResolveLifetimeRegularExpression = Singleton|Scoped
DI.Setup("Composition")
    .Bind<IService>().As(Lifetime.Singleton).To<Service>();
```

### OnCannotResolveLifetimeWildcard Hint

This is a wildcard for filtering by _lifetime_. This hint is also useful when _OnCannotResolve_ is in the _On_ state and it is necessary to restrict the set of _lifetimes_ for which the _OnCannotResolve_ method will be called.

```c#
// OnCannotResolve = On
// OnCannotResolveLifetimeWildcard = *Singleton
DI.Setup("Composition")
    .Bind<IService>().As(Lifetime.Singleton).To<Service>();
```

### ToString Hint

Controls generation of `ToString()` that returns a Mermaid class diagram of the composition. Useful for documentation and debugging, but disabled by default to reduce generated code.

```c#
// ToString = On
DI.Setup("Composition")
    .Bind<IService>().To<Service>()
    .Root<IService>("MyService");

var composition = new Composition();
string classDiagram = composition.ToString();
```

### ThreadSafe Hint

Controls whether object graph creation uses synchronization. It is _On_ by default for safety. Turn it _Off_ if you know composition creation is single-threaded and want a small performance gain.

```c#
// ThreadSafe = Off
DI.Setup("Composition")
    .Bind<IService>().To<Service>()
    .Root<IService>("MyService");
```

### ResolveMethodModifiers Hint

Overrides the modifiers of the `public T Resolve<T>()` method.

```c#
DI.Setup("Composition")
    .Hint(Hint.ResolveMethodModifiers, "internal")
    .Bind<IService>().To<Service>()
    .Root<IService>("Root");
```

### ResolveMethodName Hint

Overrides the method name for `public T Resolve<T>()`.

```c#
DI.Setup("Composition")
    .Hint(Hint.ResolveMethodName, "GetRoot")
    .Bind<IService>().To<Service>()
    .Root<IService>("Root");
```

### ResolveByTagMethodModifiers Hint

Overrides the modifiers of the `public T Resolve<T>(object? tag)` method.

```c#
DI.Setup("Composition")
    .Hint(Hint.ResolveByTagMethodModifiers, "internal")
    .Bind<IService>().To<Service>()
    .Root<IService>("Root");
```

### ResolveByTagMethodName Hint

Overrides the method name for `public T Resolve<T>(object? tag)`.

```c#
DI.Setup("Composition")
    .Hint(Hint.ResolveByTagMethodName, "GetRootByTag")
    .Bind<IService>().To<Service>()
    .Root<IService>("Root");
```

### ObjectResolveMethodModifiers Hint

Overrides the modifiers of the `public object Resolve(Type type)` method.

```c#
DI.Setup("Composition")
    .Hint(Hint.ObjectResolveMethodModifiers, "internal")
    .Bind<IService>().To<Service>()
    .Root<IService>("Root");
```

### ObjectResolveMethodName Hint

Overrides the method name for `public object Resolve(Type type)`.

```c#
DI.Setup("Composition")
    .Hint(Hint.ObjectResolveMethodName, "GetObject")
    .Bind<IService>().To<Service>()
    .Root<IService>("Root");
```

### ObjectResolveByTagMethodModifiers Hint

Overrides the modifiers of the `public object Resolve(Type type, object? tag)` method.

```c#
DI.Setup("Composition")
    .Hint(Hint.ObjectResolveByTagMethodModifiers, "internal")
    .Bind<IService>().To<Service>()
    .Root<IService>("Root");
```

### ObjectResolveByTagMethodName Hint

Overrides the method name for `public object Resolve(Type type, object? tag)`.

```c#
DI.Setup("Composition")
    .Hint(Hint.ObjectResolveByTagMethodName, "GetObjectByTag")
    .Bind<IService>().To<Service>()
    .Root<IService>("Root");
```

### DisposeMethodModifiers Hint

Overrides the modifiers of the `public void Dispose()` method.

```c#
DI.Setup("Composition")
    .Hint(Hint.DisposeMethodModifiers, "internal")
    .Bind<IService>().To<Service>()
    .Root<IService>("Root");
```

### DisposeAsyncMethodModifiers Hint

Overrides the modifiers of the `public ValueTask DisposeAsync()` method.

```c#
DI.Setup("Composition")
    .Hint(Hint.DisposeAsyncMethodModifiers, "internal")
    .Bind<IService>().To<Service>()
    .Root<IService>("Root");
```

### FormatCode Hint

Enables formatting of generated code. This can significantly increase compilation time and memory usage, so it is best reserved for debugging or presentation scenarios.

```c#
// FormatCode = On
DI.Setup("Composition")
    .Bind<IService>().To<Service>();
```

### SeverityOfNotImplementedContract Hint

Controls the diagnostic severity emitted when a binding declares a contract that the implementation does not actually implement. Possible values:

- _"Error"_ - this is the default value.
- _"Warning"_ - something suspicious but allowed.
- _"Info"_ - information that does not indicate a problem.
- _"Hidden"_ - not a problem.

```c#
DI.Setup("Composition")
    .Hint(Hint.SeverityOfNotImplementedContract, "Warning")
    .Bind<IService>().To<Service>();
```

### Comments Hint

Specifies whether the generated code should be commented.

```c#
// Represents the composition class
DI.Setup(nameof(Composition))
    .Bind<IService>().To<Service>()
    // Provides a composition root of my service
    .Root<IService>("MyService");
```

Appropriate comments will be added to the generated ```Composition``` class and the documentation for the class, depending on the IDE used, will look something like this:

![ReadmeDocumentation1.png](readme/ReadmeDocumentation1.png)

Then documentation for the composition root:

![ReadmeDocumentation2.png](readme/ReadmeDocumentation2.png)

### SkipDefaultConstructor Hint

Enables/disables skipping the default constructor. Default: `Off` (meaning the default constructor is used when available).

```c#
// SkipDefaultConstructor = On
DI.Setup("Composition")
    .Bind<IDependency>().To<Dependency>();
```

### SkipDefaultConstructorImplementationTypeNameRegularExpression Hint

Regular expression filter for implementation type names when skipping default constructors. Default: `.+`.

```c#
// SkipDefaultConstructor = On
// SkipDefaultConstructorImplementationTypeNameRegularExpression = .*Repository
DI.Setup("Composition")
    .Bind().To<OrderRepository>();
```

### SkipDefaultConstructorImplementationTypeNameWildcard Hint

Wildcard filter for implementation type names when skipping default constructors. Default: `*`.

```c#
// SkipDefaultConstructor = On
// SkipDefaultConstructorImplementationTypeNameWildcard = *Repository
DI.Setup("Composition")
    .Bind().To<OrderRepository>();
```

### SkipDefaultConstructorLifetimeRegularExpression Hint

Regular expression filter for lifetimes when skipping default constructors. Default: `.+`.

```c#
// SkipDefaultConstructor = On
// SkipDefaultConstructorLifetimeRegularExpression = Singleton|Scoped
DI.Setup("Composition")
    .Bind().As(Lifetime.Singleton).To<OrderRepository>();
```

### SkipDefaultConstructorLifetimeWildcard Hint

Wildcard filter for lifetimes when skipping default constructors. Default: `*`.

```c#
// SkipDefaultConstructor = On
// SkipDefaultConstructorLifetimeWildcard = *Singleton
DI.Setup("Composition")
    .Bind().As(Lifetime.Singleton).To<OrderRepository>();
```

Use these filters to restrict which implementation types or lifetimes are affected when `SkipDefaultConstructor` is enabled. You can apply them as comment directives or via the fluent API:

```c#
// SkipDefaultConstructor = On
// SkipDefaultConstructorImplementationTypeNameWildcard = *Repository
// SkipDefaultConstructorLifetimeRegularExpression = Singleton|Scoped
DI.Setup("Composition")
    .Bind().To<OrderRepository>()
    .Bind().To<CachedRepository>();
```

### DisableAutoBinding Hint

Disables automatic binding when no explicit binding exists. Default: `Off`.

```c#
// DisableAutoBinding = On
DI.Setup("Composition")
    .Bind<IDependency>().To<Dependency>();
```

### DisableAutoBindingImplementationTypeNameRegularExpression Hint

Regular expression filter for implementation type names to disable auto-binding. Default: `.+`.

```c#
// DisableAutoBinding = On
// DisableAutoBindingImplementationTypeNameRegularExpression = .*Service
DI.Setup("Composition")
    .Bind().To<OrderService>();
```

### DisableAutoBindingImplementationTypeNameWildcard Hint

Wildcard filter for implementation type names to disable auto-binding. Default: `*`.

```c#
// DisableAutoBinding = On
// DisableAutoBindingImplementationTypeNameWildcard = *Service
DI.Setup("Composition")
    .Bind().To<OrderService>();
```

### DisableAutoBindingLifetimeRegularExpression Hint

Regular expression filter for lifetimes to disable auto-binding. Default: `.+`.

```c#
// DisableAutoBinding = On
// DisableAutoBindingLifetimeRegularExpression = Singleton|Scoped
DI.Setup("Composition")
    .Bind().As(Lifetime.Singleton).To<OrderService>();
```

### DisableAutoBindingLifetimeWildcard Hint

Wildcard filter for lifetimes to disable auto-binding. Default: `*`.

```c#
// DisableAutoBinding = On
// DisableAutoBindingLifetimeWildcard = *Singleton
DI.Setup("Composition")
    .Bind().As(Lifetime.Singleton).To<OrderService>();
```

Use these filters to narrow which types or lifetimes are excluded from auto-binding when `DisableAutoBinding` is enabled:

```c#
// DisableAutoBinding = On
// DisableAutoBindingImplementationTypeNameRegularExpression = .*Service
DI.Setup("Composition")
    .Bind().To<OrderService>()
    .Bind().To<PaymentService>();
```

### SystemThreadingLock Hint

Indicates whether `System.Threading.Lock` should be used whenever possible instead of the classic approach of synchronizing object access using `System.Threading.Monitor`. `On` by default.

```c#
DI.Setup(nameof(Composition))
    .Hint(Hint.SystemThreadingLock, "Off")
    .Bind().To<Service>()
    .Root<Service>("MyService");
```

### LightweightAnonymousRoot Hint

Controls whether anonymous composition roots are generated in a lightweight manner. When _On_ (default), anonymous roots are optimized for minimal overhead. Set to _Off_ if you need full debugging capabilities for anonymous roots.

```c#
// LightweightAnonymousRoot = Off
DI.Setup("Composition")
    .Bind<IService>().To<Service>();
```

### ScopeMethodName Hint

Sets the scope factory name to be used for creating scopes.

```c#
// ScopeMethodName = CreateScope
DI.Setup("Composition")
    .Hint(Hint.ScopeMethodName, "CreateScope")
    .Bind<IDependency>().As(Lifetime.Scoped).To<Dependency>();
```

</details>

## Generated code

This section describes what Pure.DI produces: the generated composition class, its public API, and how to read, debug, and control it. Expand the topics you are interested in.

<details>
<summary>The following class will be generated</summary>

For the setup below:

```c#
DI.Setup("Composition")
    .Bind<IDependency>().To<Dependency>()
    .Bind<IService>().To<Service>()
    .Root<IService>("Root");
```

the following class will be generated:

```c#
partial class Composition
{
    // Composition root
    public IService Root
    {
        get
        {
            return new Service(new Dependency());
        }
    }
}
```

</details>

<details>
<summary>How to read generated code</summary>

Generated code is regular C# code. Read it in two passes:

1. First inspect the generated public API of the composition.
2. Then inspect implementation details only when debugging lifetimes, scopes, or performance.

The public API answers the main questions:

- Which composition class was generated from `DI.Setup(...)`.
- Which roots are available as properties or methods.
- Which constructor arguments are required by the composition.
- Whether `Resolve`/`ResolveByTag` methods, scopes, `Dispose`, or `DisposeAsync` were generated.

For example, this setup:

```c#
DI.Setup("Composition")
    .Arg<string>("connectionString")
    .RootArg<Guid>("userId")
    .Bind<IRepository>().To<Repository>()
    .Bind<IService>().To<Service>()
    .Root<IService>("CreateService");
```

produces a composition API shaped like this:

```c#
partial class Composition
{
    public Composition(string connectionString) { ... }

    public IService CreateService(Guid userId) { ... }
}
```

The implementation body shows how Pure.DI creates the graph:

- `new Implementation(...)` calls show constructor injection.
- Private fields usually represent cached singleton or scoped instances.
- Lock statements appear when thread-safe access is required.
- Local variables named for per-resolve or per-block lifetimes show reuse inside one generated root body.
- `Dispose` and `DisposeAsync` release tracked singleton and scoped disposable instances.

Use `Hint.FormatCode` only when reading or presenting generated code. It can increase compilation time and memory usage.

</details>

<details>
<summary>Generated API reference</summary>

| Setup element                          | Generated API                                                                                                                         |
|----------------------------------------|---------------------------------------------------------------------------------------------------------------------------------------|
| `DI.Setup("Composition")`              | A partial class named `Composition`, unless the setup kind prevents class generation.                                                 |
| `.Root<T>("Name")`                     | A public property named `Name`, or a method named `Name` when the root uses root arguments or generic type arguments.                 |
| `.Root<T>()`                           | An anonymous private root. It is available only through generated `Resolve`/`ResolveByTag` methods when those methods can resolve it. |
| `.Arg<T>("name")`                      | A composition constructor parameter when the argument is used by at least one root graph.                                             |
| `.RootArg<T>("name")`                  | A parameter on root methods that use this value. Roots with root arguments cannot be resolved by `Resolve`/`ResolveByTag`.            |
| `Lifetime.Transient`                   | A new instance is created at each injection site.                                                                                     |
| `Lifetime.Singleton`                   | A private cached field is generated and reused by the composition.                                                                    |
| `Lifetime.Scoped`                      | Scope-related constructors/members are generated and the instance is reused inside a scope.                                           |
| `Lifetime.PerResolve`                  | A local value is reused during one root or `Resolve` call.                                                                            |
| `Lifetime.PerBlock`                    | A local value is reused inside a generated code block.                                                                                |
| Disposable singleton/scoped dependency | `Dispose` and/or `DisposeAsync` are generated on the composition.                                                                     |
| `Hint.Resolve = Off`                   | `Resolve`/`ResolveByTag` methods and anonymous roots are not generated. Use named roots directly.                                     |
| `Hint.ToString = On`                   | `ToString()` returns a Mermaid class diagram of the composition.                                                                      |
| `Hint.Comments = Off`                  | XML documentation comments are not generated for the composition API.                                                                 |
| `Hint.FormatCode = On`                 | Generated code is formatted for easier reading.                                                                                       |

</details>

<details>
<summary>Constructors</summary>

By default, starting with version 2.3.0, no constructors are generated for a composition. The actual set of constructors depends on the composition arguments and lifetime scopes.

#### Parameterized constructor (automatic generation)

If the composition has any arguments defined, Pure.DI automatically generates a public parameterized constructor that includes all specified arguments. Setup contexts passed via `DependsOn(..., kind, name)` with `SetupContextKind.Argument` are treated as arguments and also appear in this constructor.

Example configuration:

```c#
DI.Setup("Composition")
  .Arg<string>("name")
  .Arg<int>("id")
  // ...
```

Resulting constructor:

```c#
public Composition(string name, int id) { /* ... */ }
```

Important notes:

- Only arguments that are actually used in the object graph appear in the constructor.
- Unused arguments are omitted to optimize resource usage.
- If no arguments (including setup context arguments) are specified, no parameterized constructor is created.

#### Setup context storage

When a dependent setup needs instance state from another setup, you can pass an explicit setup context via `DependsOn`:

- `SetupContextKind.Argument`: adds the setup context as a constructor argument.
- `SetupContextKind.RootArgument`: adds the setup context to root methods that require it, no constructor argument.
- `SetupContextKind.Members`: copies referenced setup members into the dependent composition, no constructor argument. The `name` parameter is optional.

  - **Public and protected members** are copied to the dependent composition.
  - **Properties without custom logic** (simple field-backed accessors) are copied without requiring partial methods.
  - **Properties with custom logic** require implementation of partial accessor methods. Referenced methods are declared partial without bodies. Properties with custom accessors use partial `get_`/`set_` methods (for example, `get__MyProperty()` for properties, note the double underscore prefix).

This is useful for Unity/MonoBehaviour scenarios where the composition must remain parameterless and the host sets fields or properties.

<details>
<summary>Example: Simple field-backed property</summary>

```c#
// BaseComposition
internal partial class BaseComposition
{
    // Simple property without custom logic
    public string ConnectionString { get; set; } = "";
}

// Composition - the property is automatically copied, no partial methods needed
internal partial class Composition
{
    // ConnectionString is automatically available here
}
```

</details>

<details>
<summary>Example: Property with custom accessor logic</summary>

```c#
// BaseComposition
internal partial class BaseComposition
{
    private int _maxConnections = 100;

    // Property with custom getter logic
    public int MaxConnections
    {
        get => _maxConnections;
        set => _maxConnections = value;
    }
}

// Composition - implements custom accessor logic
internal partial class Composition
{
    private int _maxConnections = 100;

    // Implement custom getter logic (note the double underscore prefix)
    private partial int get__MaxConnections() => _maxConnections + 1;

    public void SetMaxConnections(int value) => _maxConnections = value;
}
```

</details>

<details>
<summary>Example: Protected field access</summary>

```c#
// BaseComposition
internal partial class BaseComposition
{
    // Protected field accessible in derived compositions
    protected bool EnableDiagnostics = false;

    private void Setup()
    {
        DI.Setup(nameof(BaseComposition), CompositionKind.Internal)
            .Bind("enableDiagnostics").To(_ => EnableDiagnostics);
    }
}

// Composition - can access and modify protected fields
internal partial class Composition
{
    public Composition() => EnableDiagnostics = true; // Access protected field
}
```

</details>

#### Scope-related constructors (conditional generation)

If there is at least one binding with `Lifetime.Scoped`, Pure.DI generates two constructors:

1. Public default constructor

> Used for creating the root scope instance.
>  ```c#
> public Composition() { /* ... */ }
> ```

2. Internal constructor with parent scope

> Used for creating child scope instances. This constructor is internal and accepts a single parameter: the parent scope.
> ```c#
> internal Composition(Composition parentScope) { /* ... */ }
> ```
> Important notes:
> - The public default constructor enables initialization of the root composition.
> - The internal constructor with parent reference enables proper scoping hierarchy for `Lifetime.Scoped` dependencies.
> - These constructors are only generated when `Lifetime.Scoped` bindings exist in the composition.
> - Setup contexts with `SetupContextKind.Field` or `SetupContextKind.Property` do not require constructors and can be set by the host (for example, Unity).

#### Summary of constructor generation rules

- No arguments + no _Scoped_ lifetimes: no constructors generated.
- Arguments present (including setup context arguments): public parameterized constructor with all used arguments.
- At least one _Scoped_ lifetime: two constructors (public default + internal with parent).
- Both arguments and Scoped lifetimes: all three constructors (parameterized, public default, internal with parent).
- Setup context with `SetupContextKind.Argument` behaves like an argument and can add a constructor parameter.
- Setup context with `SetupContextKind.Field`, `SetupContextKind.Property`, `SetupContextKind.RootArgument`, or `SetupContextKind.Members` does not add constructor parameters.
</details>

<details>
<summary>Resolve and ResolveByTag methods</summary>

### Resolve and ResolveByTag methods

By default, a set of four _Resolve_/_ResolveByTag_ methods is generated:

```c#
public T Resolve<T>() { ... }

public T Resolve<T>(object? tag) { ... }

public object Resolve(Type type) { ... }

public object Resolve(Type type, object? tag) { ... }
```

These methods can resolve both public and anonymous composition roots that do not depend on root arguments. They are useful when using the [Service Locator](https://martinfowler.com/articles/injection.html) approach, where the code resolves composition roots in place:

```c#
var composition = new Composition();

composition.Resolve<IService>();
```

This is [not recommended](https://blog.ploeh.dk/2010/02/03/ServiceLocatorisanAnti-Pattern/) because _Resolve_/_ResolveByTag_ methods have a number of disadvantages:
- They provide access to an unlimited set of dependencies.
- Their use can potentially lead to runtime exceptions, for example, when the corresponding root has not been defined.
- Can be slower because they perform a lookup by type and tag.

To control the generation of these methods, see the [Resolve](#resolve-hint) hint.

</details>

<details>
<summary>Dispose and DisposeAsync</summary>

Provides a mechanism to release unmanaged resources. These methods are generated only if the composition contains at least one singleton/scoped instance that implements either [IDisposable](https://learn.microsoft.com/en-us/dotnet/api/system.idisposable) or [IAsyncDisposable](https://learn.microsoft.com/en-us/dotnet/api/system.iasyncdisposable). The `Dispose()` or `DisposeAsync()` method of the composition should be called to dispose of all created singleton/scoped objects:

```c#
using var composition = new Composition();
```

or

```c#
await using var composition = new Composition();
```

To dispose objects of other lifetimes, see [this](readme/tracking-disposable-instances-per-a-composition-root.md) or [this](readme/tracking-disposable-instances-in-delegates.md) example.

</details>

<details>
<summary>Code generation workflow</summary>

```mermaid
flowchart TD
    start@{ shape: circle, label: Start }
    setups[fa:fa-search DI setups analysis]
    types["`fa:fa-search Types analysis
    constructors/methods/properties/fields`"]
    subgraph dep[Dependency graph]
    option[fa:fa-search Selecting a next dependency set]
    creating[fa:fa-cog Creating a dependency graph variant]
    verification{fa:fa-check-circle Verification}
    end
    codeGeneration[fa:fa-code Code generation]
    compilation[fa:fa-cog Compilation]
    failed@{ shape: dbl-circ, label: Compilation failed }
    success@{ shape: dbl-circ, label: Success }

    start ==> setups
    setups -.->|Has problems| failed
    setups ==> types
    types -.-> |Has problems| failed
    types ==> option
    option ==> creating
    option -.-> |There are no other options| failed
    creating ==> verification
    verification -->|Has problems| option
    verification ==>|Correct| codeGeneration
    codeGeneration ==> compilation
    compilation -.-> |Has problems| failed
    compilation ==> success
```

</details>

<details>
<summary>Debugging generated code</summary>

Use this workflow when you need to inspect or debug the generated composition:

1. Save generated files in the project.

```xml
<PropertyGroup>
    <EmitCompilerGeneratedFiles>true</EmitCompilerGeneratedFiles>
    <CompilerGeneratedFilesOutputPath>$(BaseIntermediateOutputPath)Generated</CompilerGeneratedFilesOutputPath>
</PropertyGroup>
```

2. Enable formatting only while debugging.

```c#
DI.Setup("Composition")
    .Hint(Hint.FormatCode, "On")
    .Bind<IService>().To<Service>()
    .Root<IService>("Service");
```

3. Rebuild the project and open the generated `Composition.g.cs` file under the configured generated files folder. Without `CompilerGeneratedFilesOutputPath`, generated files are created under `obj/Debug/netX.X/generated/Pure.DI/Pure.DI/Pure.DI.SourceGenerator`.

4. Start with the generated public API:

- composition constructors;
- root properties and root methods;
- `Resolve`/`ResolveByTag` methods;
- scope factory methods;
- `Dispose`/`DisposeAsync`.

5. Then inspect the root body that creates the graph. Constructor calls show the exact dependency path, private fields show cached singleton/scoped values, and local variables show per-resolve/per-block reuse.

6. For a structural view, enable `Hint.ToString` and render the Mermaid diagram:

```c#
DI.Setup("Composition")
    .Hint(Hint.ToString, "On")
    .Bind<IService>().To<Service>()
    .Root<IService>("Service");

var diagram = new Composition().ToString();
```

7. If an anonymous root is hard to step through, disable lightweight anonymous roots for debugging:

```c#
DI.Setup("Composition")
    .Hint(Hint.LightweightAnonymousRoot, "Off")
    .Bind<IService>().To<Service>()
    .Root<IService>();
```

Turn `FormatCode`, `ToString`, and extra debugging hints off again when they are no longer needed.

</details>

## Troubleshooting

If something does not work as expected, start with the table of common symptoms, then expand the topic that matches your case.

<details>
<summary>Common first errors</summary>

| Symptom                              | Usually means                                                                 | What to do first                                                                                         |
|--------------------------------------|-------------------------------------------------------------------------------|----------------------------------------------------------------------------------------------------------|
| `Composition` type is missing        | The source generator did not run or the project does not reference `Pure.DI`. | Add the `Pure.DI` package, build the project, and restart the IDE/build server if IntelliSense is stale. |
| A dependency cannot be resolved      | An abstraction is requested, but no binding maps it to an implementation.     | Add `.Bind<IContract>().To<Implementation>()`, or request a concrete type only for small demos.          |
| The root is a method, not a property | The root uses `RootArg<T>(...)` or generic type arguments.                    | Call the generated method and pass the arguments explicitly.                                             |
| `Resolve` cannot create a root       | The root needs root arguments, but `Resolve` has nowhere to receive them.     | Use the generated root method directly and consider `.Hint(Hint.Resolve, "Off")`.                        |
| Generated files are not visible      | Roslyn generated the code in memory.                                          | Enable `EmitCompilerGeneratedFiles` and set `CompilerGeneratedFilesOutputPath`.                          |
| A lock appears in generated code     | Pure.DI protects cached instances for thread-safe access.                     | Keep it unless composition access is known to be single-threaded; then use `Hint.ThreadSafe`.            |
| `Dispose` or `DisposeAsync` appeared | The composition owns singleton or scoped disposable instances.                | Dispose the composition with `using` or `await using`.                                                   |

</details>

<details>
<summary>Generated code troubleshooting</summary>

### The root was generated as a method, not a property

A root becomes a method when it needs runtime data, such as `RootArg<T>(...)`, or when the root itself is generic. Call it directly and pass the required arguments:

```c#
var service = composition.CreateService(userId);
```

### Resolve methods were not generated

Check `Hint.Resolve`. When it is `Off`, `Resolve`/`ResolveByTag` methods are intentionally omitted and anonymous roots are not generated. Use named roots instead:

```c#
var service = composition.Service;
```

### Resolve cannot create a root with root arguments

`Resolve`/`ResolveByTag` methods do not have a place to pass root arguments. Use the generated root method directly, or disable `Resolve` with `Hint.Resolve = Off` to avoid warnings and keep the generated API explicit.

### A lock appears in generated code

Pure.DI generates synchronization for thread-safe access to cached instances when needed. To remove it only when composition access is known to be single-threaded, use:

```c#
DI.Setup("Composition")
    .Hint(Hint.ThreadSafe, "Off");
```

### Dispose or DisposeAsync was generated

The composition tracks singleton and scoped instances that implement `IDisposable` or `IAsyncDisposable`. Dispose the composition when it owns such instances:

```c#
using var composition = new Composition();
```

or:

```c#
await using var composition = new Composition();
```

</details>

<details>
<summary>Version update</summary>

When updating the version, it is possible that the previous version of the code generator remains active and is used by compilation services. In this case, the old and new versions of the generator may conflict. For a project where the code generator is used, it is recommended to do the following:
- After updating the version, close the IDE if it is open
- Delete the _obj_ and _bin_ directories
- Run the following commands one by one

```shell
dotnet build-server shutdown
```

```shell
dotnet restore
```

```shell
dotnet build
```

</details>

<details>
<summary>Disabling API generation</summary>

Pure.DI automatically generates its API. If an assembly already has the Pure.DI API, for example, from another assembly, it is sometimes necessary to disable its automatic generation to avoid ambiguity. To do this, you need to add a _DefineConstants_ element to the project files of these modules. For example:

```xml
<PropertyGroup>
    <DefineConstants>$(DefineConstants);PUREDI_API_SUPPRESSION</DefineConstants>
</PropertyGroup>
```

</details>

<details>
<summary>Performance profiling</summary>

Please install the [JetBrains.dotTrace.GlobalTools](https://www.nuget.org/packages/JetBrains.dotTrace.GlobalTools) dotnet tool globally, for example:

```shell
dotnet tool install --global JetBrains.dotTrace.GlobalTools --version 2024.3.3
```

Or make sure it is installed. Add the following sections to the project:

```xml
<Project Sdk="Microsoft.NET.Sdk">

  <PropertyGroup>
    <PureDIProfilePath>c:\profiling</PureDIProfilePath>
  </PropertyGroup>

  <ItemGroup>
    <CompilerVisibleProperty Include="PureDIProfilePath" />
  </ItemGroup>

</Project>
```

Replace a path like *c:\profiling* with the path where the profiling results will be saved.

Start a build and wait until a file like *c:\profiling\pure_di_????.dtt* appears in the directory.

</details>

## Additional resources

Examples of how to set up a composition
- [Pure.DI](https://github.com/DevTeam/Pure.DI/blob/master/src/Pure.DI.Core/Generator.cs)
- [C# interactive](https://github.com/DevTeam/csharp-interactive/blob/master/CSharpInteractive/Composition.cs)
- [Immutype](https://github.com/DevTeam/Immutype/blob/master/Immutype/Composition.cs)
- [MSBuild logger](https://github.com/JetBrains/teamcity-msbuild-logger/blob/master/TeamCity.MSBuild.Logger/Composition.cs)

Articles
- [An introductory article that will help you understand the basic idea and get started with Pure.DI](/readme/en_art_basics/en_basics.md)

<details>
<summary>Additional resources in Russian</summary>

Articles
- [An introductory article that will help you understand the basic idea and get started with Pure.DI](/readme/ru_art_basics/ru_basics.md)
- [Pure.DI: new features](https://habr.com/ru/articles/1010646/)
- [New in Pure.DI by the end of 2024](https://habr.com/ru/articles/868744/)
- [New in Pure.DI](https://habr.com/ru/articles/808297/)
- [Pure.DI v2.1](https://habr.com/ru/articles/795809/)
- [Pure.DI next step](https://habr.com/ru/articles/554236/)
- [Pure.DI for .NET](https://habr.com/ru/articles/552858/)

DotNext video

<a href="http://www.youtube.com/watch?feature=player_embedded&v=nrp9SH-gLqg" target="_blank"><img src="http://img.youtube.com/vi/nrp9SH-gLqg/0.jpg"
alt="DotNext Pure.DI" width="640" border="10"/></a>

</details>
