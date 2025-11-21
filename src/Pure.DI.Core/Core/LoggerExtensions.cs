// ReSharper disable UnusedMember.Global
// ReSharper disable HeapView.ObjectAllocation.Evident
// ReSharper disable HeapView.ObjectAllocation
// ReSharper disable HeapView.ClosureAllocation
// ReSharper disable HeapView.DelegateAllocation

namespace Pure.DI.Core;

using System.Runtime.CompilerServices;

static class LoggerExtensions
{
    extension (ILogger logger)
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void CompileError(string errorMessage, in ImmutableArray<Location> locations, string id) =>
            logger.Log(new LogEntry(DiagnosticSeverity.Error, errorMessage, Sort(locations), id));

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void CompileWarning(string waringMessage, in ImmutableArray<Location> locations, string id) =>
            logger.Log(new LogEntry(DiagnosticSeverity.Warning, waringMessage, Sort(locations), id));

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void CompileInfo(string message, in ImmutableArray<Location> locations, string id) =>
            logger.Log(new LogEntry(DiagnosticSeverity.Info, message, Sort(locations), id));
    }

    private static ImmutableArray<Location> Sort(in ImmutableArray<Location> locations) =>
        locations.OrderBy(GetPriority).ToImmutableArray();

    private static int GetPriority(Location location)
    {
        // ReSharper disable once InvertIf
        if (location.IsInSource)
        {
            // ReSharper disable once ConvertIfStatementToReturnStatement
            if (!location.SourceTree.FilePath.EndsWith(Names.CodeFileSuffix, StringComparison.InvariantCultureIgnoreCase))
            {
                return 0;
            }

            return 1;
        }

        return 2;
    }
}