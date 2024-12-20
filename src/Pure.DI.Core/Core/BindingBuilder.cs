// ReSharper disable InvertIf
// ReSharper disable ClassNeverInstantiated.Global

namespace Pure.DI.Core;

using static Tag;

internal class BindingBuilder(
    [Tag(Tag.UniqueTag)] IIdGenerator idGenerator,
    IBaseSymbolsProvider baseSymbolsProvider)
    : IBindingBuilder
{
    private readonly LinkedList<MdDefaultLifetime> _defaultLifetimes = [];
    private SemanticModel? _semanticModel;
    private SyntaxNode? _source;
    private MdLifetime? _lifetime;
    private MdImplementation? _implementation;
    private MdFactory? _factory;
    private MdArg? _arg;
    private readonly List<MdContract> _contracts = [];
    private readonly List<MdTag> _tags = [];

    public void AddDefaultLifetime(MdDefaultLifetime defaultLifetime) =>
        _defaultLifetimes.AddFirst(defaultLifetime);

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
        var implementationType = _implementation?.Type ?? _factory?.Type ?? _arg?.Type;
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

                    if (implementationType is not null && contractsSource is not null)
                    {
                        var baseSymbols = Enumerable.Empty<ITypeSymbol>();
                        if (implementationType is { SpecialType: Microsoft.CodeAnalysis.SpecialType.None, TypeKind: TypeKind.Class, IsAbstract: false })
                        {
                            baseSymbols = baseSymbolsProvider
                                .GetBaseSymbols(implementationType, (i, deepness) => deepness switch
                                {
                                    0 => true,
                                    1 when
                                        implementationType.TypeKind != TypeKind.Interface
                                        && !implementationType.IsAbstract
                                        && (i.TypeKind == TypeKind.Interface || i.IsAbstract)
                                        && i.SpecialType == Microsoft.CodeAnalysis.SpecialType.None
                                        => true,
                                    _ => false
                                }, 1);
                        }

                        var contracts = new HashSet<ITypeSymbol>(baseSymbols, SymbolEqualityComparer.Default)
                        {
                            implementationType
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
                var implementationTags = _tags.Select(tag => BuildTag(tag, implementationType, id)).ToImmutableArray();
                return new MdBinding(
                    0,
                    source,
                    setup,
                    semanticModel,
                    _contracts.Select(i => i with { Tags = i.Tags.Select(tag => BuildTag(tag, implementationType, id)).ToImmutableArray() }).ToImmutableArray(),
                    implementationTags,
                    GetLifetime(implementationType, implementationTags),
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
            _source = null;
            _semanticModel = null;
            _lifetime = null;
            _implementation = null;
            _factory = null;
            _arg = null;
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
            if (tagVal == Type)
            {
                return MdTag.CreateTypeTag(tag, type);
            }

            if (tagVal == Unique)
            {
                return MdTag.CreateUniqueTag(tag, id.Value);
            }
        }

        return tag;
    }

    private MdLifetime? GetLifetime(ITypeSymbol? implementationType, ImmutableArray<MdTag> implementationTags)
    {
        if (_lifetime.HasValue)
        {
            return _lifetime.Value;
        }

        if (implementationType is not null)
        {
            foreach (var defaultLifetime in _defaultLifetimes.Where(i => i.Type is not null))
            {
                var tags = defaultLifetime.Tags.IsDefaultOrEmpty
                    ? ImmutableHashSet<MdTag>.Empty
                    : defaultLifetime.Tags.ToImmutableHashSet();

                var baseSymbols = baseSymbolsProvider.GetBaseSymbols(implementationType, (i, _) =>
                {
                    if (!tags.IsEmpty)
                    {
                        var bindingTags = implementationTags.ToImmutableHashSet();
                        var contractTags = _contracts.FirstOrDefault(j => SymbolEqualityComparer.Default.Equals(j.ContractType, i)).Tags;
                        if (!contractTags.IsDefaultOrEmpty)
                        {
                            bindingTags = bindingTags.Union(contractTags);
                        }

                        if (bindingTags.Intersect(tags).IsEmpty)
                        {
                            return false;
                        }
                    }

                    return SymbolEqualityComparer.Default.Equals(defaultLifetime.Type, i);
                });

                if (baseSymbols.Any())
                {
                    return defaultLifetime.Lifetime;
                }
            }
        }

        return _defaultLifetimes.FirstOrDefault(i => i.Type is null).Lifetime;
    }
}