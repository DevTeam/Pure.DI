// ReSharper disable HeapView.ObjectAllocation
// ReSharper disable NotAccessedPositionalProperty.Global
namespace Pure.DI.Core.Models;

internal readonly record struct MdResolver(
    SemanticModel SemanticModel,
    SyntaxNode Source,
    int Position,
    ITypeSymbol ContractType,
    MdTag? Tag,
    ExpressionSyntax TargetValue,
    ImmutableArray<AttributeSyntax> Attributes = default)
{
    public override string ToString() => $"<=={ContractType}({Tag?.ToString()})";
}