// ReSharper disable ClassNeverInstantiated.Global

namespace Pure.DI.Core.Code;

sealed class ScopedDisposeOrder(
    IConstructors constructors,
    INodeTools nodeTools)
    : IScopedDisposeOrder
{
    // An identifier takes 4 bits, and 0 marks the end of the order.
    private const int MaxCount = 15;

    public bool IsEnabled(DependencyGraph graph)
    {
        if (!constructors.IsScopeEnabled(graph))
        {
            return false;
        }

        var count = graph.Graph.Vertices.Count(node => node.ActualLifetime == Lifetime.Scoped && nodeTools.IsDisposableAny(node));
        return count is > 0 and <= MaxCount;
    }

    public IEnumerable<VarDeclaration> GetTrackedFields(CompositionCode composition) =>
        composition.Singletons.Where(field => field.Node.ActualLifetime == Lifetime.Scoped && nodeTools.IsDisposableAny(field.Node.Node));

    public string GetIdName(VarDeclaration field) =>
        field.Name + Names.DisposeIdSuffix;
}
