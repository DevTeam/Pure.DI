// ReSharper disable ClassNeverInstantiated.Global
namespace Pure.DI.Core;

internal class BaseSymbolsProvider : IBaseSymbolsProvider
{
    public IEnumerable<ITypeSymbol> GetBaseSymbols(
        ITypeSymbol symbol,
        Func<ITypeSymbol, int, bool> predicate,
        int maxDeepness = int.MaxValue) =>
        GetBaseSymbols(symbol, predicate, maxDeepness, 0);

    private static IEnumerable<ITypeSymbol> GetBaseSymbols(
        ITypeSymbol symbol,
        Func<ITypeSymbol, int, bool> predicate,
        int maxDeepness,
        int deepness)
    {
        if (deepness > maxDeepness)
        {
            yield break;
        }

        while (true)
        {
            if (predicate(symbol, deepness))
            {
                yield return symbol;
            }

            deepness++;
            foreach (var type in symbol.Interfaces.SelectMany(i => GetBaseSymbols(i, predicate, maxDeepness, deepness)))
            {
                yield return type;
            }

            if (symbol.BaseType != default)
            {
                symbol = symbol.BaseType;
                continue;
            }

            break;
        }
    }
}