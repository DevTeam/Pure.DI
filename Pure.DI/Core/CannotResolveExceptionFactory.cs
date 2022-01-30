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

    public HandledException Create(IBindingMetadata binding, ExpressionSyntax? tag, string description)
    {
        var tagName = tag != default ? $"({tag})" : string.Empty;
        var error = new StringBuilder($"Cannot resolve {description} {binding.Implementation?.ToString() ?? binding.Factory?.ToString() ?? binding.ToString()}{tagName}.");
        var path = _tracer.Paths
                       .Where(i => i.Length > 1)
                       .OrderBy(i => i.Length)
                       .LastOrDefault()
                       ?.Reverse()
                       .Select(i => i.ToString())
                       .ToArray()
                   ?? Array.Empty<string>();

        if (path.Any())
        {
            error.Append($" The chain of dependencies that cannot be resolved: {string.Join(" -> ", path)}. Consider adding a binding {path.Last()} to some implementation or a factory at the DI setup.");
        }

        var errorMessage = error.ToString();
        _diagnostic.Error(Diagnostics.Error.CannotResolve, errorMessage, binding.Location);
        return new HandledException(errorMessage);
    }
}