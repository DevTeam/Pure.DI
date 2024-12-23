/*
$v=true
$p=19
$d=Consumer types
$h=`ConsumerTypes` is used to get the list of consumer types of a given dependency. It contains an array of types and guarantees that it will contain at least one element. The use of `ConsumerTypes` is demonstrated on the example of [Serilog library](https://serilog.net/):
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
namespace Pure.DI.UsageTests.Basics.ConsumerTypesScenario;

#pragma warning disable CA2263
using Serilog.Core;
using Serilog.Events;
using Xunit;

// {
//# using Pure.DI;
//# using Shouldly;
//# using Serilog.Core;
//# using Serilog.Events;
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
        var serilogLogger = new Serilog.LoggerConfiguration()
            .WriteTo.Sink(new EventSink(events))
            .CreateLogger();
// {
        //# Serilog.ILogger serilogLogger = new Serilog.LoggerConfiguration().CreateLogger();
        var composition = new Composition(logger: serilogLogger);
        var service = composition.Root;
// }
        events.Count.ShouldBe(2);
        foreach (var @event in events)
        {
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
        log.Information("created");
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
        log.Information("created");
    }

    public IDependency Dependency { get; }
}

partial class Composition
{
    private void Setup() =>
        DI.Setup(nameof(Composition))
// }
            .Hint(Hint.Resolve, "Off")
// {
            .Arg<Serilog.ILogger>("logger", "from arg")
            .Bind().To(ctx =>
            {
                ctx.Inject<Serilog.ILogger>("from arg", out var logger);
                return logger.ForContext(ctx.ConsumerTypes[0]);
            })

            .Bind().To<Dependency>()
            .Bind().To<Service>()
            .Root<IService>(nameof(Root));
}
// }