// ReSharper disable InvertIf
namespace Pure.DI.Core;

using System.Collections.Concurrent;

internal class SymbolNames : ISymbolNames
{
    private readonly ConcurrentDictionary<ITypeSymbol, string> _displayNames = new(SymbolEqualityComparer.Default);
    
    public string GetDisplayName(ITypeSymbol typeSymbol) => 
        _displayNames.GetOrAdd(typeSymbol, symbol => symbol.ToDisplayString(NullableFlowState.None, SymbolDisplayFormat.FullyQualifiedFormat));
}