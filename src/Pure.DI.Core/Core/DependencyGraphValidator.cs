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
        var isErrorReported = false;
        using (_logger.TraceProcess("search for unresolved nodes"))
        {
            foreach (var dependency in graph.Edges.Where(i => !i.IsResolved))
            {
                _cancellationToken.ThrowIfCancellationRequested();
                var errorMessage = $"Cannot resolve dependency \"{dependency.TargetSymbol?.ToString() ?? dependency.Injection.ToString()}\" in {dependency.Target.Type}.";
                var locationsWalker = new DependencyGraphLocationsWalker(dependency.Injection);
                locationsWalker.VisitDependencyNode(dependency.Target);
                foreach (var location in locationsWalker.Locations)
                {
                    _logger.CompileError(errorMessage, location, LogId.ErrorUnresolvedDependency);
                    isErrorReported = true;
                }
            }
        }

        var cycles = new List<(Dependency CyclicDependency, ImmutableArray<DependencyNode> Path)>();
        using (_logger.TraceProcess("search for cycles"))
        {
            foreach (var rootNode in dependencyGraph.Roots.Select(i => i.Value.Node))
            {
                _cancellationToken.ThrowIfCancellationRequested();
                if (!graph.TryGetInEdges(rootNode, out var dependencies))
                {
                    continue;
                }

                var path = new LinkedList<DependencyNode>();
                path.AddLast(rootNode);
                var ids = new HashSet<int>();
                var enumerators = new Stack<(int Id, IEnumerator<Dependency> Enumerator)>();
                enumerators.Push((rootNode.Binding.Id, dependencies.GetEnumerator()));
                while (enumerators.TryPop(out var enumerator))
                {
                    _cancellationToken.ThrowIfCancellationRequested();
                    if (!enumerator.Enumerator.MoveNext())
                    {
                        if (path.Count > 0)
                        {
                            path.RemoveLast();
                        }

                        ids.Remove(enumerator.Id);
                        continue;
                    }

                    var dependency = enumerator.Enumerator.Current;
                    if (!ids.Add(dependency.Source.Binding.Id))
                    {
                        cycles.Add((dependency, path.ToImmutableArray()));
                        break;
                    }

                    path.AddLast(dependency.Source);
                    enumerators.Push(enumerator);
                    if (!graph.TryGetInEdges(dependency.Source, out var nestedDependencies))
                    {
                        continue;
                    }

                    enumerators.Push((dependency.Source.Binding.Id, nestedDependencies.GetEnumerator()));
                }
            }
        }

        if (cycles.Any())
        {
            isValid = false;
            foreach (var cycle in cycles)
            {
                _cancellationToken.ThrowIfCancellationRequested();
                var pathDescription = string.Join(" <-- ", cycle.Path.Select(i => $"{i.Type}"));
                _logger.CompileError($"A cyclic dependency has been found {pathDescription}.", dependencyGraph.Source.Source.GetLocation(), LogId.ErrorCyclicDependency);
                isErrorReported = true;
            }
        }

        if (isValid)
        {
            return;
        }

        if (!isErrorReported)
        {
            _logger.CompileError("Cannot build a dependency graph.", dependencyGraph.Source.Source.GetLocation(), LogId.ErrorUnresolvedDependency);
        }

        throw HandledException.Shared;
    }
}