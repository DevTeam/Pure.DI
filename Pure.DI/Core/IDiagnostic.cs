namespace Pure.DI.Core;

internal interface IDiagnostic
{
    void Error(IEnumerable<CodeError> errors);

    void Error(string id, string message, params Location?[] locations);

    void Warning(string id, string message, params Location?[] locations);

    void Information(string id, string message, params Location?[] locations);
}