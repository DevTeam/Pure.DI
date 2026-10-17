namespace Pure.DI.Core;

class DependencyNodePrioritizer(IOverloadResolutionPriority overloadResolutionPriority) : IDependencyNodePrioritizer
{
    public IEnumerable<DependencyNode> SortByPriority(IEnumerable<DependencyNode> nodes) =>
        nodes.GroupBy(i => i.Binding)
            .OrderBy(i => i.Key.Id)
            .SelectMany(grp => grp
                .OrderBy(i => i.Implementation?.Constructor.Ordinal ?? int.MaxValue)
                .ThenByDescending(GetOverloadResolutionPriority)
                .ThenByDescending(i => i.Implementation?.Constructor.Parameters.Count(p => !p.ParameterSymbol.IsOptional))
                .ThenByDescending(i => i.Implementation?.Constructor.Method.DeclaredAccessibility));

    private int GetOverloadResolutionPriority(DependencyNode node) =>
        node.Implementation?.Constructor is { Ordinal: null } constructor
            ? overloadResolutionPriority.Get(node.Binding.SemanticModel.Compilation, constructor.Method)
            : 0;
}
