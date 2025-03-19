// ReSharper disable ClassNeverInstantiated.Global

namespace Pure.DI.Core;

sealed class RootDependencyNodeBuilder : IBuilder<DependencyNodeBuildContext, IEnumerable<DependencyNode>>
{
    public IEnumerable<DependencyNode> Build(DependencyNodeBuildContext ctx)
    {
        var id = -1;
        var setup = ctx.Setup;
        foreach (var root in ctx.Setup.Roots)
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
                        root.RootType.WithNullableAnnotation(NullableAnnotation.NotAnnotated),
                        root.Tag?.Value)));
        }
    }
}