// ReSharper disable ClassNeverInstantiated.Global
namespace Pure.DI.Core;

internal class CannotResolveExceptionFactory : ICannotResolveExceptionFactory
{
    private readonly IDiagnostic _diagnostic;
    private readonly ITracer _tracer;

    public CannotResolveExceptionFactory(
        IDiagnostic diagnostic,
        ITracer tracer)
    {
        _diagnostic = diagnostic;
        _tracer = tracer;
    }

    public HandledException Create(IBindingMetadata binding, ExpressionSyntax? tag, string description, Location? location = default)
    {
        var tagName = tag != default ? $"({tag})" : string.Empty;
        var error = new StringBuilder($"Cannot resolve {binding.Implementation?.ToString() ?? binding.Factory?.ToString() ?? binding.ToString()}{tagName}: {description}.");
        var errorMessage = error.ToString();
        _diagnostic.Error(Diagnostics.Error.CannotResolve, errorMessage, location ?? binding.Location);
        return new HandledException(errorMessage);
    }
}