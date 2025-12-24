namespace Pure.DI.Core.Code;

class Constructors(
    IRegistry<int> bindingsRegistry,
    ICache<DependencyGraph, bool> isEnabledCache)
    : IConstructors
{
    public bool IsEnabled(DependencyGraph graph) => isEnabledCache.Get(graph, IsEnabledInternal);

    public bool IsEnabled(CompositionCode composition, ConstructorKind kind) =>
        kind switch
        {
            ConstructorKind.Default => composition.ClassArgs.Length == 0 && IsEnabled(composition.Source),
            ConstructorKind.Parameterized => composition.ClassArgs.Length > 0 && IsEnabled(composition.Source),
            ConstructorKind.Scope => IsScopeEnabled(composition.Source),
            _ => throw new ArgumentOutOfRangeException(nameof(kind), kind, null)
        };

    private bool IsEnabledInternal(DependencyGraph graph) => (
        from entry in graph.Graph.Entries
        from edge in entry.Edges
        from node in ImmutableArray.Create(edge.Source, edge.Target)
        where node.Arg is { Source.Kind: ArgKind.Composition }
        where bindingsRegistry.IsRegistered(graph.Source, node.BindingId)
        select node).Any() || IsScopeEnabled(graph);

    private bool IsScopeEnabled(DependencyGraph graph) => (
        from node in graph.Graph.Vertices
        where node.ActualLifetime == Lifetime.Scoped
        where bindingsRegistry.IsRegistered(graph.Source, node.BindingId)
        select node).Any();
}