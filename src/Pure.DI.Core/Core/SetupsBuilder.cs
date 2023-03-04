namespace Pure.DI.Core;

internal class SetupsBuilder : IBuilder<SyntaxUpdate, IEnumerable<MdSetup>>, IMetadataVisitor
{
    private readonly ILogger<SetupsBuilder> _logger;
    private readonly IMetadataSyntaxWalker _metadataSyntaxWalker;
    private readonly List<MdSetup> _setups = new();
    private readonly ImmutableArray<MdBinding>.Builder _bindingsBuilder = ImmutableArray.CreateBuilder<MdBinding>();
    private readonly ImmutableArray<MdRoot>.Builder _rootsBuilder = ImmutableArray.CreateBuilder<MdRoot>();
    private readonly ImmutableArray<MdDependsOn>.Builder _dependsOnBuilder = ImmutableArray.CreateBuilder<MdDependsOn>();
    private readonly ImmutableArray<MdTypeAttribute>.Builder _typeAttributesBuilder = ImmutableArray.CreateBuilder<MdTypeAttribute>();
    private readonly ImmutableArray<MdTagAttribute>.Builder _tagAttributesBuilder = ImmutableArray.CreateBuilder<MdTagAttribute>();
    private readonly ImmutableArray<MdOrderAttribute>.Builder _orderAttributesBuilder = ImmutableArray.CreateBuilder<MdOrderAttribute>();
    private readonly ImmutableArray<MdContract>.Builder _contractsBuilder = ImmutableArray.CreateBuilder<MdContract>();
    private readonly ImmutableArray<MdTag>.Builder _tagsBuilder = ImmutableArray.CreateBuilder<MdTag>();
    private MdSetup? _setup;
    private MdBinding? _binding;
    private MdDefaultLifetime? _defaultLifetime;

    public SetupsBuilder(
        ILogger<SetupsBuilder> logger,
        IMetadataSyntaxWalker metadataSyntaxWalker)
    {
        _logger = logger;
        _metadataSyntaxWalker = metadataSyntaxWalker;
    }
    
    private MdBinding Binding
    {
        get
        {
            _binding ??= new MdBinding();
            if (_defaultLifetime.HasValue)
            {
                _binding = _binding.Value with { Lifetime = _defaultLifetime.Value.Lifetime };
            }

            return _binding.Value;
        }

        set => _binding = value;
    }

    public IEnumerable<MdSetup> Build(SyntaxUpdate update, CancellationToken cancellationToken)
    {
        _metadataSyntaxWalker.Visit(this, update, cancellationToken);
        return _setups;
    }

    public void VisitSetup(in MdSetup setup)
    {
        FinishSetup();
        _setup = setup;
    }

    public void VisitBinding(in MdBinding binding) => _binding = binding;

    public void VisitContract(in MdContract contract) => _contractsBuilder.Add(contract);

    public void VisitImplementation(in MdImplementation implementation)
    {
        Binding = Binding with
        {
            SemanticModel = implementation.SemanticModel,
            Implementation = implementation,
            Source = implementation.Source
        };

        FinishBinding();
    }

    public void VisitFactory(in MdFactory factory)
    {
        Binding = Binding with
        {
            SemanticModel = factory.SemanticModel,
            Factory = factory,
            Source = factory.Source
        };

        FinishBinding();
    }

    public void VisitResolve(MdResolver resolver)
    {
    }

    public void VisitRoot(in MdRoot root) => _rootsBuilder.Add(root);

    public void VisitArg(in MdArg arg)
    {
        Binding = Binding with
        {
            SemanticModel = arg.SemanticModel,
            Arg = arg,
            Source = arg.Source
        };

        FinishBinding();
    }

    public void VisitDefaultLifetime(in MdDefaultLifetime defaultLifetime) =>
        _defaultLifetime = defaultLifetime;

    public void VisitDependsOn(in MdDependsOn dependsOn) =>
        _dependsOnBuilder.Add(dependsOn);

    public void VisitTypeAttribute(in MdTypeAttribute typeAttribute) =>
        _typeAttributesBuilder.Add(typeAttribute);

    public void VisitTagAttribute(in MdTagAttribute tagAttribute) =>
        _tagAttributesBuilder.Add(tagAttribute);

    public void VisitOrderAttribute(in MdOrderAttribute orderAttribute) =>
        _orderAttributesBuilder.Add(orderAttribute);

    public void VisitLifetime(in MdLifetime lifetime) =>
        Binding = Binding with { Lifetime = lifetime };
    
    public void VisitTag(in MdTag tag) => _tagsBuilder.Add(tag);

    public void VisitAnyTag(in MdAnyTag anyTag) =>
        Binding = Binding with { AnyTag = anyTag };

    public void VisitFinish() => FinishSetup();

    private void FinishBinding()
    {
        if (!_binding.HasValue)
        {
            return;
        }

        _bindingsBuilder.Add(
            _binding.Value with
            {
                Contracts = _contractsBuilder.ToImmutable(),
                Tags = _tagsBuilder.ToImmutable()
            });
        
        _contractsBuilder.Clear();
        _tagsBuilder.Clear();
        _binding = default;
    }

    private void FinishSetup()
    {
        if (_binding.HasValue)
        {
            _logger.CompileError($"Binding {_binding} is not fully defined.", _binding.Value.Source.GetLocation(), LogId.ErrorInvalidMetadata);
            throw HandledException.Shared;
        }

        FinishBinding();
        if (_setup != default)
        {
            _setups.Add(
                _setup with
                {
                    Bindings = _bindingsBuilder.ToImmutable(),
                    Roots = _rootsBuilder.ToImmutable(),
                    DependsOn = _dependsOnBuilder.ToImmutable(),
                    TypeAttributes = _typeAttributesBuilder.ToImmutable(),
                    TagAttributes = _tagAttributesBuilder.ToImmutable(),
                    OrderAttributes = _orderAttributesBuilder.ToImmutable()
                });
            
            _bindingsBuilder.Clear();
            _rootsBuilder.Clear();
            _dependsOnBuilder.Clear();
            _typeAttributesBuilder.Clear();
            _tagsBuilder.Clear();
            _orderAttributesBuilder.Clear();
            _setup = default;
        }
        
        _defaultLifetime = default;
    }
}