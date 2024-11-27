// ReSharper disable ForeachCanBeConvertedToQueryUsingAnotherGetEnumerator
// ReSharper disable ClassNeverInstantiated.Global

namespace Pure.DI.Core;

internal sealed class FactoryDependencyNodeBuilder(
    IAttributes attributes,
    IInstanceDpProvider instanceDpProvider)
    : IBuilder<DependencyNodeBuildContext, IEnumerable<DependencyNode>>
{
    public IEnumerable<DependencyNode> Build(DependencyNodeBuildContext ctx)
    {
        var setup = ctx.Setup;
        foreach (var binding in setup.Bindings)
        {
            if (binding.Factory is not { } factory)
            {
                continue;
            }

            var injections = new List<Injection>(factory.Resolvers.Length);
            foreach (var resolver in factory.Resolvers)
            {
                var tag = attributes.GetAttribute(resolver.SemanticModel, setup.TagAttributes, resolver.Attributes, default(object?)) ?? resolver.Tag?.Value;
                injections.Add(new Injection(InjectionKind.FactoryInjection, resolver.ContractType.WithNullableAnnotation(NullableAnnotation.NotAnnotated), tag));
            }

            var compilation = binding.SemanticModel.Compilation;
            var initializers = new List<DpInitializer>();
            foreach (var initializer in factory.Initializers)
            {
                if (initializer.Type is not INamedTypeSymbol targetType)
                {
                    continue;
                }
                
                var targetDp = instanceDpProvider.Get(setup, ctx.TypeConstructor, compilation, targetType);
                initializers.Add(new DpInitializer(initializer, targetDp.Methods, targetDp.Properties, targetDp.Fields));
            }

            yield return new DependencyNode(0, binding, Factory: new DpFactory(factory, binding, injections.ToImmutableArray(), initializers.ToImmutableArray()));
        }
    }
}