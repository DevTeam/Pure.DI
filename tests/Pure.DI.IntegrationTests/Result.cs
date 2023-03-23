// ReSharper disable NotAccessedPositionalProperty.Global
namespace Pure.DI.IntegrationTests;

using Core.Models;

internal readonly record struct Result(
    bool Success,
    ImmutableArray<string> StdOut,
    IReadOnlyList<LogEntry> Logs,
    IReadOnlyList<LogEntry> Errors,
    IReadOnlyList<LogEntry> Warnings,
    IReadOnlyList<DependencyGraph> DependencyGraphs,
    string GeneratedCode);