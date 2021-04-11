namespace Pure.DI.Core
{
    using Microsoft.CodeAnalysis;

    internal interface IDiagnostic
    {
        void Error(string id, string message, Location? location = null);

        void Warning(string id, string message, Location? location = null);

        void Information(string id, string message, Location? location = null);
    }
}