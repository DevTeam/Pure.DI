namespace Pure.DI.Core;

internal sealed class RootDependencyNodeBuilder : IBuilder<MdSetup, IEnumerable<DependencyNode>>
{
    public IEnumerable<DependencyNode> Build(MdSetup setup)
    {
        var id = -1;
        foreach (var root in setup.Roots)
        {
            var rootBinding = new MdBinding(
                id--,
                root.Source,
                setup,
                root.SemanticModel,
                ImmutableArray<MdContract>.Empty,
                ImmutableArray<MdTag>.Empty);
            
            yield return new DependencyNode(0, rootBinding, Root: new DpRoot(root, rootBinding, new Injection(root.RootType.WithNullableAnnotation(NullableAnnotation.NotAnnotated), root.Tag?.Value)));
        }
    }
}