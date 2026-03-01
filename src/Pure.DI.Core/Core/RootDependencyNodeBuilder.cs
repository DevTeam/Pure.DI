// ReSharper disable ClassNeverInstantiated.Global

namespace Pure.DI.Core;

sealed class RootDependencyNodeBuilder(
    ILocationProvider locationProvider,
    ITypes types,
    IBindingsFactory bindingsFactory)
    : IBuilder<DependencyNodeBuildContext, IEnumerable<DependencyNode>>
{
    public IEnumerable<DependencyNode> Build(DependencyNodeBuildContext ctx)
    {
        var id = -1;
        var setup = ctx.Setup;
        var roots = ctx.Setup.Roots;
        foreach (var root in roots)
        {
            var rootBinding = new MdBinding(
                id--,
                root.Source,
                setup,
                root.SemanticModel,
                ImmutableArray<MdContract>.Empty,
                ImmutableArray<MdTag>.Empty);

            yield return new DependencyNode(
                0,
                rootBinding,
                ctx.TypeConstructor,
                new DpRoot(
                    root,
                    rootBinding,
                    new Injection(
                        InjectionKind.Root,
                        RefKind.None,
                        root.RootType.WithNullableAnnotation(NullableAnnotation.NotAnnotated),
                        root.Tag?.Value,
                        root.RootType.Locations),
                    locationProvider));
        }

        // ReSharper disable once InvertIf
        if (setup.Kind == CompositionKind.Public
            && roots.Any(i => i.Kind.HasFlag(RootKinds.Light))
            && types.TryGet(SpecialType.LightweightRoot, setup.SemanticModel.Compilation) is {} rootType)
        {
            var root = new MdRoot(
                id,
                setup.Source,
                setup.SemanticModel,
                rootType,
                Names.LightweightRootName,
                Names.LightweightRootName,
                null,
                RootKinds.Private,
                ImmutableArray<string>.Empty,
                rootType,
                false,
                ImmutableArray<MdRoot>.Empty,
                LightweightKind: LightweightKind.TransientComposition);

            var binding = bindingsFactory.CreateLightweightRootBinding(setup, root);

            var rootInjection = new Injection(
                InjectionKind.Root,
                RefKind.None,
                rootType,
                null,
                ImmutableArray<Location>.Empty);

            yield return new DependencyNode(
                0,
                binding,
                ctx.TypeConstructor,
                new DpRoot(
                    root,
                    binding,
                    rootInjection,
                    locationProvider));
        }
    }
}