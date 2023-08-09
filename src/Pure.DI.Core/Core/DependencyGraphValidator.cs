// ReSharper disable ClassNeverInstantiated.Global
namespace Pure.DI.Core;

internal sealed class DependencyGraphValidator: IValidator<DependencyGraph>
{
    private readonly ILogger<DependencyGraphValidator> _logger;
    private readonly IPathfinder _pathfinder;
    private readonly Func<IGraphPath> _pathFactory;
    private readonly CancellationToken _cancellationToken;

    public DependencyGraphValidator(
        ILogger<DependencyGraphValidator> logger,
        IPathfinder pathfinder,
        Func<IGraphPath> pathFactory,
        CancellationToken cancellationToken)
    {
        _logger = logger;
        _pathfinder = pathfinder;
        _pathFactory = pathFactory;
        _cancellationToken = cancellationToken;
    }

    public void Validate(in DependencyGraph dependencyGraph)
    {
        var graph = dependencyGraph.Graph;
        var isValid = dependencyGraph.IsValid;
        var isErrorReported = false;
        using (_logger.TraceProcess("search for unresolved nodes"))
        {
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
        }

        using (_logger.TraceProcess("search for cycles"))
        {
            var paths = new Dictionary<int, IGraphPath>();
            foreach (var rootNode in dependencyGraph.Roots.Select(i => i.Value.Node))
            {
                foreach (var (id, dependency) in _pathfinder.GetPaths(graph, rootNode))
                {
                    if (!paths.TryGetValue(id, out var path))
                    {
                        path = _pathFactory();
                        paths.Add(id, path);
                    }
                    
                    if (!path.TryAddPart(dependency.Target))
                    {
                        _logger.CompileError($"A cyclic dependency has been found: {path}.", dependencyGraph.Source.Source.GetLocation(), LogId.ErrorCyclicDependency);
                        isErrorReported = true;
                        isValid = false;
                        break;
                    }

                    if (path.IsCompleted(dependency.Target))
                    {
                        break;
                    }
                }
                
                paths.Clear();
            }
        }
        
        if (isValid)
        {
            return;
        }

        if (!isErrorReported)
        {
            _logger.CompileError("Cannot build a dependency graph.", dependencyGraph.Source.Source.GetLocation(), LogId.ErrorUnableToResolve);
        }

        throw HandledException.Shared;
    }
}