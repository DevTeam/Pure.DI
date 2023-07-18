// ReSharper disable ClassNeverInstantiated.Global
namespace Pure.DI.Core;

internal class DependencyGraphValidator: IValidator<DependencyGraph>
{
    private readonly ILogger<DependencyGraphValidator> _logger;

    public DependencyGraphValidator(ILogger<DependencyGraphValidator> logger)
    {
        _logger = logger;
    }

    public void Validate(in DependencyGraph data, CancellationToken cancellationToken)
    {
        var graph = data.Graph;
        var isValid = data.IsValid;
        var isErrorReported = false;
        using (_logger.TraceProcess("search for unresolved nodes"))
        {
            foreach (var dependency in graph.Edges.Where(i => !i.IsResolved))
            {
                cancellationToken.ThrowIfCancellationRequested();
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
            foreach (var rootNode in data.Roots.Select(i => i.Value.Node))
            {
                cancellationToken.ThrowIfCancellationRequested();
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
                    cancellationToken.ThrowIfCancellationRequested();
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
            var hashes = new List<HashSet<int>>();
            var locations = new HashSet<Location>();
            foreach (var cycle in cycles)
            {
                cancellationToken.ThrowIfCancellationRequested();
                var hash = new HashSet<int>(cycle.Path.Select(i => i.Binding.Id));
                if (hashes.Any(i => hash.IsSubsetOf(i)))
                {
                    continue;
                }
                
                var locationsWalker = new DependencyGraphLocationsWalker(cycle.CyclicDependency.Injection);
                locationsWalker.VisitDependencyNode(cycle.CyclicDependency.Target);
                if (!locationsWalker.Locations.Any())
                {
                    continue;
                }
                
                hashes.Add(hash);
                foreach (var location in locationsWalker.Locations.Take(1).Where(i => locations.Add(i)))
                {
                    var pathDescription = string.Join(" <-- ", cycle.Path.Select(i => $"{i.Type}"));
                    _logger.CompileError($"A cyclic dependency has been found {pathDescription}.", location, LogId.ErrorCyclicDependency);
                    isErrorReported = true;    
                }
            }
        }

        if (isValid)
        {
            return;
        }

        if (!isErrorReported)
        {
            _logger.CompileError("Cannot build a dependency graph.", data.Source.Source.GetLocation(), LogId.ErrorUnresolvedDependency);
        }

        throw HandledException.Shared;
    }
}