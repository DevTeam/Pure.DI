/*
$v=true
$p=6
$d=Weak Reference
$h=Injecting `WeakReference<T>` lets a service hold a dependency without keeping it alive — useful for large, recreatable objects such as caches. Bind the underlying type as usual and request `WeakReference<T>`; the consumer then calls `TryGetTarget`, which returns `false` once the object has been garbage-collected.
$f=>[!NOTE]
$f=>`WeakReference<T>` is useful for caching scenarios where you want to allow garbage collection when memory is constrained.
$r=Shouldly
*/

// ReSharper disable ClassNeverInstantiated.Local
// ReSharper disable CheckNamespace
// ReSharper disable ArrangeTypeModifiers
// ReSharper disable UnusedVariable
// ReSharper disable UnusedMember.Global

namespace Pure.DI.UsageTests.BCL.WeakReferenceScenario;

using Xunit;

// {
//# using Pure.DI;
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
            .Bind<ILargeCache>().To<LargeCache>()
            .Bind<IService>().To<Service>()

            // Composition root
            .Root<IService>("MyService");

        var composition = new Composition();
        var service = composition.MyService;
        // }
        composition.SaveClassDiagram();
    }
}

// {
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
// }