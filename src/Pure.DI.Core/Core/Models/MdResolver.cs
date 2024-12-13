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
    TypeSyntax? ArgumentType = null,
    ParameterSyntax? Parameter = null,
    ImmutableArray<AttributeSyntax> Attributes = default,
    ISymbol? Member = null,
    ITypeConstructor? TypeConstructor = null)
{
    public override string ToString() => $"<=={ContractType}({Tag?.ToString()})";
}