/*
$v=true
$t=QuickStart
$t=Lifetimes
$p=0
$p=QuickStart:4
$d=Transient
$sa=Singleton
$sa=PerResolve
$sa=PerBlock
$h=The `Transient` lifetime specifies to create a new dependency instance each time. It is the default lifetime and can be omitted. A transient service may still capture singleton dependencies: what is recreated is the service itself, not everything it uses.
$f=The `Transient` lifetime is the safest and is used by default. Yes, its widespread use can cause a lot of memory traffic, but if there are doubts about thread safety, the `Transient` lifetime is preferable because each consumer has its own instance of the dependency. The following nuances should be considered when choosing the `Transient` lifetime:
$f=
$f=- There will be unnecessary memory overhead that could be avoided.
$f=
$f=- Every object created must be disposed of, and this will waste CPU resources, at least when the GC does its memory-clearing job.
$f=
$f=- Poorly designed constructors can run slowly, perform functions that are not their own, and greatly hinder the efficient creation of compositions of multiple objects.
$f=
$f=>[!IMPORTANT]
$f=>The following very important rule, in my opinion, will help in the last point. Now, when a constructor is used to implement dependencies, it should not be loaded with other tasks. Accordingly, constructors should be free of all logic except for checking arguments and saving them for later use. Following this rule, even the largest compositions of objects will be built quickly.
$f=
$f=This is the lifetime a real project starts with. A service that holds its dependencies and no per-instance state is cheap to create and cannot be broken by sharing, so adding `Singleton` or `Scoped` later is a deliberate decision about identity, not a performance tweak.
$f=>[!WARNING]
$f=>A transient captured by a singleton stops being transient: the singleton keeps that single instance for its whole life. The constructors of singletons, not the `.As(...)` calls, decide the real lifetimes in a graph.
$f=
$f=The composition does not own transient instances, so an `IDisposable` part of the graph belongs to the caller of the root. When the composition should release it, declare the root as `Owned<T>` - see [Tracking disposable instances per a composition root](tracking-disposable-instances-per-a-composition-root.md).
$r=Shouldly
*/

// ReSharper disable ClassNeverInstantiated.Local
// ReSharper disable CheckNamespace
// ReSharper disable ArrangeTypeModifiers

namespace Pure.DI.UsageTests.Lifetimes.TransientScenario;

using Xunit;
using static Lifetime;

// {
//# using Pure.DI;
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
        // }
        composition.SaveClassDiagram();
    }
}

// {
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
// }
