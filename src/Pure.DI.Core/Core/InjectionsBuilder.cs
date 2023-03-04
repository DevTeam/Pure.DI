namespace Pure.DI.Core;

internal class InjectionsBuilder: IBuilder<MdBinding, ISet<Injection>>
{
    private readonly IMarker _marker;
    private readonly IUnboundTypeConstructor _unboundTypeConstructor;

    public InjectionsBuilder(
        IMarker marker,
        IUnboundTypeConstructor unboundTypeConstructor)
    {
        _marker = marker;
        _unboundTypeConstructor = unboundTypeConstructor;
    }

    public ISet<Injection> Build(MdBinding binding, CancellationToken cancellationToken)
    {
        if (binding.Tags.IsDefault)
        {
            return ImmutableHashSet<Injection>.Empty;
        }

        var injections = new HashSet<Injection>();
        var bindingTags = binding.Tags.Select(i => i.Value).ToHashSet();
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

            if (!contractTags.Any())
            {
                contractTags.Add(default);
            }

            foreach (var tag in contractTags)
            {
                injections.Add(new Injection(contractType, tag) { Contract = contract });
            }
        }

        return injections;
    }
}