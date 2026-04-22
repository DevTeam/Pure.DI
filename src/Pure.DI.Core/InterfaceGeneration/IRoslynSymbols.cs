namespace Pure.DI.InterfaceGeneration;

interface IRoslynSymbols
{
    IEnumerable<ISymbol> GetAllMembers(ITypeSymbol type);

    string GetWhereStatement(ITypeParameterSymbol typeParameterSymbol, SymbolDisplayFormat typeDisplayFormat);
}