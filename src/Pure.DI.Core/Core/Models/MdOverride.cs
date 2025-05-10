// ReSharper disable HeapView.ObjectAllocation
// ReSharper disable NotAccessedPositionalProperty.Global

namespace Pure.DI.Core.Models;

readonly record struct MdOverride(
    SemanticModel SemanticModel,
    InvocationExpressionSyntax Source,
    int Id,
    int Position,
    ITypeSymbol ContractType,
    in ImmutableArray<MdTag> Tags,
    ExpressionSyntax ValueExpression);