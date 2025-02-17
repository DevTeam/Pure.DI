// ReSharper disable ClassNeverInstantiated.Global

namespace Pure.DI.Core;

sealed class DependencyGraphValidator(
    ILogger logger,
    ITypeResolver typeResolver,
    IFilter filter,
    Func<IDependencyGraphLocationsWalker> dependencyGraphLocationsWalkerFactory,
    CancellationToken cancellationToken)
    : IValidator<DependencyGraph>
{
    public bool Validate(DependencyGraph dependencyGraph)
    {
        var graph = dependencyGraph.Graph;
        var isResolved = dependencyGraph.IsResolved;
        if (isResolved)
        {
            return true;
        }

        var isErrorReported = false;
        foreach (var (_, dependencyNode, unresolvedInjection, target) in graph.Edges.Where(i => !i.IsResolved))
        {
            cancellationToken.ThrowIfCancellationRequested();
            var setup = dependencyGraph.Source;
            if (setup.Hints.IsOnCannotResolveEnabled)
            {
                string GetContractName() => typeResolver.Resolve(setup, unresolvedInjection.Type).Name;
                string GetTagName() => unresolvedInjection.Tag.ValueToString();
                string GetLifetimeName() => dependencyNode.Lifetime.ValueToString();
                if (filter.IsMeet(
                        setup,
                        (Hint.OnCannotResolveContractTypeNameRegularExpression, Hint.OnCannotResolveContractTypeNameWildcard, GetContractName),
                        (Hint.OnCannotResolveTagRegularExpression, Hint.OnCannotResolveTagWildcard, GetTagName),
                        (Hint.OnCannotResolveLifetimeRegularExpression, Hint.OnCannotResolveLifetimeWildcard, GetLifetimeName)))
                {
                    continue;
                }
            }

            isResolved = false;
            var errorMessage = $"Unable to resolve \"{unresolvedInjection}\" in {target}.";
            var locationsWalker = dependencyGraphLocationsWalkerFactory().Initialize(unresolvedInjection);
            locationsWalker.VisitDependencyNode(Unit.Shared, target);
            foreach (var location in locationsWalker.Locations)
            {
                logger.CompileError(errorMessage, location, LogId.ErrorUnableToResolve);
                isErrorReported = true;
            }
        }

        if (!isResolved && !isErrorReported)
        {
            logger.CompileError("Cannot build a dependency graph.", dependencyGraph.Source.Source.GetLocation(), LogId.ErrorUnableToResolve);
        }

        throw HandledException.Shared;
    }
}