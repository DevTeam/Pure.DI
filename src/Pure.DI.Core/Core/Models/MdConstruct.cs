// ReSharper disable NotAccessedPositionalProperty.Global

namespace Pure.DI.Core.Models;

readonly record struct MdConstruct(
    SemanticModel SemanticModel,
    SyntaxNode Source,
    ITypeSymbol Type,
    ITypeSymbol ElementType,
    MdConstructKind Kind,
    ImmutableArray<MdContract> Dependencies,
    bool HasExplicitDefaultValue,
    object? ExplicitDefaultValue,
    object? State = null)
{
    public override string ToString() => $"To<{Kind}<{Type}>>()";
}