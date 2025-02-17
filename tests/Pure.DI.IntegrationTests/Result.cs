// ReSharper disable NotAccessedPositionalProperty.Global

namespace Pure.DI.IntegrationTests;

using System.Text;
using Core.Models;

readonly record struct Result(
    bool Success,
    ImmutableArray<string> StdOut,
    IReadOnlyList<LogEntry> Logs,
    IReadOnlyList<LogEntry> Errors,
    IReadOnlyList<LogEntry> Warnings,
    IReadOnlyList<DependencyGraph> DependencyGraphs,
    string GeneratedCode)
{
    public override string ToString()
    {
        var text = new StringBuilder();
        if (!string.IsNullOrWhiteSpace(GeneratedCode))
        {
            text.AppendLine("Code:");
            text.AppendLine(GeneratedCode);
        }
        else
        {
            text.AppendLine("The code was not generated.");
        }

        text.AppendLine();
        foreach (var error in Errors)
        {
            text.AppendLine(error.Message);
        }

        return text.ToString();
    }

    public static implicit operator string(Result result) => result.ToString();
}