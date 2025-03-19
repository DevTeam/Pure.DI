namespace Pure.DI.Core;

class NodesFactory(IEnumerable<IBuilder<DependencyNodeBuildContext, IEnumerable<DependencyNode>>> dependencyNodeBuilders)
    : INodesFactory
{
    public IEnumerable<DependencyNode> CreateNodes(MdSetup setup, ITypeConstructor typeConstructor, MdBinding binding)
    {
        var newSetup = setup with { Roots = ImmutableArray<MdRoot>.Empty, Bindings = ImmutableArray.Create(binding) };
        var ctx = new DependencyNodeBuildContext(newSetup, typeConstructor);
        return dependencyNodeBuilders.SelectMany(builder => builder.Build(ctx));
    }
}