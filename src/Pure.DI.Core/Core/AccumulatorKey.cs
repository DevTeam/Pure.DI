namespace Pure.DI.Core;

internal readonly struct AccumulatorKey(
    ITypeSymbol type,
    Lifetime lifetime)
{
    private readonly ITypeSymbol _type = type;
    private readonly Lifetime _lifetime = lifetime;

    public override bool Equals(object? obj) => 
        obj is AccumulatorKey other && Equals(other);

    private bool Equals(AccumulatorKey other) => 
        SymbolEqualityComparer.Default.Equals(_type, other._type) && _lifetime == other._lifetime;

    public override int GetHashCode()
    {
        unchecked
        {
            return (SymbolEqualityComparer.Default.GetHashCode(_type) * 397) ^ (int)_lifetime;
        }
    }
}