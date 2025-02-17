// ReSharper disable ClassNeverInstantiated.Global

namespace Pure.DI.Core;

sealed class LifetimesValidator(
    IGraphWalker<HashSet<object>, ImmutableArray<DependencyNode>> graphWalker,
    [Tag(typeof(LifetimesValidatorVisitor))] IGraphVisitor<HashSet<object>, ImmutableArray<DependencyNode>> visitor,
    CancellationToken cancellationToken)
    : IValidator<DependencyGraph>
{
    public bool Validate(DependencyGraph dependencyGraph)
    {
        if (!dependencyGraph.IsResolved)
        {
            return false;
        }

        var errors = new HashSet<object>();
        var graph = dependencyGraph.Graph;
        foreach (var root in dependencyGraph.Roots)
        {
            graphWalker.Walk(
                errors,
                graph,
                root.Value.Node,
                visitor,
                cancellationToken);
        }

        return errors.Count == 0;
    }
}