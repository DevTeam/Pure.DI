namespace Pure.DI.Core.Code;

internal record Accumulator(
    string Name,
    bool IsDeclared,
    ITypeSymbol Type,
    ITypeSymbol AccumulatorType);