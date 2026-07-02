/*
$v=true
$p=13
$d=Tag Any
$sa=Tags
$h=`Tag.Any` creates a binding that matches any tag value, including default (null), allowing flexible injection scenarios where the tag value can be used within factory contexts. This is useful when you need to dynamically handle different tag values in a single binding.
$f=>[!IMPORTANT]
$f=>`Tag.Any` provides maximum flexibility but requires careful handling within factories to properly interpret and use the tag value.
$r=Shouldly
*/

// ReSharper disable ClassNeverInstantiated.Local
// ReSharper disable CheckNamespace
// ReSharper disable UnusedType.Global
// ReSharper disable ArrangeTypeModifiers
// ReSharper disable ClassNeverInstantiated.Global

// ReSharper disable PreferConcreteValueOverDefault
namespace Pure.DI.UsageTests.Advanced.TagAnyScenario;

using Shouldly;
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
            // Binds IQueue to the Queue implementation.
            // Tag.Any creates a binding that matches any tag (including null),
            // allowing the specific tag value to be used within the factory (ctx.Tag).
            .Bind<IQueue>(Tag.Any).To(ctx => new Queue(ctx.Tag))
            .Bind<IQueueService>().To<QueueService>()

            // Composition root
            .Root<IQueueService>("QueueService")

            // Root by Tag.Any: Resolves IQueue with the tag "Audit"
            .Root<IQueue>("AuditQueue", "Audit");

        var composition = new Composition();
        var queueService = composition.QueueService;

        queueService.WorkItemsQueue.Id.ShouldBe("WorkItems");
        queueService.PartitionQueue.Id.ShouldBe(42);
        queueService.DefaultQueue.Id.ShouldBeNull();
        composition.AuditQueue.Id.ShouldBe("Audit");
        // }
        composition.SaveClassDiagram();
    }
}

// {
interface IQueue
{
    object? Id { get; }
}

record Queue(object? Id) : IQueue;

interface IQueueService
{
    IQueue WorkItemsQueue { get; }

    IQueue PartitionQueue { get; }

    IQueue DefaultQueue { get; }
}

class QueueService(
    // Injects IQueue tagged with "WorkItems"
    [Tag("WorkItems")] IQueue workItemsQueue,
    // Injects IQueue tagged with integer 42
    [Tag(42)] Func<IQueue> partitionQueueFactory,
    // Injects IQueue with the default (null) tag
    IQueue defaultQueue)
    : IQueueService
{
    public IQueue WorkItemsQueue { get; } = workItemsQueue;

    public IQueue PartitionQueue { get; } = partitionQueueFactory();

    public IQueue DefaultQueue { get; } = defaultQueue;
}
// }