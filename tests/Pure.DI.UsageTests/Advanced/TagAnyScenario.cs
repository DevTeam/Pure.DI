/*
$v=true
$p=3
$d=Tag Any
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
        // This hint indicates to not generate methods such as Resolve
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