// ReSharper disable HeapView.ObjectAllocation
// ReSharper disable NotAccessedPositionalProperty.Global

namespace Pure.DI.Core.Models;

readonly record struct MdAccumulator(
    SemanticModel SemanticModel,
    SyntaxNode Source,
    ITypeSymbol Type,
    ITypeSymbol AccumulatorType,
    Lifetime Lifetime)
{
    public bool Equals(MdAccumulator other) =>
        SymbolEqualityComparer.Default.Equals(Type, other.Type)
        && SymbolEqualityComparer.Default.Equals(AccumulatorType, other.AccumulatorType)
        && Lifetime == other.Lifetime;

    public override int GetHashCode()
    {
        unchecked
        {
            var hashCode = SymbolEqualityComparer.Default.GetHashCode(Type);
            hashCode = hashCode * 397 ^ SymbolEqualityComparer.Default.GetHashCode(AccumulatorType);
            hashCode = hashCode * 397 ^ (int)Lifetime;
            return hashCode;
        }
    }
}