// ReSharper disable InvertIf
// ReSharper disable ClassNeverInstantiated.Global

// ReSharper disable UseCollectionExpression
#pragma warning disable RS1024 // Pure.DI intentionally uses ITypeSymbolComparer to control nullable-reference contract equality.

namespace Pure.DI.Core;

using static Tag;

sealed class BindingBuilder(
    [Tag(UniqueTagIdGenerator)] IIdGenerator idGenerator,
    [Tag(SpecialBindingIdGenerator)] IIdGenerator specialBindingIdGenerator,
    IBaseSymbolsProvider baseSymbolsProvider,
    ILocationProvider locationProvider,
    ILifetimeProvider lifetimeProvider,
    ITypeSymbolComparer typeSymbolComparer,
    ITypes types)
    : IBindingBuilder
{
    private readonly List<MdContract> _contracts = [];
    private readonly List<MdDefaultLifetime> _defaultLifetimes = [];
    private readonly List<MdTag> _tags = [];
    private MdArg? _arg;
    private MdFactory? _factory;
    private MdImplementation? _implementation;
    private MdLifetime? _lifetime;
    private SemanticModel? _semanticModel;
    private ExpressionSyntax? _source;

    public void AddDefaultLifetime(MdDefaultLifetime defaultLifetime) =>
        _defaultLifetimes.Add(defaultLifetime);

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

    public void CopyConfigurationTo(IBindingBuilder target)
    {
        foreach (var contract in _contracts)
        {
            target.AddContract(contract);
        }

        foreach (var tag in _tags)
        {
            target.AddTag(tag);
        }

        if (_lifetime is {} lifetime)
        {
            target.Lifetime = lifetime;
        }
    }

    public MdBinding Build(MdSetup setup)
    {
        if (_semanticModel is not {} semanticModel || _source is not {} source)
        {
            throw new CompileErrorException(
                Strings.Error_InvalidBinding,
                ImmutableArray.Create(locationProvider.GetLocation(setup.Source)),
                LogId.ErrorInvalidBinding,
                nameof(Strings.Error_InvalidBinding));
        }

        var implementationType = _implementation?.Type ?? _factory?.Type ?? _arg?.Type;
        var contracts = _contracts.Where(i => i.ContractType is not null).ToList();

        // Process implementation contracts if they exist
        if (implementationType is not null
            && (_implementation?.Source ?? _factory?.Source) is {} contractsSource)
        {
            contracts.AddRange(CreateExplicitContractsFromImplementation(setup, semanticModel, implementationType, contractsSource));
        }

        var id = new Lazy<int>(idGenerator.Generate);
        var implementationTags = _tags
            .Select(tag => BuildTag(tag, implementationType, id))
            .ToImmutableArray();

        // Map tags for all contracts
        var contractsWithTags = contracts
            .Select(c => c with { Tags = c.Tags.Select(tag => BuildTag(tag, implementationType, id)).ToImmutableArray() })
            .ToImmutableArray();

        var lifetime = lifetimeProvider.GetActualLifetime(_defaultLifetimes, _lifetime, implementationType, implementationTags, contractsWithTags, true);

        return new MdBinding(
            int.MaxValue - specialBindingIdGenerator.Generate(),
            source,
            setup,
            semanticModel,
            contractsWithTags,
            implementationTags,
            lifetime,
            _implementation,
            _factory,
            _arg);
    }

    private IEnumerable<MdContract> CreateExplicitContractsFromImplementation(
        MdSetup setup,
        SemanticModel semanticModel,
        ITypeSymbol implementationType,
        ExpressionSyntax contractsSource)
    {
        var implementationContracts = _contracts.Where(i => i.ContractType is null).ToList();
        if (implementationContracts.Count == 0)
        {
            yield break;
        }

        var baseSymbols = Enumerable.Empty<ITypeSymbol>();

        // Only search for base symbols if the implementation is a concrete class or struct
        if (implementationType is { SpecialType: Microsoft.CodeAnalysis.SpecialType.None, TypeKind: TypeKind.Class or TypeKind.Struct, IsAbstract: false })
        {
            var specialTypes = setup.SpecialTypes.Select(i => i.Type).ToImmutableHashSet(typeSymbolComparer.Runtime);
            var compilation = semanticModel.Compilation;
            var directInterfaces = GetDirectInterfaces(implementationType);
            baseSymbols = baseSymbolsProvider
                .GetBaseSymbols(implementationType, (type, deepness) => deepness switch
                {
                    0 => true,
                    1 => IsSuitableForBinding(compilation, specialTypes, type),
                    _ => false
                }, 1)
                .Select(i => i.Type)
                // For a type loaded from another assembly Roslyn may report transitive
                // InterfaceImpl entries in Interfaces. Keep the source-level semantics:
                // only interfaces implemented directly by the implementation are contracts.
                .Where(type => type.TypeKind != TypeKind.Interface || directInterfaces.Contains(type));
        }

        var contracts = new HashSet<ITypeSymbol>(baseSymbols, typeSymbolComparer.Runtime) { implementationType };
        var tags = implementationContracts
            .SelectMany(i => i.Tags)
            .GroupBy(i => i.Value)
            .Select(i => i.First())
            .ToImmutableArray();

        foreach (var contract in contracts)
        {
            yield return new MdContract(semanticModel, contractsSource, contract, ContractKind.Explicit, tags);
        }
    }

    private ImmutableHashSet<ITypeSymbol> GetDirectInterfaces(ITypeSymbol implementationType)
    {
        var declaredInterfaces = implementationType.Interfaces;
        if (declaredInterfaces.IsDefaultOrEmpty)
        {
            return ImmutableHashSet<ITypeSymbol>.Empty.WithComparer(typeSymbolComparer.Runtime);
        }

        var inheritedByBaseType = implementationType.BaseType?.AllInterfaces
            .ToImmutableHashSet(typeSymbolComparer.Runtime)
            ?? ImmutableHashSet<ITypeSymbol>.Empty.WithComparer(typeSymbolComparer.Runtime);

        return declaredInterfaces
            .Where(candidate => !inheritedByBaseType.Contains(candidate))
            .Where(candidate => !declaredInterfaces.Any(other =>
                !typeSymbolComparer.Runtime.Equals(other, candidate)
                && other.AllInterfaces.Any(inherited => typeSymbolComparer.Runtime.Equals(inherited, candidate))))
            .ToImmutableHashSet(typeSymbolComparer.Runtime);
    }

    private bool IsSuitableForBinding(Compilation compilation, ImmutableHashSet<ITypeSymbol> specialTypes, ITypeSymbol type)
    {
        // Checks if the type is an interface or an abstract class, which are typical candidates for DI contracts.
        var isAbstractOrInterface = type.TypeKind == TypeKind.Interface || type.IsAbstract;

        // For constructed generic types Roslyn may expose the special type only on the original
        // definition (for example IEnumerable<int> -> IEnumerable<T>).
        var specialType = type is INamedTypeSymbol namedType
            ? namedType.OriginalDefinition.SpecialType
            : type.SpecialType;
        var isNotSpecialType = specialType == Microsoft.CodeAnalysis.SpecialType.None;

        // 'System.IAsyncDisposable' is not covered by the Roslyn special types, so it is excluded explicitly
        // to keep it in line with 'System.IDisposable'.
        var isNotAsyncDisposable = types.TryGet(Core.SpecialType.IAsyncDisposable, compilation) is not {} asyncDisposableType
            || !types.TypeEquals(type, asyncDisposableType);

        // Verifies that the type is not explicitly excluded via the 'SpecialTypes' setup configuration.
        var isNotMarkedAsSpecial = !specialTypes.Contains(type);

        return isAbstractOrInterface && isNotSpecialType && isNotAsyncDisposable && isNotMarkedAsSpecial;
    }

    private static MdTag BuildTag(MdTag tag, ITypeSymbol? type, Lazy<int> id)
    {
        if (type is null || tag.Value is not Tag tagVal)
        {
            return tag;
        }

        if (tagVal == Type)
        {
            return MdTag.CreateTypeTag(tag, type);
        }

        if (tagVal == Unique)
        {
            return MdTag.CreateUniqueTag(tag, id.Value);
        }

        // ReSharper disable once ConvertIfStatementToReturnStatement
        if (tagVal == Any)
        {
            return MdTag.CreateAnyTag(tag);
        }

        return tag;
    }
}
