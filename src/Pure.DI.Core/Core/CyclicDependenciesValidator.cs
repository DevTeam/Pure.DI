// ReSharper disable ClassNeverInstantiated.Global

namespace Pure.DI.Core;

sealed class CyclicDependenciesValidator(
    IGraphWalker<CyclicDependenciesValidatorContext, ImmutableArray<Dependency>> graphWalker,
    IGraphVisitor<CyclicDependenciesValidatorContext, ImmutableArray<Dependency>> visitor,
    [Tag(Tag.Local)] ICache<SingleDependencyValidationKey, bool> validatedSingleDependencies,
    ILogger logger,
    ILocationProvider locationProvider,
    ITypeResolver typeResolver,
    CancellationToken cancellationToken)
    : IValidator<DependencyGraph>
{
    public bool Validate(DependencyGraph dependencyGraph)
    {
        var errors = new HashSet<object>();
        foreach (var root in dependencyGraph.Roots)
        {
            var hasSingleDependency = dependencyGraph.TryGetSingleResolvedDependency(root.Node, out var singleDependency);
            var validationKey = new SingleDependencyValidationKey(dependencyGraph.Graph, singleDependency);
            if (hasSingleDependency && validatedSingleDependencies.TryGet(validationKey, out _))
            {
                continue;
            }

            var ctx = new CyclicDependenciesValidatorContext(dependencyGraph, errors);
            var path = graphWalker.Walk(
                ctx,
                dependencyGraph,
                root.Node,
                visitor, cancellationToken);

            if (ctx.Cyclicdependency is not {} dependency)
            {
                if (hasSingleDependency)
                {
                    validatedSingleDependencies.Set(validationKey, true);
                }

                continue;
            }

            var pathStr = string.Join(" <-- ", path.Select(i => typeResolver.Resolve(ctx.DependencyGraph.Source, i.Source.Type).Name.Replace(Names.GlobalNamespacePrefix, "")));
            var locations = (dependency.Injection.Locations.IsDefault ? ImmutableArray<Location>.Empty : dependency.Injection.Locations)
                .AddRange(path.SelectMany(i => i.Injection.Locations.IsDefault ? ImmutableArray<Location>.Empty : i.Injection.Locations))
                .Add(locationProvider.GetLocation(dependency.Source.Binding.Source));

            logger.CompileError(
                LogMessage.Format(nameof(Strings.Error_Template_CyclicDependency), Strings.Error_Template_CyclicDependency, pathStr),
                locations,
                LogId.ErrorCyclicDependency);
        }

        return errors.Count == 0;
    }
}
