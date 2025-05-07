// ReSharper disable HeapView.BoxingAllocation
// ReSharper disable NotAccessedPositionalProperty.Global
namespace Pure.DI.Core.Models;

readonly record struct LogEntry(
    DiagnosticSeverity Severity,
    string Message,
    in ImmutableArray<Location> Locations = default,
    string? Id = null,
    Exception? Exception = null,
    Type? TargetType = null);