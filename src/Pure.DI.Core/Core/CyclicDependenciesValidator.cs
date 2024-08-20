
// ReSharper disable ClassNeverInstantiated.Global
namespace Pure.DI.Core;

internal sealed class CyclicDependenciesValidator(
    IPathsWalker<HashSet<object>> pathsWalker,
    [Tag(typeof(CyclicDependencyValidatorVisitor))] IPathVisitor<HashSet<object>> visitor,
    CancellationToken cancellationToken)
    : IValidator<DependencyGraph>
{
    public bool Validate(DependencyGraph dependencyGraph)
    {
        var graph = dependencyGraph.Graph;
        var errors = new HashSet<object>();
        foreach (var root in dependencyGraph.Roots)
        {
            pathsWalker.Walk(
                errors,
                graph,
                root.Value.Node,
                visitor,
                cancellationToken);
        }

        return errors.Count == 0;
    }
}