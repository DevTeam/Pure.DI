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
    IReadOnlyCollection<string> Comments)
{
    private readonly Lazy<HashSet<string>> _genericTypeArgumentTypes =
        new(() => [..GenericTypeArguments.Select(i => i.Type.ToDisplayString(NullableFlowState.None, SymbolDisplayFormat.FullyQualifiedFormat))]);

    private readonly Lazy<HashSet<string>> _genericTypeArgumentAttributesTypes =
        new(() => [..GenericTypeArgumentAttributes.Select(i => i.AttributeType.ToDisplayString(NullableFlowState.None, SymbolDisplayFormat.FullyQualifiedFormat))]);

    public bool IsGenericTypeArgument(ITypeSymbol typeSymbol) =>
        _genericTypeArgumentTypes.Value.Contains(typeSymbol.ToDisplayString(NullableFlowState.None, SymbolDisplayFormat.FullyQualifiedFormat));

    public bool IsGenericTypeArgumentAttribute(ITypeSymbol typeSymbol) =>
        _genericTypeArgumentAttributesTypes.Value.Contains(typeSymbol.ToDisplayString(NullableFlowState.None, SymbolDisplayFormat.FullyQualifiedFormat));
}