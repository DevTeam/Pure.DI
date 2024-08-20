// ReSharper disable ClassNeverInstantiated.Global
namespace Pure.DI.Core;

internal sealed class LifetimesValidator(
    IPathsWalker<HashSet<object>> pathsWalker,
    [Tag(typeof(LifetimesValidatorVisitor))] IPathVisitor<HashSet<object>> visitor,
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