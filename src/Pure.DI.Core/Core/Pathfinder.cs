namespace Pure.DI.Core;

internal class Pathfinder : IPathfinder
{
    private readonly CancellationToken _cancellationToken;

    public Pathfinder(CancellationToken cancellationToken)
    {
        _cancellationToken = cancellationToken;
    }

    public IEnumerable<(int pathId, Dependency dependency)> GetPaths(
        IGraph<DependencyNode, Dependency> graph,
        DependencyNode rootNode)
    {
        if (!graph.TryGetInEdges(rootNode, out var dependencies))
        {
            yield break;
        }

        var id = 0;
        var enumerators = new Stack<(int Id, IEnumerator<Dependency> Enumerator)>();
        enumerators.Push((rootNode.Binding.Id, dependencies.GetEnumerator()));
        while (enumerators.TryPop(out var enumerator))
        {
            _cancellationToken.ThrowIfCancellationRequested();
            if (!enumerator.Enumerator.MoveNext())
            {
                id++;  
                continue;
            }

            var dependency = enumerator.Enumerator.Current;
            yield return (id, dependency);
            enumerators.Push(enumerator);
            if (!graph.TryGetInEdges(dependency.Source, out var nestedDependencies))
            {
                continue;
            }

            enumerators.Push((dependency.Source.Binding.Id, nestedDependencies.GetEnumerator()));
        }
    }
}