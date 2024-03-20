// ReSharper disable HeapView.ObjectAllocation
// ReSharper disable NotAccessedPositionalProperty.Global
namespace Pure.DI.Core.Models;

internal readonly record struct MdAccumulator(
    SemanticModel SemanticModel,
    SyntaxNode Source,
    ITypeSymbol Type,
    ITypeSymbol AccumulatorType,
    Lifetime Lifetime);