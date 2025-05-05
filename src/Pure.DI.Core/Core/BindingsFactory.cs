namespace Pure.DI.Core;

class BindingsFactory(
    Func<IBuilder<RewriterContext<MdFactory>, MdFactory>> factoryRewriterFactory,
    ITypes types,
    IMarker marker)
    : IBindingsFactory
{
    public MdBinding CreateGenericBinding(
        MdSetup setup,
        Injection injection,
        DependencyNode sourceNode,
        ITypeConstructor typeConstructor,
        int newId)
    {
        var newContracts = sourceNode.Binding.Contracts
            .Where(contract => contract.ContractType is not null)
            .Select(contract => contract with
            {
                ContractType = typeConstructor.Construct(setup, contract.ContractType!),
                Tags = contract.Tags.Select(tag => CreateTag(injection, tag)).Where(tag => tag.HasValue).Select(tag => tag!.Value).ToImmutableArray()
            })
            .ToImmutableArray();

        return sourceNode.Binding with
        {
            Id = newId,
            OriginalIds = ImmutableArray.Create(sourceNode.Binding.Id),
            TypeConstructor = typeConstructor,
            Contracts = newContracts,
            Implementation = sourceNode.Binding.Implementation.HasValue
                ? sourceNode.Binding.Implementation.Value with
                {
                    Type = typeConstructor.Construct(setup, sourceNode.Binding.Implementation.Value.Type)
                }
                : null,
            Factory = sourceNode.Binding.Factory.HasValue
                ? factoryRewriterFactory().Build(
                    new RewriterContext<MdFactory>(setup, typeConstructor, injection, sourceNode.Binding.Factory.Value))
                : null,
            Arg = sourceNode.Binding.Arg.HasValue
                ? sourceNode.Binding.Arg.Value with
                {
                    Type = typeConstructor.Construct(setup, sourceNode.Binding.Arg.Value.Type)
                }
                : null
        };
    }

    public MdBinding CreateAccumulatorBinding(
        MdSetup setup,
        DependencyNode targetNode,
        ref int newId,
        IReadOnlyCollection<MdAccumulator> accumulators,
        bool hasExplicitDefaultValue,
        object? explicitDefaultValue)
    {
        var accumulator = accumulators.First();
        return new MdBinding(
            ++newId,
            targetNode.Binding.Source,
            setup,
            targetNode.Binding.SemanticModel,
            ImmutableArray.Create(new MdContract(targetNode.Binding.SemanticModel, accumulator.Source, accumulator.AccumulatorType, ContractKind.Implicit, ImmutableArray<MdTag>.Empty)),
            ImmutableArray<MdTag>.Empty,
            new MdLifetime(targetNode.Binding.SemanticModel, accumulator.Source, Lifetime.Transient),
            null,
            null,
            null,
            new MdConstruct(
                targetNode.Binding.SemanticModel,
                targetNode.Binding.Source,
                accumulator.AccumulatorType,
                accumulator.Type,
                MdConstructKind.Accumulator,
                ImmutableArray<MdContract>.Empty,
                hasExplicitDefaultValue,
                explicitDefaultValue,
                accumulators));
    }

    public MdBinding CreateAutoBinding(
        MdSetup setup,
        DependencyNode targetNode,
        Injection injection,
        ITypeConstructor typeConstructor,
        int newId)
    {
        var semanticModel = targetNode.Binding.SemanticModel;
        var sourceType = injection.Type;
        if (marker.IsMarkerBased(setup, injection.Type))
        {
            typeConstructor.TryBind(setup, injection.Type, injection.Type);
            sourceType = typeConstructor.Construct(setup, injection.Type);
        }

        var newTags = injection.Tag is not null
            ? ImmutableArray.Create(new MdTag(0, injection.Tag))
            : ImmutableArray<MdTag>.Empty;

        var newContracts = ImmutableArray.Create(new MdContract(semanticModel, setup.Source, sourceType, ContractKind.Implicit, ImmutableArray<MdTag>.Empty));
        var newBinding = new MdBinding(
            newId,
            targetNode.Binding.Source,
            setup,
            semanticModel,
            newContracts,
            newTags,
            new MdLifetime(semanticModel, setup.Source, Lifetime.Transient),
            new MdImplementation(semanticModel, setup.Source, sourceType),
            TypeConstructor: typeConstructor);
        return newBinding;
    }

    public MdBinding CreateConstructBinding(
        MdSetup setup,
        DependencyNode targetNode,
        Injection injection,
        ITypeSymbol elementType,
        Lifetime lifetime,
        ITypeConstructor typeConstructor,
        int newId,
        MdConstructKind constructKind,
        object? tag = null,
        bool hasExplicitDefaultValue = false,
        object? explicitDefaultValue = null,
        object? state = null)
    {
        elementType = elementType.WithNullableAnnotation(NullableAnnotation.NotAnnotated);
        var dependencyContracts = new List<MdContract>();
        var contracts = new HashSet<Injection>();
        var originalIds = new HashSet<int>();
        if (constructKind is MdConstructKind.Array or MdConstructKind.AsyncEnumerable or MdConstructKind.Enumerable or MdConstructKind.Span)
        {
            foreach (var nestedBinding in setup.Bindings.Where(i => i != targetNode.Binding))
            {
                var matchedContracts = GetMatchedMdContracts(setup, elementType, nestedBinding, typeConstructor).ToList();
                if (matchedContracts.Count == 0)
                {
                    continue;
                }

                var tags = matchedContracts.First().Tags
                    .Concat(nestedBinding.Tags)
                    .Select((i, position) => i with { Position = position })
                    .ToImmutableArray();

                var isDuplicate = false;
                if (constructKind is MdConstructKind.Enumerable or MdConstructKind.Array or MdConstructKind.Span or MdConstructKind.AsyncEnumerable)
                {
                    foreach (var mdTag in tags.DefaultIfEmpty(new MdTag(0, null)))
                    {
                        if (!contracts.Add(new Injection(InjectionKind.Construct, elementType, mdTag)))
                        {
                            isDuplicate = true;
                        }
                    }
                }

                if (isDuplicate)
                {
                    continue;
                }

                originalIds.Add(nestedBinding.Id);
                dependencyContracts.Add(new MdContract(targetNode.Binding.SemanticModel, targetNode.Binding.Source, elementType, ContractKind.Implicit, tags));
            }
        }

        var newTags = tag is not null
            ? ImmutableArray.Create(new MdTag(0, tag))
            : ImmutableArray<MdTag>.Empty;
        var newContracts = ImmutableArray.Create(new MdContract(targetNode.Binding.SemanticModel, targetNode.Binding.Source, injection.Type, ContractKind.Implicit, newTags));
        var newBinding = new MdBinding(
            newId,
            targetNode.Binding.Source,
            setup,
            targetNode.Binding.SemanticModel,
            newContracts,
            ImmutableArray<MdTag>.Empty,
            new MdLifetime(targetNode.Binding.SemanticModel, targetNode.Binding.Source, lifetime),
            null,
            null,
            null,
            new MdConstruct(
                targetNode.Binding.SemanticModel,
                targetNode.Binding.Source,
                injection.Type,
                elementType,
                constructKind,
                dependencyContracts.ToImmutableArray(),
                hasExplicitDefaultValue,
                explicitDefaultValue,
                state),
            OriginalIds: originalIds.ToImmutableArray());

        return newBinding;
    }

    private IEnumerable<MdContract> GetMatchedMdContracts(MdSetup setup, ITypeSymbol elementType, MdBinding nestedBinding, ITypeConstructor typeConstructor)
    {
        foreach (var contract in nestedBinding.Contracts)
        {
            var contractType = contract.ContractType;
            if (contractType is not null && marker.IsMarkerBased(setup, contractType))
            {
                if (typeConstructor.TryBind(setup, contractType, elementType))
                {
                    contractType = typeConstructor.Construct(setup, contractType);
                }
            }

            if (types.TypeEquals(contractType, elementType))
            {
                yield return contract;
            }
        }
    }

    private static MdTag? CreateTag(in Injection injection, in MdTag? tag)
    {
        if (!tag.HasValue || !ReferenceEquals(tag.Value.Value, MdTag.ContextTag))
        {
            return tag;
        }

        return new MdTag(0, injection.Tag);
    }
}