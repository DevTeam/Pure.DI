// ReSharper disable NotAccessedPositionalProperty.Global

namespace Pure.DI.Core.Models;

internal readonly record struct LogInfo(
    in LogEntry Entry,
    DiagnosticDescriptor? DiagnosticDescriptor);