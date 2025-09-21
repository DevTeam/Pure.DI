// ReSharper disable ClassNeverInstantiated.Global

namespace Pure.DI.Core;

sealed class CyclicDependenciesValidator(
    IGraphWalker<CyclicDependenciesValidatorContext, ImmutableArray<Dependency>> graphWalker,
    [Tag(typeof(CyclicDependencyValidatorVisitor))] IGraphVisitor<CyclicDependenciesValidatorContext, ImmutableArray<Dependency>> visitor,
    ILogger logger,
    ILocationProvider locationProvider,
    ITypeResolver typeResolver,
    CancellationToken cancellationToken)
    : IValidator<DependencyGraph>
{
    public bool Validate(DependencyGraph dependencyGraph)
    {
        var graph = dependencyGraph.Graph;
        var errors = new HashSet<object>();
        foreach (var root in dependencyGraph.Roots)
        {
            var ctx = new CyclicDependenciesValidatorContext(dependencyGraph, errors);
            var path = graphWalker.Walk(
                ctx,
                graph,
                root.Node,
                visitor, cancellationToken);

            if (ctx.Cyclicdependency is not {} dependency)
            {
                continue;
            }

            var pathStr = string.Join(" <-- ", path.Select(i => typeResolver.Resolve(ctx.DependencyGraph.Source, i.Source.Type).Name.Replace(Names.GlobalNamespacePrefix, "")));
            var locations = (dependency.Injection.Locations.IsDefault ? ImmutableArray<Location>.Empty : dependency.Injection.Locations)
                .AddRange(path.SelectMany(i => i.Injection.Locations.IsDefault ? ImmutableArray<Location>.Empty : i.Injection.Locations))
                .Add(locationProvider.GetLocation(dependency.Source.Binding.Source));

            logger.CompileError(string.Format(Strings.Error_Template_CyclicDependency, pathStr), locations, LogId.ErrorCyclicDependency);
        }

        return errors.Count == 0;
    }
}