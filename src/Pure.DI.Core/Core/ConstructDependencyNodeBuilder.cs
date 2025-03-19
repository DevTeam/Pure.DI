// ReSharper disable ForeachCanBeConvertedToQueryUsingAnotherGetEnumerator
// ReSharper disable ClassNeverInstantiated.Global

namespace Pure.DI.Core;

sealed class ConstructDependencyNodeBuilder : IBuilder<DependencyNodeBuildContext, IEnumerable<DependencyNode>>
{
    public IEnumerable<DependencyNode> Build(DependencyNodeBuildContext ctx)
    {
        var setup = ctx.Setup;
        foreach (var binding in setup.Bindings)
        {
            if (binding.Construct is not {} construct)
            {
                continue;
            }

            var injections = new List<Injection>();
            foreach (var contract in construct.Dependencies)
            {
                if (contract.ContractType is null)
                {
                    continue;
                }

                var tag = contract.Tags.Select(i => i.Value).FirstOrDefault();
                injections.Add(
                    new Injection(
                        InjectionKind.Contract,
                        ctx.TypeConstructor.Construct(setup, contract.ContractType.WithNullableAnnotation(NullableAnnotation.NotAnnotated)),
                        tag));
            }

            yield return new DependencyNode(0, binding, ctx.TypeConstructor, Construct: new DpConstruct(construct, binding, injections.ToImmutableArray()));
        }
    }
}