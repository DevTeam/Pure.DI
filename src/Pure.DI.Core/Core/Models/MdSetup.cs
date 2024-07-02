// ReSharper disable HeapView.ObjectAllocation
// ReSharper disable HeapView.BoxingAllocation
// ReSharper disable HeapView.ObjectAllocation.Evident
namespace Pure.DI.Core.Models;

internal record MdSetup(
    SemanticModel SemanticModel,
    SyntaxNode Source,
    in CompositionName Name,
    in ImmutableArray<MdUsingDirectives> UsingDirectives,
    CompositionKind Kind,
    IHints Hints,
    in ImmutableArray<MdBinding> Bindings,
    in ImmutableArray<MdRoot> Roots,
    in ImmutableArray<MdDependsOn> DependsOn,
    ImmutableArray<MdGenericTypeArgument> GenericTypeArguments,
    ImmutableArray<MdGenericTypeArgumentAttribute> GenericTypeArgumentAttributes,
    in ImmutableArray<MdTypeAttribute> TypeAttributes,
    in ImmutableArray<MdTagAttribute> TagAttributes,
    in ImmutableArray<MdOrdinalAttribute> OrdinalAttributes,
    in ImmutableArray<MdAccumulator> Accumulators,
    IReadOnlyCollection<MdTagOnSites> TagOn,
    IReadOnlyCollection<string> Comments,
    ITypeConstructor? TypeConstructor = default)
{
    private readonly Lazy<HashSet<ITypeSymbol>> _genericTypeArgumentTypes =
        new(() => new HashSet<ITypeSymbol>(GenericTypeArguments.Select(i => i.Type), SymbolEqualityComparer.Default));
    
    private readonly Lazy<HashSet<ITypeSymbol>> _genericTypeArgumentAttributesTypes =
        new(() => new HashSet<ITypeSymbol>(GenericTypeArgumentAttributes.Select(i => i.AttributeType), SymbolEqualityComparer.Default));
    
    public bool IsGenericTypeArgument(ITypeSymbol typeSymbol) =>
        _genericTypeArgumentTypes.Value.Contains(typeSymbol);

    public bool IsGenericTypeArgumentAttribute(ITypeSymbol typeSymbol) =>
        _genericTypeArgumentAttributesTypes.Value.Contains(typeSymbol);
}