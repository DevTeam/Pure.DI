/*
$v=true
$p=19
$d=Consumer type
$h=`ConsumerType` is used to get the consumer type of the given dependency. The use of `ConsumerType` is demonstrated on the example of [Serilog library](https://serilog.net/):
$r=Shouldly;Serilog.Core;Serilog.Events
*/

// ReSharper disable ClassNeverInstantiated.Local
// ReSharper disable CheckNamespace
// ReSharper disable UnusedType.Global
// ReSharper disable ArrangeTypeModifiers
// ReSharper disable ClassNeverInstantiated.Global
// ReSharper disable UnusedVariable

// ReSharper disable UnusedTypeParameter
// ReSharper disable UnusedMember.Local
// ReSharper disable UnusedMemberInSuper.Global
#pragma warning disable CS9113 // Parameter is unread.
namespace Pure.DI.UsageTests.Basics.ConsumerTypeScenario;

#pragma warning disable CA2263
using Serilog.Core;
using Serilog.Events;
using Xunit;

// {
//# using Pure.DI;
//# using Serilog.Core;
// }

class EventSink(ICollection<LogEvent> events)
    : ILogEventSink
{
    public void Emit(LogEvent logEvent) =>
        events.Add(logEvent);
}

public class Scenario
{
    [Fact]
    public void Run()
    {
        var events = new List<LogEvent>();
        // Create a Serilog logger that writes to our list to verify the output
        var serilogLogger = new Serilog.LoggerConfiguration()
            .WriteTo.Sink(new EventSink(events))
            .CreateLogger();
// {
        //# Serilog.ILogger serilogLogger = new Serilog.LoggerConfiguration().CreateLogger();
        var composition = new Composition(logger: serilogLogger);
        var service = composition.Root;
// }
        // Verify that we captured 2 log events
        events.Count.ShouldBe(2);
        foreach (var @event in events)
        {
            // Check if the log event contains the 'SourceContext' property.
            // This property is added by Serilog's .ForContext() method,
            // which we use in the DI binding below.
            @event.Properties.ContainsKey("SourceContext").ShouldBeTrue();
        }

        composition.SaveClassDiagram();
    }
}

// {
interface IDependency;

class Dependency : IDependency
{
    public Dependency(Serilog.ILogger log)
    {
        log.Information("Dependency created");
    }
}

interface IService
{
    IDependency Dependency { get; }
}

class Service : IService
{
    public Service(
        Serilog.ILogger log,
        IDependency dependency)
    {
        Dependency = dependency;
        log.Information("Service initialized");
    }

    public IDependency Dependency { get; }
}

partial class Composition
{
    private void Setup() =>
// }
        // This hint indicates to not generate methods such as Resolve
        // Resolve = Off
// {
        DI.Setup(nameof(Composition))
            .Arg<Serilog.ILogger>("logger", "from arg")
            .Bind().To(ctx => {
                ctx.Inject<Serilog.ILogger>("from arg", out var logger);

                // Enriches the logger with the specific context of the consumer type.
                // ctx.ConsumerType represents the type into which the dependency is being injected.
                // This allows logs to be tagged with the name of the class that created them.
                return logger.ForContext(ctx.ConsumerType);
            })
            .Bind().To<Dependency>()
            .Bind().To<Service>()
            .Root<IService>(nameof(Root));
}
// }