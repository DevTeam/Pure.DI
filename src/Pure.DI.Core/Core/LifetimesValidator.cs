// ReSharper disable ClassNeverInstantiated.Global

namespace Pure.DI.Core;

sealed class LifetimesValidator(
    IGraphWalker<LifetimesValidatorContext, ImmutableArray<Dependency>> graphWalker,
    IGraphVisitor<LifetimesValidatorContext, ImmutableArray<Dependency>> visitor,
    [Tag(Tag.Local)] ICache<LifetimesValidationKey, bool> validatedSingleDependencies,
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
        foreach (var root in dependencyGraph.Roots)
        {
            var hasSingleDependency = dependencyGraph.TryGetSingleResolvedDependency(root.Node, out var singleDependency);
            var validationKey = new LifetimesValidationKey(
                dependencyGraph.Graph,
                singleDependency,
                root.Node.Lifetime,
                root.IsStatic);
            if (hasSingleDependency && validatedSingleDependencies.TryGet(validationKey, out _))
            {
                continue;
            }

            var ctx = new LifetimesValidatorContext(root, errors);
            graphWalker.Walk(
                ctx,
                dependencyGraph,
                root.Node,
                visitor,
                cancellationToken);

            if (hasSingleDependency && !ctx.HasErrors)
            {
                validatedSingleDependencies.Set(validationKey, true);
            }
        }

        return errors.Count == 0;
    }
}

[SuppressMessage("ReSharper", "NotAccessedPositionalProperty.Global")]
readonly record struct LifetimesValidationKey(
    IGraph<DependencyNode, Dependency> Graph,
    DependencyNode Dependency,
    Lifetime RootLifetime,
    bool IsStatic);
