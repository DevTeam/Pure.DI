namespace Pure.DI.Core;

internal interface ISymbolNames
{
    string GetDisplayName(ITypeSymbol typeSymbol);
}