namespace Pure.DI.Core;

internal class BindingBuilder
{
    private MdDefaultLifetime? _defaultLifetime;
    private SemanticModel? _semanticModel;
    private SyntaxNode? _source;
    private MdLifetime? _lifetime;
    private MdImplementation? _implementation;
    private MdFactory? _factory;
    private MdArg? _arg;
    private readonly List<MdContract> _contracts = [];
    private readonly List<MdTag> _tags = [];

    public MdDefaultLifetime DefaultLifetime
    {
        set => _defaultLifetime = value;
    }

    public MdLifetime Lifetime
    {
        set => _lifetime = value;
    }
    
    public MdImplementation Implementation
    {
        set
        {
            _source = value.Source;
            _semanticModel = value.SemanticModel;
            _implementation = value;
        }
    }

    public MdFactory Factory
    {
        set
        {
            _source = value.Source;
            _semanticModel = value.SemanticModel;
            _factory = value;
        }
    }

    public MdArg Arg
    {
        set
        {
            _source = value.Source;
            _semanticModel = value.SemanticModel;
            _arg = value;
        }
    }

    public void AddContract(in MdContract contract) => 
        _contracts.Add(contract);
    
    public void AddTag(in MdTag tag) =>
        _tags.Add(tag);

    public MdBinding Build(MdSetup setup)
    {
        try
        {
            if (_semanticModel is { } semanticModel
                && _source is { } source)
            {
                return new MdBinding(
                    0,
                    source,
                    setup,
                    semanticModel,
                    _contracts.ToImmutableArray(),
                    _tags.ToImmutableArray(),
                    _lifetime ?? _defaultLifetime?.Lifetime,
                    _implementation,
                    _factory,
                    _arg);
            }

            throw new CompileErrorException("The binding is defined incorrectly.", setup.Source.GetLocation(), LogId.ErrorInvalidMetadata);
        }
        finally
        {
            _contracts.Clear();
            _tags.Clear();
            _source = default;
            _semanticModel = default;
            _lifetime = default;
            _implementation = default;
            _factory = default;
            _arg = default;
        }
    }
}