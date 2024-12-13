// ReSharper disable HeapView.ObjectAllocation

namespace Pure.DI.Core.Models;

internal readonly record struct MdFactory(
    SemanticModel SemanticModel,
    SyntaxNode Source,
    ITypeSymbol Type,
    LambdaExpressionSyntax Factory,
    bool IsSimpleFactory,
    ParameterSyntax Context,
    in ImmutableArray<MdResolver> Resolvers,
    in ImmutableArray<MdInitializer> Initializers,
    bool HasContextTag,
    in MdResolver? MemberResolver = null)
{
    public override string ToString() => $"To<{Type}>({Factory})";
}