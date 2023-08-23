// ReSharper disable ClassNeverInstantiated.Global
namespace Pure.DI.Core;

internal sealed class DependencyGraphValidator: IValidator<DependencyGraph>
{
    private readonly ILogger<DependencyGraphValidator> _logger;
    private readonly CancellationToken _cancellationToken;

    public DependencyGraphValidator(
        ILogger<DependencyGraphValidator> logger,
        CancellationToken cancellationToken)
    {
        _logger = logger;
        _cancellationToken = cancellationToken;
    }

    public void Validate(in DependencyGraph dependencyGraph)
    {
        var graph = dependencyGraph.Graph;
        var isValid = dependencyGraph.IsValid;
        if (isValid)
        {
            return;
        }
        
        var isErrorReported = false;
        foreach (var dependency in graph.Edges.Where(i => !i.IsResolved))
        {
            _cancellationToken.ThrowIfCancellationRequested();
            var errorMessage = $"Unable to resolve \"{dependency.Injection}\" in {dependency.Target}.";
            var locationsWalker = new DependencyGraphLocationsWalker(dependency.Injection);
            locationsWalker.VisitDependencyNode(dependency.Target);
            foreach (var location in locationsWalker.Locations)
            {
                _logger.CompileError(errorMessage, location, LogId.ErrorUnableToResolve);
                isErrorReported = true;
            }
        }

        if (!isErrorReported)
        {
            _logger.CompileError("Cannot build a dependency graph.", dependencyGraph.Source.Source.GetLocation(), LogId.ErrorUnableToResolve);
        }

        throw HandledException.Shared;
    }
}