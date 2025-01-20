// ReSharper disable InvertIf
namespace Pure.DI.Core;

using System.Collections.Concurrent;

internal class SymbolNames : ISymbolNames
{
    private readonly ConcurrentDictionary<ITypeSymbol, string> _names = new(SymbolEqualityComparer.Default);
    private readonly ConcurrentDictionary<ITypeSymbol, string> _globalNames = new(SymbolEqualityComparer.Default);

    public string GetName(ITypeSymbol typeSymbol) =>
        _names.GetOrAdd(typeSymbol, symbol => symbol.ToString());

    public string GetGlobalName(ITypeSymbol typeSymbol) => 
        _globalNames.GetOrAdd(typeSymbol, symbol => symbol.ToDisplayString(NullableFlowState.None, SymbolDisplayFormat.FullyQualifiedFormat));
}