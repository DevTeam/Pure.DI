# Bindings and roots

Pure.DI is a compile-time source generator. A setup describes the object graph; a root exposes an entry point as a generated member.

```csharp
using Pure.DI;

DI.Setup(nameof(Composition))
    .Bind<IClock>().To<SystemClock>()
    .Bind<IService>().To<Service>()
    .Root<IService>("Service");

var service = new Composition().Service;

interface IClock;
sealed class SystemClock : IClock;
interface IService;
sealed class Service(IClock clock) : IService
{
    public IClock Clock { get; } = clock;
}
```

Use `Bind<TContract>().To<TImplementation>()` for an abstraction. Concrete types can be auto-bound, but explicit abstraction bindings make implementation choices clear. `Bind().To<T>()` can infer the implementation and directly implemented abstractions; it excludes special framework types and inherited interfaces.

Injecting `T[]`, `IEnumerable<T>` or another collection of `T` gives one element per distinct tag of `T`; bindings with the same tag, or without a tag, override each other, so use `Tag.Unique` for implementations that must all be included. A binding is never an element of its own collection, so a composite that implements `T` and injects `IEnumerable<T>` gets the other bindings of `T`.

Use tags when one contract has multiple implementations. A consumer can request a tagged dependency with `[Tag("name")]`, and a root can specify the tag. Use `Arg<T>("name")` for values supplied to the composition constructor and `RootArg<T>("name")` for values supplied when calling a root. Factory bindings use `To<T>(ctx => ...)` or the simplified lambda form when construction requires setup work.

Prefer a small set of named roots. `Resolve<T>()` accesses declared roots; it is not a replacement for missing bindings or an unrestricted runtime container.

For complete scenarios, use the documentation matching the project's package version: https://github.com/DevTeam/Pure.DI/tree/master/readme . Check the installed API with a build when docs and package versions differ.
