// ReSharper disable InvertIf
// ReSharper disable ClassNeverInstantiated.Global
namespace Pure.DI.Core;

internal class BindingBuilder(
    [Tag("UniqueTags")] IIdGenerator idGenerator,
    IBaseSymbolsProvider baseSymbolsProvider)
    : IBindingBuilder
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
        var type = _implementation?.Type ?? _factory?.Type ??_arg?.Type;
        var contractsSource = _implementation?.Source ?? _factory?.Source;
        try
        {
            if (_semanticModel is { } semanticModel
                && _source is { } source)
            {
                var autoContracts = _contracts.Where(i => i.ContractType == null).ToList();
                if (autoContracts.Count > 0)
                {
                    foreach (var contract in autoContracts)
                    {
                        _contracts.Remove(contract);
                    }
                    
                    if (type is not null && contractsSource is not null)
                    {
                        var baseSymbols = Enumerable.Empty<ITypeSymbol>();
                        if (type is { SpecialType: SpecialType.None, TypeKind: TypeKind.Class, IsAbstract: false })
                        {
                            baseSymbols = baseSymbolsProvider
                                .GetBaseSymbols(type, (i, deepness) => deepness switch
                                {
                                    0 => true,
                                    1 when 
                                        type.TypeKind != TypeKind.Interface
                                        && !type.IsAbstract
                                        && (i.TypeKind == TypeKind.Interface || i.IsAbstract)
                                        && i.SpecialType == SpecialType.None 
                                        => true,
                                    _ => false
                                }, 1);
                        }
                        
                        var contracts = new HashSet<ITypeSymbol>(baseSymbols, SymbolEqualityComparer.Default)
                        {
                            type
                        };
                        
                        var tags = autoContracts
                            .SelectMany(i => i.Tags)
                            .GroupBy(i => i.Value)
                            .Select(i => i.First())
                            .ToImmutableArray();

                        foreach (var contract in contracts)
                        {
                            _contracts.Add(
                                new MdContract(
                                    semanticModel,
                                    contractsSource,
                                    contract,
                                    ContractKind.Implicit,
                                    tags));
                        }
                    }
                }

                var id = new Lazy<int>(idGenerator.Generate);
                return new MdBinding(
                    0,
                    source,
                    setup,
                    semanticModel,
                    _contracts.Select(i => i with { Tags = i.Tags.Select(tag => BuildTag(tag, type, id)).ToImmutableArray()}).ToImmutableArray(),
                    _tags.Select(tag => BuildTag(tag, type, id)).ToImmutableArray(),
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

    private static MdTag BuildTag(MdTag tag, ITypeSymbol? type, Lazy<int> id)
    {
        if (type is null || tag.Value is null)
        {
            return tag;
        }

        if (tag.Value is Tag tagVal)
        {
            // ReSharper disable once SwitchStatementHandlesSomeKnownEnumValuesWithDefault
            if (tagVal == Tag.Type)
            {
                return MdTag.CreateTypeTag(tag, type);
            }
            
            if (tagVal == Tag.Unique)
            {
                return MdTag.CreateUniqueTag(tag, id.Value);
            }
        }

        return tag;
    }
}