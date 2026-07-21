namespace Pure.DI.Core.Models;

record Accumulator(
    // bool IsRoot,
    // string Name,
    // bool IsDeclared,
    VarInjection VarInjection,
    ITypeSymbol Type,
    Lifetime Lifetime,
    bool IsEmpty,
    int Capacity
    // ITypeSymbol AccumulatorType
    );
