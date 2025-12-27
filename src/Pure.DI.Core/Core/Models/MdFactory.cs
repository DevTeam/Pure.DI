// ReSharper disable HeapView.ObjectAllocation

namespace Pure.DI.Core.Models;

readonly record struct MdFactory(
    SemanticModel SemanticModel,
    ExpressionSyntax Source,
    ITypeSymbol Type,
    ILocalVariableRenamingRewriter LocalVariableRenamingRewriter,
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