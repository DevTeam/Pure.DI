namespace Pure.DI.Core;

internal interface ISymbolNames
{
    string GetName(ITypeSymbol typeSymbol);
    
    string GetGlobalName(ITypeSymbol typeSymbol);
}