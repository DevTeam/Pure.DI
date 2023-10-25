// ReSharper disable ClassNeverInstantiated.Global
namespace Pure.DI.Core;

internal sealed class ContractsBuilder: IBuilder<ContractsBuildContext, ISet<Injection>>
{
    public ISet<Injection> Build(ContractsBuildContext context)
    {
        var binding = context.Binding;
        if (binding.Tags.IsDefault)
        {
            return ImmutableHashSet<Injection>.Empty;
        }

        var hasContextTag = binding.Factory is { HasContextTag: true };
        var contracts = new HashSet<Injection>();
        var bindingTags = new HashSet<object?>(binding.Tags.Select(i => i.Value));
        foreach (var contract in binding.Contracts)
        {
            var contractType = contract.ContractType;
            var contractTags = new HashSet<object?>(bindingTags);
            foreach (var tag in contract.Tags)
            {
                contractTags.Add(tag.Value);
            }
            
            if (hasContextTag)
            {
                contractTags.Add(context.ContextTag);
            }

            if (!contractTags.Any())
            {
                contractTags.Add(default);
            }

            foreach (var tag in contractTags)
            {
                contracts.Add(new Injection(contractType, tag));
            }
        }

        return contracts;
    }
}