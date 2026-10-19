/*
$v=true
$t=QuickStart
$t=Lifetimes
$p=1
$p=QuickStart:5
$d=Singleton
$sa=Transient
$sa=Disposable singleton
$sa=Default lifetime
$h=The `Singleton` lifetime ensures that there will be a single instance of the dependency for each composition. Use it when consumers must share identity or state: a cache, a connection pool, a dispatcher, a run registry, or an accounting store. A stateless service does not need it, because a transient instance can capture the same singleton dependencies without becoming shared itself.
$f=Some articles advise using objects with a `Singleton` lifetime as often as possible, but the following details must be considered:
$f=
$f=- For .NET the default behavior is to create a new instance of the type each time it is needed, other behavior requires, additional logic that is not free and requires additional resources.
$f=
$f=- The use of `Singleton` adds a requirement for thread-safety controls on their use, since singletons are more likely to share their state between different threads without even realizing it.
$f=
$f=- The thread-safety control should be automatically extended to all dependencies that _Singleton_ uses, since their state is also now shared.
$f=
$f=- Logic for thread-safety control can be resource-costly, error-prone, interlocking, and difficult to test.
$f=
$f=- _Singleton_ can retain dependency references longer than their expected lifetime, this is especially significant for objects that hold "non-renewable" resources, such as the operating system Handler.
$f=
$f=- Sometimes additional logic is required to dispose of _Singleton_.
$f=
$f=In a real project this is the lifetime of infrastructure that outlives a request: a client over a connection pool, an in-memory cache of parsed data, or a dispatcher every consumer must reach. The instance belongs to the composition, so it lives exactly as long as that composition object, and it is the composition that disposes it (see [Disposable singleton](disposable-singleton.md)), not whichever consumer receives it first.
$f=>[!WARNING]
$f=>A singleton keeps every dependency it captured for its own lifetime. If a singleton receives a transient, scoped or per-resolve dependency, that dependency silently becomes shared as well and is never recreated per request. Check each constructor parameter of a type before giving it a `Singleton` lifetime.
$f=
$f=Sharing also moves the thread-safety burden onto this instance and onto everything it uses. Keep the `ThreadSafe` hint on for a composition whose roots or deferred factories are reached from several threads, and synchronize the singleton's own mutable state. When neither identity nor shared state is required, prefer [Transient](transient.md).
$r=Shouldly
*/

// ReSharper disable ClassNeverInstantiated.Local
// ReSharper disable CheckNamespace
// ReSharper disable ArrangeTypeModifiers

namespace Pure.DI.UsageTests.Lifetimes.SingletonScenario;

using System.Diagnostics.CodeAnalysis;
using Xunit;
using static Lifetime;

// {
//# using Pure.DI;
//# using System.Diagnostics.CodeAnalysis;
//# using static Pure.DI.Lifetime;
// }

public class Scenario
{
    [Fact]
    public void Run()
    {
        // Disable Resolve methods to keep the public API minimal
        // Resolve = Off
// {
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
// }
        composition.SaveClassDiagram();
    }
}

// {
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
// }
