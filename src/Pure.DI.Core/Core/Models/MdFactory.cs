// ReSharper disable HeapView.ObjectAllocation
namespace Pure.DI.Core.Models;

internal readonly record struct MdFactory(
    SemanticModel SemanticModel,
    SyntaxNode Source,
    ITypeSymbol Type,
    SimpleLambdaExpressionSyntax Factory,
    ParameterSyntax Context,
    in ImmutableArray<MdResolver> Resolvers,
    bool HasContextTag)
    : IMdImplementation
{
    public override string ToString() => $"To<{Type}>({Factory})";
}