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
                var tag = resolver.Tag?.Value;
                if (ReferenceEquals(tag, MdTag.ContextTag))
                {
                    tag = new CompositeTag(binding.Tags.Concat(binding.Contracts.SelectMany(i => i.Tags)).Select(i => i.Value).ToImmutableHashSet());
                }
                
                injectionsBuilder.Add(new Injection(resolver.ContractType, tag));
            }

            yield return new DependencyNode(Factory: new DpFactory(factory, binding, injectionsBuilder.SafeMoveToImmutable()));
        }
    }
}