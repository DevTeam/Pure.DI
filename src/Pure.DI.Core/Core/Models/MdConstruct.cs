// ReSharper disable NotAccessedPositionalProperty.Global
namespace Pure.DI.Core.Models;

internal readonly record struct MdConstruct(
    SemanticModel SemanticModel,
    SyntaxNode Source,
    ITypeSymbol Type,
    ITypeSymbol ElementType,
    MdConstructKind Kind,
    ImmutableArray<MdContract> Dependencies)
{
    public override string ToString() => $"To<{Kind}<{Type}>>()";
}