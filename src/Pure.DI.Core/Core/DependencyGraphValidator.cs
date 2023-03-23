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
        var isValid = data.IsValid;
        foreach (var dependency in graph.Edges.Where(i => !i.IsResolved))
        {
            var errorMessage = $"Cannot resolve injected dependency [{dependency.Injection}] of {dependency.Target.KindName} {dependency.Target}.";
            var locationsWalker = new DependencyGraphLocationsWalker(dependency.Injection);
            locationsWalker.VisitDependencyNode(dependency.Target);
            foreach (var location in locationsWalker.Locations)
            {
                _logger.CompileError(errorMessage, location, LogId.ErrorUnresolved);   
            }
        }

        var cycles = new List<(Dependency CyclicDependency, ImmutableArray<DependencyNode> Path)>();
        foreach (var rootNode in graph.Vertices.Where(i => i.Root is not {}))
        {
            if (!graph.TryGetEdges(rootNode, out var dependencies))
            {
                continue;
            }
            
            var path = new LinkedList<DependencyNode>();
            path.AddLast(rootNode);
            var ids = new HashSet<int>();
            var enumerators = new Stack<(int Id, ImmutableArray<Dependency>.Enumerator Enumerator)>();
            enumerators.Push((rootNode.Binding.Id, dependencies.GetEnumerator()));
            while (enumerators.TryPop(out var enumerator))
            {
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
                if (!graph.TryGetEdges(dependency.Source, out var nestedDependencies))
                {
                    continue;
                }
                
                enumerators.Push((dependency.Source.Binding.Id, nestedDependencies.GetEnumerator()));
            }
        }

        if (cycles.Any())
        {
            isValid = false;
            var hashes = new List<HashSet<int>>(); 
            foreach (var cycle in cycles)
            {
                var hash = cycle.Path.Select(i => i.Binding.Id).ToHashSet();
                if (hashes.Any(i => i.SetEquals(hash)))
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
                var pathDescription = string.Join("<--", cycle.Path.Select(i => $"[{i.Type}]"));
                var errorMessage = $"A cyclic dependency has been found {pathDescription}.";
                foreach (var location in locationsWalker.Locations)
                {
                    _logger.CompileError(errorMessage, location, LogId.ErrorCyclicDependency);
                }
            }
        }

        if (!isValid)
        {
            throw HandledException.Shared;
        }
    }
}