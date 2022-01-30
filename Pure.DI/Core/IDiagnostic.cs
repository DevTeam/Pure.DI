namespace Pure.DI.Core;

internal interface IDiagnostic
{
    void Error(string id, string message, Location? location = null);

    void Warning(string id, string message, Location? location = null);

    void Information(string id, string message, Location? location = null);
}