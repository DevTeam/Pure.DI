// ReSharper disable ClassNeverInstantiated.Global
namespace Pure.DI.Core;

internal sealed class LifetimesValidator(
    IPathsWalker<HashSet<object>> pathsWalker,
    [Tag(typeof(LifetimesValidatorVisitor))] Func<IPathVisitor<HashSet<object>>> visitorFactory,
    CancellationToken cancellationToken)
    : IValidator<DependencyGraph>
{
    public bool Validate(DependencyGraph dependencyGraph)
    {
        if (!dependencyGraph.IsResolved)
        {
            return false;
        }

        var warnings = new HashSet<object>();
        var graph = dependencyGraph.Graph;
        foreach (var root in dependencyGraph.Roots)
        {
            pathsWalker.Walk(
                warnings,
                graph,
                root.Value.Node,
                visitorFactory(),
                cancellationToken);
        }

        return true;
    }
}