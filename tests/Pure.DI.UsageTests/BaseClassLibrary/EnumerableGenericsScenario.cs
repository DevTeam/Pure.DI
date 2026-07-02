/*
$v=true
$p=4
$d=Enumerable generics
$sa=Enumerable
$h=Shows how generic middleware pipelines collect all matching implementations.
$f=>[!NOTE]
$f=>Generic enumerable injections are useful for implementing middleware patterns where multiple handlers need to be invoked in sequence.
$r=Shouldly
*/

// ReSharper disable ClassNeverInstantiated.Local
// ReSharper disable CheckNamespace
// ReSharper disable ArrangeTypeModifiers
// ReSharper disable ClassNeverInstantiated.Global
// ReSharper disable UnusedTypeParameter

namespace Pure.DI.UsageTests.BCL.EnumerableGenericsScenario;

using System.Collections.Immutable;
using Shouldly;
using Xunit;

// {
//# using Pure.DI;
//# using System.Collections.Immutable;
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
            // Register generic middleware components.
            // LoggingMiddleware<T> is registered as the default implementation.
            .Bind<IMiddleware<TT>>().To<LoggingMiddleware<TT>>()
            // MetricsMiddleware<T> is registered with the "Metrics" tag.
            .Bind<IMiddleware<TT>>("Metrics").To<MetricsMiddleware<TT>>()
            
            // Register the pipeline that takes the collection of all middleware.
            .Bind<IPipeline<TT>>().To<Pipeline<TT>>()

            // Composition roots for different data types (int and string)
            .Root<IPipeline<int>>("IntPipeline")
            .Root<IPipeline<string>>("StringPipeline");

        var composition = new Composition();

        // Validate the pipeline for int
        var intPipeline = composition.IntPipeline;
        intPipeline.Middlewares.Length.ShouldBe(2);
        intPipeline.Middlewares[0].ShouldBeOfType<LoggingMiddleware<int>>();
        intPipeline.Middlewares[1].ShouldBeOfType<MetricsMiddleware<int>>();

        // Validate the pipeline for string
        var stringPipeline = composition.StringPipeline;
        stringPipeline.Middlewares.Length.ShouldBe(2);
        stringPipeline.Middlewares[0].ShouldBeOfType<LoggingMiddleware<string>>();
        stringPipeline.Middlewares[1].ShouldBeOfType<MetricsMiddleware<string>>();
// }
        composition.SaveClassDiagram();
    }
}

// {
// Middleware interface
interface IMiddleware<T>;

// Logging implementation
class LoggingMiddleware<T> : IMiddleware<T>;

// Metrics implementation
class MetricsMiddleware<T> : IMiddleware<T>;

// Pipeline interface
interface IPipeline<T>
{
    ImmutableArray<IMiddleware<T>> Middlewares { get; }
}

// Pipeline implementation that aggregates all available middleware
class Pipeline<T>(IEnumerable<IMiddleware<T>> middlewares) : IPipeline<T>
{
    public ImmutableArray<IMiddleware<T>> Middlewares { get; }
        = [..middlewares];
}
// }