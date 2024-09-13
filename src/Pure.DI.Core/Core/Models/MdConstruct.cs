// ReSharper disable NotAccessedPositionalProperty.Global

namespace Pure.DI.Core.Models;

internal readonly record struct MdConstruct(
    SemanticModel SemanticModel,
    SyntaxNode Source,
    ITypeSymbol Type,
    ITypeSymbol ElementType,
    MdConstructKind Kind,
    ImmutableArray<MdContract> Dependencies,
    bool HasExplicitDefaultValue,
    object? ExplicitDefaultValue,
    object? State = default)
{
    public override string ToString() => $"To<{Kind}<{Type}>>()";
}