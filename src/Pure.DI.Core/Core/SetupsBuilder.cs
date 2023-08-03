// ReSharper disable ClassNeverInstantiated.Global
namespace Pure.DI.Core;

internal sealed class SetupsBuilder : IBuilder<SyntaxUpdate, IEnumerable<MdSetup>>, IMetadataVisitor
{
    private readonly ILogger<SetupsBuilder> _logger;
    private readonly Func<IMetadataSyntaxWalker> _metadataSyntaxWalkerFactory;
    private readonly ICache<ImmutableArray<byte>, bool> _setupCache;
    private readonly List<MdSetup> _setups = new();
    private readonly List<MdBinding> _bindings = new();
    private readonly List<MdRoot> _roots = new();
    private readonly List<MdDependsOn> _dependsOn = new();
    private readonly List<MdTypeAttribute> _typeAttributes = new();
    private readonly List<MdTagAttribute> _tagAttributes = new();
    private readonly List<MdOrdinalAttribute> _ordinalAttributes = new();
    private readonly List<MdContract> _contracts = new();
    private readonly List<MdTag> _tags = new();
    private readonly List<MdUsingDirectives> _usingDirectives = new();
    private MdSetup? _setup;
    private MdBinding? _binding;
    private MdDefaultLifetime? _defaultLifetime;

    public SetupsBuilder(
        ILogger<SetupsBuilder> logger,
        Func<IMetadataSyntaxWalker> metadataSyntaxWalkerFactory,
        ICache<ImmutableArray<byte>, bool> setupCache)
    {
        _logger = logger;
        _metadataSyntaxWalkerFactory = metadataSyntaxWalkerFactory;
        _setupCache = setupCache;
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

    public IEnumerable<MdSetup> Build(SyntaxUpdate update)
    {
        var checkSum = update.Node.SyntaxTree.GetText().GetChecksum();
        if (!_setupCache.Get(checkSum))
        {
            return Array.Empty<MdSetup>();
        }
        
        _metadataSyntaxWalkerFactory().Visit(this, update);
        if (!_setups.Any())
        {
            _setupCache.Set(checkSum, false);
        }
        
        return _setups;
    }

    public void VisitSetup(in MdSetup setup)
    {
        FinishSetup();
        _setup = setup;
    }

    public void VisitUsingDirectives(in MdUsingDirectives usingDirectives) =>
        _usingDirectives.Add(usingDirectives);

    public void VisitBinding(in MdBinding binding) => _binding = binding;

    public void VisitContract(in MdContract contract) => _contracts.Add(contract);

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

    public void VisitRoot(in MdRoot root) => _roots.Add(root);

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
        _dependsOn.Add(dependsOn);

    public void VisitTypeAttribute(in MdTypeAttribute typeAttribute) =>
        _typeAttributes.Add(typeAttribute);

    public void VisitTagAttribute(in MdTagAttribute tagAttribute) =>
        _tagAttributes.Add(tagAttribute);

    public void VisitOrdinalAttribute(in MdOrdinalAttribute ordinalAttribute) =>
        _ordinalAttributes.Add(ordinalAttribute);

    public void VisitLifetime(in MdLifetime lifetime) =>
        Binding = Binding with { Lifetime = lifetime };
    
    public void VisitTag(in MdTag tag) => _tags.Add(tag);

    public void VisitFinish() => FinishSetup();

    private void FinishBinding()
    {
        if (!_binding.HasValue)
        {
            return;
        }

        _bindings.Add(
            _binding.Value with
            {
                Contracts = _contracts.ToImmutableArray(),
                Tags = _tags.ToImmutableArray()
            });
        
        _contracts.Clear();
        _tags.Clear();
        _binding = default;
    }

    private void FinishSetup()
    {
        if (_setup == default)
        {
            return;
        }

        if (_binding.HasValue)
        {
            throw new CompileErrorException($"Binding {_binding} is not fully defined.", _binding.Value.Source.GetLocation(), LogId.ErrorInvalidMetadata);
        }

        _setups.Add(
            _setup with
            {
                Bindings = _bindings.Select(i => i with { SourceSetup = _setup }).ToImmutableArray(),
                Roots = _roots.ToImmutableArray(),
                DependsOn = _dependsOn.ToImmutableArray(),
                TypeAttributes = _typeAttributes.ToImmutableArray(),
                TagAttributes = _tagAttributes.ToImmutableArray(),
                OrdinalAttributes = _ordinalAttributes.ToImmutableArray(),
                UsingDirectives = _usingDirectives.ToImmutableArray()
        });
            
        _bindings.Clear();
        _roots.Clear();
        _dependsOn.Clear();
        _typeAttributes.Clear();
        _tags.Clear();
        _ordinalAttributes.Clear();
        _usingDirectives.Clear();
        _setup = default;
    }
}