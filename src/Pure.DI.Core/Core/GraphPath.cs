namespace Pure.DI.Core;

internal class GraphPath : IGraphPath
{
    private readonly List<DependencyNode> _parts = new();
    private readonly HashSet<MdBinding> _bindings = new();
    private HashSet<MdBinding> _prevBindings = new();

    public bool TryAddPart(in DependencyNode node)
    {
        _parts.Add(node);

        // ReSharper disable once InvertIf
        if (node.Type is INamedTypeSymbol { IsGenericType: true, Name: "Func" } namedType
            && namedType.ContainingNamespace.Name == "System")
        {
            LazyBarrier();
        }

        return _bindings.Add(node.Binding);
    }

    public bool IsCompleted(in DependencyNode node) => 
        _prevBindings.Contains(node.Binding);

    private void LazyBarrier()
    {
        _prevBindings = new HashSet<MdBinding>(_bindings);
        _bindings.Clear();
    }

    public override string ToString() => string.Join(" <-- ", _parts.Select(i => i.Type));
}