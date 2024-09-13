// ReSharper disable HeapView.BoxingAllocation

namespace Pure.DI.Core.Models;

public readonly record struct LogEntry(
    DiagnosticSeverity Severity,
    string Message,
    Location? Location = default,
    string? Id = default,
    Exception? Exception = default);