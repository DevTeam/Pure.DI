namespace Pure.DI.Core.Code;

record Accumulator(
    bool IsRoot,
    string Name,
    bool IsDeclared,
    ITypeSymbol Type,
    Lifetime Lifetime,
    ITypeSymbol AccumulatorType);