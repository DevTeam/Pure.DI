namespace Pure.DI.Core;

internal class RootsBuilder: IBuilder<DependencyGraph, IReadOnlyDictionary<Injection, Root>>
{
    private readonly IBuilder<MdBinding, ISet<Injection>> _injectionsBuilder;

    public RootsBuilder(IBuilder<MdBinding, ISet<Injection>> injectionsBuilder) => _injectionsBuilder = injectionsBuilder;

    public IReadOnlyDictionary<Injection, Root> Build(DependencyGraph dependencyGraph, CancellationToken cancellationToken)
    {
        var rootsPairs = new List<KeyValuePair<Injection, Root>>();
            foreach (var curNode in dependencyGraph.Graph.Vertices)
            {
                cancellationToken.ThrowIfCancellationRequested();
                var node = curNode;
                if (node.Type is INamedTypeSymbol { IsUnboundGenericType: true })
                {
                    continue;
                }

                bool isRoot;
                var name = "";
                if (node.Root is { } root)
                {
                    if (dependencyGraph.Graph.TryGetEdges(node, out var rotDependencies))
                    {
                        node = rotDependencies.Single().Source;
                        isRoot = true;
                        name = root.Source.Name;
                    }
                    else
                    {
                        continue;
                    }
                }
                else
                {
                    isRoot = false;
                }

                var index = 0;
                foreach (var injection in _injectionsBuilder.Build(node.Binding, cancellationToken))
                {
                    if (!isRoot)
                    {
                        name = $"Root{node.Binding.Id}_{index++}{Variable.Postfix}";
                    }

                    rootsPairs.Add(new KeyValuePair<Injection, Root>(
                        injection,
                        new Root(
                            node,
                            injection,
                            isRoot,
                            name)));    
                }
            }

            return rootsPairs
                .GroupBy(i => i.Key)
                .ToDictionary(
                    i => i.Key,
                    i => i.OrderByDescending(j => j.Value.IsPublic).Select(j => j.Value).First());
    }
}