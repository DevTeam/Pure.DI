/*
$v=true
$p=300
$d=DI tracing via serilog
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
// ReSharper disable UnusedMember.Global
#pragma warning disable CS9113 // Parameter is unread.
#pragma warning disable CA1859
namespace Pure.DI.UsageTests.Advanced.DITracingViaSerilogScenario;

#pragma warning disable CA2263
using System.Runtime.CompilerServices;
using Serilog.Core;
using Serilog.Events;
using Xunit;

// {
interface IDependency;

class Dependency : IDependency;

interface IService
{
    IDependency Dependency { get; }
}

class Service : IService
{
    public Service(Serilog.ILogger log, IDependency dependency)
    {
        Dependency = dependency;
        log.Information("Created");
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
            .Hint(Hint.OnNewInstance, "On")
            .Hint(Hint.OnDependencyInjection, "On")
            // Excluding loggers
            .Hint(Hint.OnNewInstanceImplementationTypeNameRegularExpression, "^((?!Logger).)*$")
            .Hint(Hint.OnDependencyInjectionContractTypeNameRegularExpression, "^((?!Logger).)*$")

            .Arg<Serilog.ILogger>("logger", "from arg")
            .Bind<Serilog.ILogger>().To(ctx =>
            {
                ctx.Inject("from arg", out Serilog.ILogger logger);
                return logger.ForContext(ctx.ConsumerTypes[0]);
            })

            .Bind().To<Dependency>()
            .Bind().To<Service>()
            .Root<Serilog.ILogger>(nameof(Log), kind: RootKinds.Private)
            .Root<IService>(nameof(Root));
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    partial void OnNewInstance<T>(ref T value, object? tag, Lifetime lifetime) => 
        Log.Information("Created [{Value}], tag [{Tag}] as {Lifetime}", value, tag, lifetime);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private partial T OnDependencyInjection<T>(in T value, object? tag, Lifetime lifetime)
    {
        Log.Information("Injected [{Value}], tag [{Tag}] as {Lifetime}", value, tag, lifetime);
        return value;
    }
}
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
        //# Serilog.ILogger serilogLogger = CreateLogger();
        var composition = new Composition(logger: serilogLogger);
        var service = composition.Root;
// }
        events.Count.ShouldBe(5);
        foreach (var e in events)
        {
            e.Properties.ContainsKey("SourceContext").ShouldBeTrue();
        }

        composition.SaveClassDiagram();
    }
}