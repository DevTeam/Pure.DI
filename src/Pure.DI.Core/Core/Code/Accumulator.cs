namespace Pure.DI.Core.Code;

internal record Accumulator(
    bool IsRoot,
    string Name,
    bool IsDeclared,
    ITypeSymbol Type,
    ITypeSymbol AccumulatorType);