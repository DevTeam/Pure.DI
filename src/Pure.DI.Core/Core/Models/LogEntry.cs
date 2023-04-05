// ReSharper disable HeapView.BoxingAllocation
namespace Pure.DI.Core.Models;

public readonly record struct LogEntry(
    DiagnosticSeverity Severity,
    IEnumerable<string> Lines,
    Location? Location = default,
    string? Id = default,
    Exception? Exception = default,
    string Source = "",
    bool IsOutcome = false)
{
    public override string ToString()
    {
        return $"{Severity} {string.Join(Environment.NewLine, Lines)}";
    }
}