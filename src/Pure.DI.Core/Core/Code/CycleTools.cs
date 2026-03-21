namespace Pure.DI.Core.Code;

class CycleTools : ICycleTools
{
    public DependencyNode? GetCyclicNode(
        IGraph<DependencyNode, Dependency> graph,
        DependencyNode node)
    {
        var visited = new HashSet<DependencyNode>();
        var recursionStack = new HashSet<DependencyNode>();
        var stack = new Stack<(DependencyNode node, IEnumerator<Dependency>? enumerator)>();
        stack.Push((node, null));
        while (stack.Count > 0)
        {
            var (currentNode, enumerator) = stack.Peek();
            if (enumerator == null)
            {
                if (visited.Contains(currentNode) && !recursionStack.Contains(currentNode))
                {
                    stack.Pop();
                    continue;
                }
                
                visited.Add(currentNode);
                recursionStack.Add(currentNode);
                
                if (!graph.TryGetInEdges(currentNode, out var dependencies))
                {
                    stack.Pop();
                    recursionStack.Remove(currentNode);
                    continue;
                }
                
                stack.Pop();
                // ReSharper disable once GenericEnumeratorNotDisposed
                stack.Push((currentNode, dependencies.GetEnumerator()));
            }
            else
            {
                if (!enumerator.MoveNext())
                {
                    enumerator.Dispose();
                    stack.Pop();
                    recursionStack.Remove(currentNode);
                    continue;
                }
                
                var dependency = enumerator.Current;
                if (!visited.Contains(dependency.Source))
                {
                    stack.Push((dependency.Source, null));
                }
                else if (recursionStack.Contains(dependency.Source))
                {
                    enumerator.Dispose();
                    return dependency.Source;
                }
            }
        }
        
        return null;
    }
}