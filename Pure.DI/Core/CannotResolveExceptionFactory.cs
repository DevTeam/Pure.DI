// ReSharper disable ClassNeverInstantiated.Global
namespace Pure.DI.Core;

internal class CannotResolveExceptionFactory : ICannotResolveExceptionFactory
{
    private readonly IDiagnostic _diagnostic;

    public CannotResolveExceptionFactory(IDiagnostic diagnostic) =>
        _diagnostic = diagnostic;

    public HandledException Create(IBindingMetadata binding, ExpressionSyntax? tag, string description, params Location[] locations)
    {
        var tagName = tag != default ? $"({tag})" : string.Empty;
        var error = new StringBuilder($"Cannot resolve {binding.Implementation?.ToString() ?? binding.Factory?.ToString() ?? binding.ToString()}{tagName}: {description}.");
        var errorMessage = error.ToString();
        var newLocations = locations.Concat(new []{binding.Location}).Where(i => i!= default).Select(i => i!).Distinct().ToArray();
        _diagnostic.Error(Diagnostics.Error.CannotResolve, errorMessage, newLocations);
        return new HandledException(errorMessage);
    }
}