// ReSharper disable ForeachCanBeConvertedToQueryUsingAnotherGetEnumerator
namespace Pure.DI.Core;

internal sealed class ConstructDependencyNodeBuilder : IBuilder<MdSetup, IEnumerable<DependencyNode>>
{
    public IEnumerable<DependencyNode> Build(MdSetup setup)
    {
        foreach (var binding in setup.Bindings)
        {
            if (binding.Construct is not {} construct)
            {
                continue;
            }

            var injections = new List<Injection>();
            foreach (var contract in construct.Dependencies)
            {
                var tag = contract.Tags.Select(i => i.Value).FirstOrDefault();
                injections.Add(new Injection(contract.ContractType.WithNullableAnnotation(NullableAnnotation.NotAnnotated), tag));
            }
            
            yield return new DependencyNode(0, Construct: new DpConstruct(construct, binding, injections.ToImmutableArray()));
        }
    }
}