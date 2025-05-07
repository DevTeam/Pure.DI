// ReSharper disable ClassNeverInstantiated.Global

namespace Pure.DI.Core;

sealed class CyclicDependenciesValidator(
    IGraphWalker<HashSet<object>, ImmutableArray<Dependency>> graphWalker,
    [Tag(typeof(CyclicDependencyValidatorVisitor))] IGraphVisitor<HashSet<object>, ImmutableArray<Dependency>> visitor,
    CancellationToken cancellationToken)
    : IValidator<DependencyGraph>
{
    public bool Validate(DependencyGraph dependencyGraph)
    {
        var graph = dependencyGraph.Graph;
        var errors = new HashSet<object>();
        foreach (var root in dependencyGraph.Roots)
        {
            graphWalker.Walk(
                errors,
                graph,
                root.Node,
                visitor,
                cancellationToken);
        }

        return errors.Count == 0;
    }
}