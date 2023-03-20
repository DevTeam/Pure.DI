namespace Pure.DI.Core;

internal class RootDependencyNodeBuilder : IBuilder<MdSetup, IEnumerable<DependencyNode>>
{
    public IEnumerable<DependencyNode> Build(MdSetup setup, CancellationToken cancellationToken)
    {
        var id = -1;
        foreach (var root in setup.Roots)
        {
            var rootBinding = new MdBinding(
                id--,
                root.Source,
                root.SemanticModel,
                ImmutableArray<MdContract>.Empty,
                ImmutableArray<MdTag>.Empty);
            
            yield return new DependencyNode(Root: new DpRoot(root, rootBinding, new Injection(root.RootType, root.Tag?.Value)));
        }
    }
}