// ReSharper disable NotAccessedPositionalProperty.Global

namespace Pure.DI.Core.Models;

readonly record struct LogInfo(
    in LogEntry Entry,
    DiagnosticDescriptor? DiagnosticDescriptor);