namespace Pure.DI.Core;

internal class DependencyGraphValidator: IValidator<DependencyGraph>
{
    private readonly ILogger<DependencyGraphValidator> _logger;

    public DependencyGraphValidator(ILogger<DependencyGraphValidator> logger)
    {
        _logger = logger;
    }

    public void Validate(in DependencyGraph data, in CancellationToken cancellationToken)
    {
        var graph = data.Graph;
        var unresolvedDependency = false;
        foreach (var dependency in graph.Edges.Where(i => !i.IsResolved))
        {
            unresolvedDependency = true;
            var errorMessage = $"Cannot resolve injected dependency [{dependency.Injection}] of {dependency.Target.KindName} {dependency.Target}.";
            var locationsWalker = new DependencyGraphLocationsWalker(dependency.Injection);
            locationsWalker.VisitDependencyNode(dependency.Target);
            foreach (var location in locationsWalker.Locations)
            {
                _logger.CompileError(errorMessage, location, LogId.ErrorUnresolved);   
            }
        }

        if (unresolvedDependency)
        {
            throw HandledException.Shared;
        }
    }
}