// ReSharper disable ClassNeverInstantiated.Global
namespace Pure.DI.Core;

internal class CannotResolveExceptionFactory : ICannotResolveExceptionFactory
{
    private readonly IDiagnostic _diagnostic;

    public CannotResolveExceptionFactory(IDiagnostic diagnostic) =>
        _diagnostic = diagnostic;

    public HandledException Create(IBindingMetadata binding, ExpressionSyntax? tag, CodeError[] errors)
    {
        var tagName = tag != default ? $"({tag})" : string.Empty;
        var baseErrorMessage = $"Cannot resolve {binding.Implementation?.ToString() ?? binding.Factory?.ToString() ?? binding.ToString()}{tagName}";
        errors = errors.Select(error =>
        {
            var errorMessage = new StringBuilder($"{baseErrorMessage}: {error.Description}.");
            var newLocations = error.Locations.Concat(new[] { binding.Location }).Where(i => i != default).Select(i => i!).Distinct().ToArray();
            return new CodeError(errorMessage.ToString(), newLocations);
        }).ToArray();
        
        _diagnostic.Error(Diagnostics.Error.CannotResolve, errors);
        return new HandledException($"{baseErrorMessage}.");
    }
}