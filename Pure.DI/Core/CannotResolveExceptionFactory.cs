// ReSharper disable ClassNeverInstantiated.Global
namespace Pure.DI.Core;

internal sealed class CannotResolveExceptionFactory : ICannotResolveExceptionFactory
{
    private readonly IDiagnostic _diagnostic;

    public CannotResolveExceptionFactory(IDiagnostic diagnostic) =>
        _diagnostic = diagnostic;

    public HandledException Create(IBindingMetadata binding, IEnumerable<CodeError> errors)
    {
        _diagnostic.Error(
            errors.Select(error =>
                {
                    var newLocations = error.Locations.Concat(new[] { binding.Location }).Where(i => i != default).Select(i => i!).Distinct().ToArray();
                    return new CodeError(error.Dependency, error.Id, $"Cannot resolve {error.Dependency} {error.Description}", newLocations);
                })
            );

        return new HandledException("Cannot resolve");
    }
}
