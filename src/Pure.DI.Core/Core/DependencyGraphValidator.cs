// ReSharper disable ClassNeverInstantiated.Global
namespace Pure.DI.Core;

internal sealed class DependencyGraphValidator(
    ILogger<DependencyGraphValidator> logger,
    CancellationToken cancellationToken)
    : IValidator<DependencyGraph>
{
    public bool Validate(in DependencyGraph dependencyGraph)
    {
        var graph = dependencyGraph.Graph;
        var isValid = dependencyGraph.IsValid;
        if (isValid)
        {
            return true;
        }
        
        var isErrorReported = false;
        foreach (var dependency in graph.Edges.Where(i => !i.IsResolved))
        {
            cancellationToken.ThrowIfCancellationRequested();
            var errorMessage = $"Unable to resolve \"{dependency.Injection}\" in {dependency.Target}.";
            var locationsWalker = new DependencyGraphLocationsWalker(dependency.Injection);
            locationsWalker.VisitDependencyNode(Unit.Shared, dependency.Target);
            foreach (var location in locationsWalker.Locations)
            {
                logger.CompileError(errorMessage, location, LogId.ErrorUnableToResolve);
                isErrorReported = true;
            }
        }

        if (!isErrorReported)
        {
            logger.CompileError("Cannot build a dependency graph.", dependencyGraph.Source.Source.GetLocation(), LogId.ErrorUnableToResolve);
        }

        throw HandledException.Shared;
    }
}