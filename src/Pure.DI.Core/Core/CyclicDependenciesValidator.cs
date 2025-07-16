// ReSharper disable ClassNeverInstantiated.Global

namespace Pure.DI.Core;

sealed class CyclicDependenciesValidator(
    IGraphWalker<CyclicDependenciesValidatorContext, ImmutableArray<Dependency>> graphWalker,
    [Tag(typeof(CyclicDependencyValidatorVisitor))] IGraphVisitor<CyclicDependenciesValidatorContext, ImmutableArray<Dependency>> visitor,
    CancellationToken cancellationToken)
    : IValidator<DependencyGraph>
{
    public bool Validate(DependencyGraph dependencyGraph)
    {
        var graph = dependencyGraph.Graph;
        var errors = new HashSet<object>();
        var ctx = new CyclicDependenciesValidatorContext(dependencyGraph, errors);
        foreach (var root in dependencyGraph.Roots)
        {
            graphWalker.Walk(
                ctx,
                graph,
                root.Node,
                visitor,
                cancellationToken);
        }

        return errors.Count == 0;
    }
}