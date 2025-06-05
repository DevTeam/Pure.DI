namespace Pure.DI.Core.Models;

using Code.v2;

record Accumulator(
    // bool IsRoot,
    // string Name,
    // bool IsDeclared,
    Var Var,
    ITypeSymbol Type,
    Lifetime Lifetime
    // ITypeSymbol AccumulatorType
    );