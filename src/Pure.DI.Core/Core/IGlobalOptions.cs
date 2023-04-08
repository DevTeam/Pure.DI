namespace Pure.DI.Core;

internal interface IGlobalOptions
{
    string? LogFile { get; }

    DiagnosticSeverity Severity { get; }

    int MaxIterations { get; }
}