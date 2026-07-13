// ReSharper disable ClassNeverInstantiated.Global

namespace Pure.DI.Core;

sealed class DependencyGraphValidator(
    ILogger logger,
    ITypeResolver typeResolver,
    IFilter filter,
    Func<Injection, IDependencyGraphLocationsWalker> dependencyGraphLocationsWalkerFactory,
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
            if (dependency.Target.Error is {} targetError)
            {
                logger.CompileError(
                    new LogMessage(targetError.MessageKey, targetError.ErrorMessage),
                    targetError.Locations,
                    targetError.Id);
                isErrorReported = true;
                continue;
            }

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
            var locationsWalker = dependencyGraphLocationsWalkerFactory(dependency.Injection);
            locationsWalker.VisitDependencyNode(Unit.Shared, dependency.Target);
            var locations = locationsWalker.Locations.ToImmutableArray().Add(locationProvider.GetLocation(dependency.Target.Binding.Source));
            logger.CompileError(
                LogMessage.Format(nameof(Strings.Error_Template_UnableToResolve), Strings.Error_Template_UnableToResolve, dependency.Injection, dependency.Target),
                locations,
                LogId.ErrorUnableToResolve);
            isErrorReported = true;
        }

        if (!isResolved && !isErrorReported)
        {
            logger.CompileError(
                LogMessage.From(nameof(Strings.Error_CannotBuildDependencyGraph), Strings.Error_CannotBuildDependencyGraph),
                ImmutableArray.Create(locationProvider.GetLocation(dependencyGraph.Source.Source)),
                LogId.ErrorUnableToResolve);
        }

        throw HandledException.Shared;
    }
}
