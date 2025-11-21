/*
$v=true
$p=1
$d=Enumerable generics
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
        // This hint indicates to not generate methods such as Resolve
        // Resolve = Off
// {
        DI.Setup(nameof(Composition))
            // Регистрируем обобщенные компоненты middleware.
            // LoggingMiddleware<T> регистрируется как стандартная реализация.
            .Bind<IMiddleware<TT>>().To<LoggingMiddleware<TT>>()
            // MetricsMiddleware<T> регистрируется с тегом "Metrics".
            .Bind<IMiddleware<TT>>("Metrics").To<MetricsMiddleware<TT>>()
            
            // Регистрируем сам конвейер, который будет принимать коллекцию всех middleware.
            .Bind<IPipeline<TT>>().To<Pipeline<TT>>()

            // Корни композиции для разных типов данных (int и string)
            .Root<IPipeline<int>>("IntPipeline")
            .Root<IPipeline<string>>("StringPipeline");

        var composition = new Composition();

        // Проверяем конвейер для обработки int
        var intPipeline = composition.IntPipeline;
        intPipeline.Middlewares.Length.ShouldBe(2);
        intPipeline.Middlewares[0].ShouldBeOfType<LoggingMiddleware<int>>();
        intPipeline.Middlewares[1].ShouldBeOfType<MetricsMiddleware<int>>();

        // Проверяем конвейер для обработки string
        var stringPipeline = composition.StringPipeline;
        stringPipeline.Middlewares.Length.ShouldBe(2);
        stringPipeline.Middlewares[0].ShouldBeOfType<LoggingMiddleware<string>>();
        stringPipeline.Middlewares[1].ShouldBeOfType<MetricsMiddleware<string>>();
// }
        composition.SaveClassDiagram();
    }
}

// {
// Интерфейс для промежуточного ПО (middleware)
interface IMiddleware<T>;

// Реализация для логирования
class LoggingMiddleware<T> : IMiddleware<T>;

// Реализация для сбора метрик
class MetricsMiddleware<T> : IMiddleware<T>;

// Интерфейс конвейера обработки
interface IPipeline<T>
{
    ImmutableArray<IMiddleware<T>> Middlewares { get; }
}

// Реализация конвейера, собирающая все доступные middleware
class Pipeline<T>(IEnumerable<IMiddleware<T>> middlewares) : IPipeline<T>
{
    public ImmutableArray<IMiddleware<T>> Middlewares { get; }
        = [..middlewares];
}
// }