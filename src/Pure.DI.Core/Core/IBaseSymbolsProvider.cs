namespace Pure.DI.Core;

interface IBaseSymbolsProvider
{
    IEnumerable<TypeInfo> GetBaseSymbols(
        ITypeSymbol symbol,
        Func<ITypeSymbol, int, bool> predicate,
        int maxDeepness = int.MaxValue);
}