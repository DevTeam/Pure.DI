namespace Pure.DI.Core;

static class GraphCycles
{
    // The strongly connected components of the graph that have cycles, by Tarjan's algorithm.
    public static List<List<DependencyNode>> GetCycles(IGraph<DependencyNode, Dependency> graph)
    {
        var cycles = new List<List<DependencyNode>>();
        var indexes = new Dictionary<int, (int Index, int LowLink)>();
        var onStack = new HashSet<int>();
        var stack = new Stack<DependencyNode>();
        var index = 0;
        foreach (var vertex in graph.Vertices)
        {
            if (indexes.ContainsKey(vertex.Binding.Id))
            {
                continue;
            }

            // The iterative version keeps the position in the dependencies of each node on the path.
            var path = new Stack<(DependencyNode Node, IEnumerator<Dependency> Dependencies)>();
            Enter(vertex);
            while (path.Count > 0)
            {
                var (node, dependencies) = path.Peek();
                if (dependencies.MoveNext())
                {
                    var source = dependencies.Current.Source;
                    if (!dependencies.Current.IsResolved)
                    {
                        continue;
                    }

                    if (!indexes.TryGetValue(source.Binding.Id, out var sourceIndex))
                    {
                        Enter(source);
                    }
                    else if (onStack.Contains(source.Binding.Id))
                    {
                        var current = indexes[node.Binding.Id];
                        indexes[node.Binding.Id] = (current.Index, Math.Min(current.LowLink, sourceIndex.Index));
                    }

                    continue;
                }

                path.Pop();
                var nodeIndex = indexes[node.Binding.Id];
                if (path.Count > 0)
                {
                    var parent = path.Peek().Node;
                    var parentIndex = indexes[parent.Binding.Id];
                    indexes[parent.Binding.Id] = (parentIndex.Index, Math.Min(parentIndex.LowLink, nodeIndex.LowLink));
                }

                if (nodeIndex.LowLink != nodeIndex.Index)
                {
                    continue;
                }

                var component = new List<DependencyNode>();
                DependencyNode member;
                do
                {
                    member = stack.Pop();
                    onStack.Remove(member.Binding.Id);
                    component.Add(member);
                } while (member.Binding.Id != node.Binding.Id);

                if (component.Count > 1 || graph.TryGetInEdges(node, out var edges) && edges.Any(i => i.IsResolved && i.Source.Binding.Id == node.Binding.Id))
                {
                    cycles.Add(component);
                }
            }

            void Enter(DependencyNode node)
            {
                indexes[node.Binding.Id] = (index, index);
                index++;
                stack.Push(node);
                onStack.Add(node.Binding.Id);
                path.Push((node, graph.TryGetInEdges(node, out var nodeDependencies) ? nodeDependencies.GetEnumerator() : Enumerable.Empty<Dependency>().GetEnumerator()));
            }
        }

        return cycles;
    }
}
