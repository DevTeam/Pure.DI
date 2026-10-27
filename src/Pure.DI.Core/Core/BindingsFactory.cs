// ReSharper disable UseCollectionExpression
namespace Pure.DI.Core;

class BindingsFactory(
    Func<IFastBuilder<RewriterContext<MdFactory>, MdFactory>> factoryRewriterFactory,
    ITypes types,
    IMarker marker,
    ILifetimeProvider lifetimeProvider,
    IInjectionComparer injectionComparer)
    : IBindingsFactory
{
    public MdBinding CreateGenericBinding(
        MdSetup setup,
        Injection injection,
        DependencyNode sourceNode,
        ITypeConstructor typeConstructor,
        int bindingId)
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
            Id = bindingId,
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
        ref int bindingId,
        IReadOnlyCollection<MdAccumulator> accumulators,
        bool hasExplicitDefaultValue,
        object? explicitDefaultValue)
    {
        var accumulator = accumulators.First();
        return new MdBinding(
            ++bindingId,
            targetNode.Binding.Source,
            setup,
            targetNode.Binding.SemanticModel,
            ImmutableArray.Create(new MdContract(targetNode.Binding.SemanticModel, accumulator.Source, accumulator.AccumulatorType, ContractKind.Implicit, ImmutableArray<MdTag>.Empty)),
            ImmutableArray<MdTag>.Empty,
            new MdLifetime(targetNode.Binding.SemanticModel, accumulator.Source, Lifetime.PerBlock),
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

    public MdBinding CreateLightweightRootBinding(
        MdSetup setup,
        MdRoot root)
    {
        var lifetime = new MdLifetime(
            root.SemanticModel,
            root.Source,
            Lifetime.Transient);

        var implementation = new MdImplementation(
            root.SemanticModel,
            root.Source,
            root.RootType);

        var contract = new MdContract(
            root.SemanticModel,
            root.Source,
            root.RootType,
            ContractKind.Explicit,
            ImmutableArray<MdTag>.Empty);

        return new MdBinding(
            root.OriginalId,
            root.Source,
            setup,
            root.SemanticModel,
            ImmutableArray.Create(contract),
            ImmutableArray<MdTag>.Empty,
            Lifetime: lifetime,
            Implementation: implementation);
    }

    public MdBinding CreateAutoBinding(
        MdSetup setup,
        DependencyNode targetNode,
        Injection injection,
        ITypeConstructor typeConstructor,
        int bindingId)
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
        var actualLifetime = lifetimeProvider.GetActualLifetime(setup.DefaultLifetimes, null, sourceType, ImmutableArray<MdTag>.Empty, newContracts, false);

        return new MdBinding(
            bindingId,
            targetNode.Binding.Source,
            setup,
            semanticModel,
            newContracts,
            newTags,
            actualLifetime,
            Implementation: new MdImplementation(semanticModel, setup.Source, sourceType),
            TypeConstructor: typeConstructor);
    }

    public MdBinding CreateConstructBinding(
        MdSetup setup,
        DependencyNode targetNode,
        Injection injection,
        ITypeSymbol elementType,
        Lifetime lifetime,
        ITypeConstructor typeConstructor,
        int bindingId,
        MdConstructKind constructKind,
        object? tag = null,
        bool hasExplicitDefaultValue = false,
        object? explicitDefaultValue = null,
        object? state = null,
        Func<MdBinding, MdContract, MdContract?>? selectElement = null)
    {
        var dependencyContracts = new List<MdContract>();
        var contracts = new HashSet<Injection>(injectionComparer);
        var originalIds = new HashSet<int>();
        if (constructKind is MdConstructKind.Array or MdConstructKind.AsyncEnumerable or MdConstructKind.Enumerable or MdConstructKind.Span)
        {
            foreach (var (nestedBinding, contract) in GetElements(setup, targetNode, elementType, typeConstructor, true))
            {
                var elementContract = contract;
                if (selectElement is not null)
                {
                    if (selectElement(nestedBinding, contract) is not {} selectedContract)
                    {
                        continue;
                    }

                    elementContract = selectedContract;
                }

                // The element is resolved by its first tag, so only that tag is taken when the element was selected by its tags.
                var tags = selectElement is not null ? elementContract.Tags.Take(1).ToImmutableArray() : elementContract.Tags;
                var isDuplicate = false;
                if (constructKind is MdConstructKind.Enumerable or MdConstructKind.Array or MdConstructKind.Span or MdConstructKind.AsyncEnumerable)
                {
                    foreach (var mdTag in tags.DefaultIfEmpty(new MdTag(0, null)))
                    {
                        if (!contracts.Add(new Injection(InjectionKind.Construct, RefKind.None, elementType, mdTag, injection.Locations)))
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
                dependencyContracts.Add(elementContract);
            }
        }

        var newTags = tag is not null
            ? ImmutableArray.Create(new MdTag(0, tag))
            : ImmutableArray<MdTag>.Empty;
        var newContracts = ImmutableArray.Create(new MdContract(targetNode.Binding.SemanticModel, targetNode.Binding.Source, injection.Type, ContractKind.Implicit, newTags));
        var newBinding = new MdBinding(
            bindingId,
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

    public MdBinding CreateImplicitConversionBinding(
        MdSetup setup,
        DependencyNode targetNode,
        Injection injection,
        DependencyNode sourceNode,
        ITypeSymbol sourceType,
        ITypeConstructor typeConstructor,
        int bindingId)
    {
        var semanticModel = targetNode.Binding.SemanticModel;
        var tags = injection.Tag is not null
            ? ImmutableArray.Create(new MdTag(0, injection.Tag))
            : ImmutableArray<MdTag>.Empty;

        return new MdBinding(
            bindingId,
            targetNode.Binding.Source,
            setup,
            semanticModel,
            ImmutableArray.Create(new MdContract(semanticModel, targetNode.Binding.Source, injection.Type, ContractKind.Implicit, tags)),
            ImmutableArray<MdTag>.Empty,
            new MdLifetime(semanticModel, targetNode.Binding.Source, Lifetime.Transient),
            Construct: new MdConstruct(
                semanticModel,
                targetNode.Binding.Source,
                injection.Type,
                sourceType,
                MdConstructKind.ImplicitConversion,
                ImmutableArray.Create(new MdContract(semanticModel, targetNode.Binding.Source, sourceType, ContractKind.Implicit, tags)),
                false,
                null),
            TypeConstructor: typeConstructor,
            OriginalIds: ImmutableArray.Create(sourceNode.Binding.Id));
    }

    public IEnumerable<(MdBinding Binding, MdContract Contract)> GetElementContracts(
        MdSetup setup,
        DependencyNode targetNode,
        ITypeSymbol elementType,
        ITypeConstructor typeConstructor) =>
        GetElements(setup, targetNode, elementType, typeConstructor, false);

    private IEnumerable<(MdBinding Binding, MdContract Contract)> GetElements(
        MdSetup setup,
        DependencyNode targetNode,
        ITypeSymbol elementType,
        ITypeConstructor typeConstructor,
        bool excludeTarget)
    {
        foreach (var nestedBinding in setup.Bindings.Where(i => !excludeTarget || i != targetNode.Binding))
        {
            using var matchedContracts = GetMatchedMdContracts(setup, elementType, nestedBinding, typeConstructor).GetEnumerator();
            if (!matchedContracts.MoveNext())
            {
                continue;
            }

            var tags = matchedContracts.Current.Tags
                .Concat(nestedBinding.Tags)
                .Select((i, position) => i with { Position = position })
                .ToImmutableArray();

            yield return (nestedBinding, new MdContract(targetNode.Binding.SemanticModel, targetNode.Binding.Source, elementType, ContractKind.Implicit, tags));
        }
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

            if (types.TypeEquals(contractType, elementType)
                || contractType is not null
                && types.IsImplicitUnionConversion(contract.SemanticModel.Compilation, contractType, elementType))
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
