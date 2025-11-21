/*
$v=true
$p=6
$d=Weak Reference
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
        // This hint indicates to not generate methods such as Resolve
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