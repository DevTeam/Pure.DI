namespace Pure.DI.Core;

internal interface IBaseSymbolsProvider
{
    IEnumerable<ITypeSymbol> GetBaseSymbols(ITypeSymbol symbol, int deep = int.MaxValue);
}