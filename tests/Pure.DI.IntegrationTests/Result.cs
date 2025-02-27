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
        text.AppendLine();
        text.AppendLine("Errors:");
        foreach (var error in Errors)
        {
            text.AppendLine($"{error.Message} at {error.Location.GetSource()}");
        }

        text.AppendLine();
        text.AppendLine("Warnings:");
        foreach (var warning in Warnings)
        {
            text.AppendLine($"{warning.Message} at {warning.Location.GetSource()}");
        }

        text.AppendLine();
        if (!string.IsNullOrWhiteSpace(GeneratedCode))
        {
            text.AppendLine("Code:");
            text.AppendLine(GeneratedCode);
        }
        else
        {
            text.AppendLine("The code was not generated.");
        }

        return text.ToString();
    }

    public static implicit operator string(Result result) => result.ToString();
}