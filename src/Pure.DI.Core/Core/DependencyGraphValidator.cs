// ReSharper disable ClassNeverInstantiated.Global

namespace Pure.DI.Core;

sealed class DependencyGraphValidator(
    ILogger logger,
    ITypeResolver typeResolver,
    IFilter filter,
    Func<IDependencyGraphLocationsWalker> dependencyGraphLocationsWalkerFactory,
    ILocationProvider locationProvider,
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
        foreach (var dependency in graph.Edges.Where(i => !i.IsResolved))
        {
            cancellationToken.ThrowIfCancellationRequested();
            var setup = dependencyGraph.Source;
            if (setup.Hints.IsOnCannotResolveEnabled)
            {
                string GetContractName() => typeResolver.Resolve(setup, dependency.Injection.Type).Name;
                string GetTagName() => dependency.Injection.Tag.ValueToString();
                string GetLifetimeName() => dependency.Source.Lifetime.ValueToString();
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
            if (dependency.Target.Error is {} error)
            {
                logger.CompileError(error.ErrorMessage, error.Location, error.Id);
                isErrorReported = true;
                continue;
            }

            var errorMessage = string.Format(Strings.Error_Template_UnableToResolve, dependency.Injection, dependency.Target);
            var locationsWalker = dependencyGraphLocationsWalkerFactory().Initialize(dependency.Injection);
            locationsWalker.VisitDependencyNode(Unit.Shared, dependency.Target);
            foreach (var location in locationsWalker.Locations.Where(i => i.IsInSource).DefaultIfEmpty(locationProvider.GetLocation(dependency.Target.Binding.Source)))
            {
                logger.CompileError(errorMessage, location, LogId.ErrorUnableToResolve);
                isErrorReported = true;
            }
        }

        if (!isResolved && !isErrorReported)
        {
            logger.CompileError(Strings.Error_CannotBuildDependencyGraph, locationProvider.GetLocation(dependencyGraph.Source.Source), LogId.ErrorUnableToResolve);
        }

        throw HandledException.Shared;
    }
}