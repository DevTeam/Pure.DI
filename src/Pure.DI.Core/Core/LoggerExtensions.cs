// ReSharper disable UnusedMember.Global
// ReSharper disable HeapView.ObjectAllocation.Evident
// ReSharper disable HeapView.ObjectAllocation
// ReSharper disable HeapView.ClosureAllocation
// ReSharper disable HeapView.DelegateAllocation
namespace Pure.DI.Core;

using System.Diagnostics;

internal static class LoggerExtensions
{
    [ThreadStatic] private static int _processLevel;
    
    [Conditional("DEBUG")]
    public static void Trace<T, TState>(this ILogger<T> logger, in TState state, Func<TState, IEnumerable<string>> messageFactory, in Location? location = default)
    {
        if (!IsTracing(logger))
        {
            return;
        }
        
        logger.Log(new LogEntry(DiagnosticSeverity.Hidden, messageFactory(state), location));
    }

    public static void CompileError<T>(this ILogger<T> logger, string errorMessage, in Location location, string id) =>
        logger.Log(new LogEntry(DiagnosticSeverity.Error, new []{ errorMessage }, location, id));

    public static void CompileWarning<T>(this ILogger<T> logger, string waringMessage, in Location location, string id) =>
        logger.Log(new LogEntry(DiagnosticSeverity.Warning, new []{ waringMessage }, location, id));
    
    public static void CompileInfo<T>(this ILogger<T> logger, string message, in Location location, string id) =>
        logger.Log(new LogEntry(DiagnosticSeverity.Info, new []{ message }, location, id));

    public static IDisposable TraceProcess<T>(this ILogger<T> logger, string shortDescription, in Location? location = default)
    {
        if (!IsTracing(logger))
        {
            return Disposables.Empty;
        }

        _processLevel++;
        var prefix = new string(' ', _processLevel * 2);
        var logEntry = new LogEntry(
            DiagnosticSeverity.Hidden,
            new []{ $">        {prefix}{shortDescription} started" },
            location);

        logger.Log(logEntry);
        var stopwatch = new Stopwatch();
        stopwatch.Start();
        return Disposables.Create(() =>
        {
            stopwatch.Stop();
            logger.Log(logEntry with
            {
                Lines = new []{ $"{stopwatch.Elapsed.TotalMilliseconds,8:#####0.0} {prefix}{shortDescription} finished" },
                IsOutcome = true
            });
            
            _processLevel--;
        });
    }

    public static bool IsTracing<T>(this ILogger<T> logger)
    {
#if DEBUG
        return true;
#else
        return false;
#endif
    }
}