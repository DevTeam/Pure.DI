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
            var errorMessage = string.Format(Strings.Error_Template_UnableToResolve, unresolvedInjection, target);
            var locationsWalker = dependencyGraphLocationsWalkerFactory().Initialize(unresolvedInjection);
            locationsWalker.VisitDependencyNode(Unit.Shared, target);
            foreach (var location in locationsWalker.Locations.Where(i => i.IsInSource).DefaultIfEmpty(target.Binding.Source.GetLocation()))
            {
                logger.CompileError(errorMessage, location, LogId.ErrorUnableToResolve);
                isErrorReported = true;
            }
        }

        if (!isResolved && !isErrorReported)
        {
            logger.CompileError(Strings.Error_CannotBuildDependencyGraph, dependencyGraph.Source.Source.GetLocation(), LogId.ErrorUnableToResolve);
        }

        throw HandledException.Shared;
    }
}