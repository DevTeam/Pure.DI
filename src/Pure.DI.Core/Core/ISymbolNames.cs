namespace Pure.DI.Core;

interface ISymbolNames
{
    string GetName(ITypeSymbol typeSymbol);

    string GetGlobalName(ITypeSymbol typeSymbol);
}