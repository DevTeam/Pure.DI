// ReSharper disable HeapView.BoxingAllocation

// ReSharper disable NotAccessedPositionalProperty.Global
namespace Pure.DI.Core.Models;

public readonly record struct LogEntry(
    DiagnosticSeverity Severity,
    string Message,
    Location? Location = null,
    string? Id = null,
    Exception? Exception = null,
    Type? TargetType = null);