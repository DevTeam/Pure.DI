namespace Pure.DI.Core;

internal class FactoryDependencyNodeBuilder : IBuilder<MdSetup, IEnumerable<DependencyNode>>
{
    public IEnumerable<DependencyNode> Build(MdSetup setup, CancellationToken cancellationToken)
    {
        foreach (var binding in setup.Bindings)
        {
            if (binding.Factory is not {} factory)
            {
                continue;
            }

            var injectionsBuilder = ImmutableArray.CreateBuilder<Injection>(factory.Resolvers.Length);
            foreach (var resolver in factory.Resolvers)
            {
                injectionsBuilder.Add(new Injection(resolver.ContractType, resolver.Tag?.Value));
            }

            yield return new DependencyNode(0, Factory: new DpFactory(factory, binding, injectionsBuilder.SafeMoveToImmutable()));
        }
    }
}