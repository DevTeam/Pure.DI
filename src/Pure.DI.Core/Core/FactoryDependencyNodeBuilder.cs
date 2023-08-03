// ReSharper disable ForeachCanBeConvertedToQueryUsingAnotherGetEnumerator
namespace Pure.DI.Core;

internal sealed class FactoryDependencyNodeBuilder : IBuilder<MdSetup, IEnumerable<DependencyNode>>
{
    public IEnumerable<DependencyNode> Build(MdSetup setup)
    {
        foreach (var binding in setup.Bindings)
        {
            if (binding.Factory is not {} factory)
            {
                continue;
            }

            var injections = new List<Injection>(factory.Resolvers.Length);
            foreach (var resolver in factory.Resolvers)
            {
                injections.Add(new Injection(resolver.ContractType.WithNullableAnnotation(NullableAnnotation.NotAnnotated), resolver.Tag?.Value));
            }

            yield return new DependencyNode(0, Factory: new DpFactory(factory, binding, injections.ToImmutableArray()));
        }
    }
}