// ReSharper disable UnusedMember.Global
// ReSharper disable HeapView.ObjectAllocation.Evident
// ReSharper disable HeapView.ObjectAllocation
// ReSharper disable HeapView.ClosureAllocation
// ReSharper disable HeapView.DelegateAllocation

namespace Pure.DI.Core;

using System.Runtime.CompilerServices;

static class LoggerExtensions
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void CompileError(this ILogger logger, string errorMessage, in Location location, string id) =>
        logger.Log(new LogEntry(DiagnosticSeverity.Error, errorMessage, location, id));

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void CompileWarning(this ILogger logger, string waringMessage, in Location location, string id) =>
        logger.Log(new LogEntry(DiagnosticSeverity.Warning, waringMessage, location, id));

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void CompileInfo(this ILogger logger, string message, in Location location, string id) =>
        logger.Log(new LogEntry(DiagnosticSeverity.Info, message, location, id));
}