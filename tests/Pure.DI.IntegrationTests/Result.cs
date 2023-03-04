// ReSharper disable NotAccessedPositionalProperty.Global
namespace Pure.DI.IntegrationTests;

using System.Collections.Immutable;
using Core.Models;

internal readonly record struct Result(
    bool Success,
    ImmutableArray<string> StdOut,
    ImmutableArray<string> StdErr,
    ImmutableArray<string> Logs,
    ImmutableArray<DependencyGraph> DependencyGraphs,
    string GeneratedCode);