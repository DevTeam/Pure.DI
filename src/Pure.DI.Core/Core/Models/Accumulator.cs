namespace Pure.DI.Core.Models;

record Accumulator(
    bool IsRoot,
    string Name,
    bool IsDeclared,
    ITypeSymbol Type,
    Lifetime Lifetime,
    ITypeSymbol AccumulatorType);