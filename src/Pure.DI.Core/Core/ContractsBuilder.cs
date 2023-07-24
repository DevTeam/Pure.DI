// ReSharper disable ClassNeverInstantiated.Global
namespace Pure.DI.Core;

internal class ContractsBuilder: IBuilder<ContractsBuildContext, ISet<Injection>>
{
    private readonly IMarker _marker;
    private readonly IUnboundTypeConstructor _unboundTypeConstructor;

    public ContractsBuilder(
        IMarker marker,
        IUnboundTypeConstructor unboundTypeConstructor)
    {
        _marker = marker;
        _unboundTypeConstructor = unboundTypeConstructor;
    }

    public ISet<Injection> Build(ContractsBuildContext context, CancellationToken cancellationToken)
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
            if (_marker.IsMarkerBased(contractType))
            {
                contractType = _unboundTypeConstructor.Construct(binding.SemanticModel.Compilation, contractType);
            }
            
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