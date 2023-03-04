namespace Pure.DI.Core.Models;

internal readonly record struct Injection(
    ITypeSymbol Type,
    object? Tag,
    ISymbol? Symbol = default,
    MdResolver? Resolver = default,
    MdRoot? Root = default,
    MdContract? Contract = default)
{
    public override string ToString() => $"{Type}{(Tag != default ? $"({Tag})" : "")}";

    public bool Equals(Injection other) => 
        SymbolEqualityComparer.Default.Equals(Type, other.Type) && Equals(Tag, other.Tag);

    public override int GetHashCode() => 
        HashCode.Combine(SymbolEqualityComparer.Default.GetHashCode(Type), Tag);
}