namespace Pure.DI.Core;

internal class NamedTypeSymbolEqualityComparer: IEqualityComparer<INamedTypeSymbol>
{
    public bool Equals(INamedTypeSymbol x, INamedTypeSymbol y) => 
        SymbolEqualityComparer.Default.Equals(x, y);

    public int GetHashCode(INamedTypeSymbol obj) => 
        SymbolEqualityComparer.Default.GetHashCode(obj);
}