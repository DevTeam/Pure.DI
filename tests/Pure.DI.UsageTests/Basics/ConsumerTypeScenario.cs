/*
$v=true
$p=32
$d=Consumer type
$sa=Consumer types
$sa=Generics
$h=`ConsumerType` is used to get the consumer type of the given dependency. The use of `ConsumerType` is demonstrated on the example of [Serilog library](https://serilog.net/):
$h=Idiomatic consumers use it for context-aware logging, metrics and auditing, where every component reports under its own name instead of one global category.
$f=>[!NOTE]
$f=>ConsumerType is useful for creating context-aware loggers or when you need to know which type is consuming a dependency.
$f=
$f=It is available only inside a factory binding (`ctx.ConsumerType`), where it is known statically, so the value costs nothing at runtime.
$f=The same trick applies to `ActivitySource`, `Meter` and `ILogger<T>`-style abstractions: one binding produces a per-consumer instance.
$f=Each call produces a distinct wrapper, so bind this factory as transient; wrapping a singleton logger factory is free, while wrapping an expensive object per consumer is not.
$f=
$f=When the consumer type is also a type argument of the dependency, no runtime value is needed: bind the marker `TT` and the generated code passes the concrete type to a generic call, as in `.Bind<ILogger<TT>>().To(ctx => { ctx.Inject<ILoggerFactory>(out var factory); return factory.CreateLogger<TT>(); })`. For a consumer `Service` this becomes `factory.CreateLogger<Service>()`, so `ILogger<Service>` is created at compile time without reflection.
$f=Prefer the marker when the consumer type appears in the resolved abstraction (`ILogger<T>`, `IHandler<T>`, `IRepository<T>`); prefer `ctx.ConsumerType` when it is only passed as a value, such as the `Type` accepted by `Serilog.ILogger.ForContext`.
$f=There is no separate `TConsumer` marker and none is needed: `TT` is substituted by the generator at the call site, while `ctx.ConsumerType` gives the same type as a runtime `Type` value.
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
        // Disable Resolve methods to keep the public API minimal
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