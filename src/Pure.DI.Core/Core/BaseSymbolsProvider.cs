namespace Pure.DI.Core;

internal class BaseSymbolsProvider : IBaseSymbolsProvider
{
    public IEnumerable<ITypeSymbol> GetBaseSymbols(ITypeSymbol symbol, int deep = int.MaxValue)
    {
        while (true)
        {
            yield return symbol;
            foreach (var type in symbol.AllInterfaces.SelectMany(i => GetBaseSymbols(i, --deep)))
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