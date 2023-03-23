namespace Pure.DI.Core;

internal class ConstructDependencyNodeBuilder : IBuilder<MdSetup, IEnumerable<DependencyNode>>
{
    public IEnumerable<DependencyNode> Build(MdSetup setup, CancellationToken cancellationToken)
    {
        foreach (var binding in setup.Bindings)
        {
            if (binding.Construct is not {} construct)
            {
                continue;
            }

            var injectionsBuilder = ImmutableArray.CreateBuilder<Injection>();
            foreach (var contract in construct.Dependencies)
            {
                var tag = contract.Tags.Select(i => i.Value).FirstOrDefault();
                injectionsBuilder.Add(new Injection(contract.ContractType, tag));
            }
            
            yield return new DependencyNode(0, Construct: new DpConstruct(construct, binding, injectionsBuilder.ToImmutable()));
        }
    }
}