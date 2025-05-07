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
    public static void CompileError(this ILogger logger, string errorMessage, in ImmutableArray<Location> locations, string id) =>
        logger.Log(new LogEntry(DiagnosticSeverity.Error, errorMessage, Sort(locations), id));

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void CompileWarning(this ILogger logger, string waringMessage, in ImmutableArray<Location> locations, string id) =>
        logger.Log(new LogEntry(DiagnosticSeverity.Warning, waringMessage, Sort(locations), id));

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void CompileInfo(this ILogger logger, string message, in ImmutableArray<Location> locations, string id) =>
        logger.Log(new LogEntry(DiagnosticSeverity.Info, message, Sort(locations), id));

    private static ImmutableArray<Location> Sort(in ImmutableArray<Location> locations) =>
        locations.IsDefaultOrEmpty || locations.Length == 1
            ? locations
            : locations.OrderBy(i => !i.IsInSource).ToImmutableArray();
}