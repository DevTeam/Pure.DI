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
    TypeSyntax? ArgumentType= default,
    ParameterSyntax? Parameter = default,
    ImmutableArray<AttributeSyntax> Attributes = default,
    ISymbol? Member = default,
    ITypeConstructor? TypeConstructor = default)
{
    public override string ToString() => $"<=={ContractType}({Tag?.ToString()})";
}