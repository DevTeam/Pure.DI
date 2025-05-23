﻿// ReSharper disable ForeachCanBeConvertedToQueryUsingAnotherGetEnumerator
// ReSharper disable ClassNeverInstantiated.Global

namespace Pure.DI.Core;

sealed class FactoryDependencyNodeBuilder(
    IAttributes attributes,
    IInstanceDpProvider instanceDpProvider,
    ILocationProvider locationProvider)
    : IBuilder<DependencyNodeBuildContext, IEnumerable<DependencyNode>>
{
    public IEnumerable<DependencyNode> Build(DependencyNodeBuildContext ctx)
    {
        var setup = ctx.Setup;
        foreach (var binding in setup.Bindings)
        {
            if (binding.Factory is not {} factory)
            {
                continue;
            }

            var resolvers = new List<DpResolver>(factory.Resolvers.Length);
            foreach (var resolver in factory.Resolvers)
            {
                var tag = attributes.GetAttribute(resolver.SemanticModel, setup.TagAttributes, resolver.Attributes, AttributeKind.Tag, default(object?)) ?? resolver.Tag?.Value;
                var locations = ImmutableArray<Location>.Empty;
                if (resolver.TargetValue is not null)
                {
                    locations = locations.Add(locationProvider.GetLocation(resolver.TargetValue));
                }

                locations = locations.AddRange(resolver.ContractType.Locations);
                var injection = new Injection(InjectionKind.FactoryInjection, resolver.ContractType.WithNullableAnnotation(NullableAnnotation.NotAnnotated), tag, locations);
                resolvers.Add(new DpResolver(resolver, injection, CreateOverrides(resolver.Overrides)));
            }

            var initializers = new List<DpInitializer>(factory.Initializers.Length);
            foreach (var initializer in factory.Initializers)
            {
                if (initializer.Type is not INamedTypeSymbol targetType)
                {
                    continue;
                }

                var targetDp = instanceDpProvider.Get(setup, ctx.TypeConstructor, targetType);
                initializers.Add(new DpInitializer(initializer, targetDp.Methods, targetDp.Properties, targetDp.Fields, CreateOverrides(initializer.Overrides), locationProvider));
            }

            var dpFactory = new DpFactory(factory, binding, resolvers.ToImmutableArray(), initializers.ToImmutableArray(), new Dictionary<int, DpOverride>(), locationProvider);
            yield return new DependencyNode(0, binding, ctx.TypeConstructor, Factory: dpFactory);
        }
    }

    private ImmutableArray<DpOverride> CreateOverrides(in ImmutableArray<MdOverride> overrides) =>
        overrides.IsDefault
            ? ImmutableArray<DpOverride>.Empty
            : overrides.AsEnumerable()
                .Select(i => new DpOverride(i, i.Tags.Select(tag =>
                    new Injection(
                        InjectionKind.Override,
                        i.ContractType,
                        tag.Value,
                        ImmutableArray.Create(locationProvider.GetLocation(i.Source)).AddRange(i.ContractType.Locations))).ToImmutableArray()))
                .ToImmutableArray();
}