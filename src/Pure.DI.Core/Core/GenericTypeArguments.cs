// ReSharper disable InvertIf
namespace Pure.DI.Core;

using System.Collections.Concurrent;

internal class GenericTypeArguments(ISymbolNames symbolNames) : IGenericTypeArguments
{
    private readonly ConcurrentDictionary<MdSetup, HashSet<string>> _genericTypeArgumentTypes = new();
    private readonly ConcurrentDictionary<MdSetup, HashSet<string>> _genericTypeArgumentAttributesTypes = new();
    
    public bool IsGenericTypeArgument(MdSetup setup, ITypeSymbol typeSymbol) => 
        _genericTypeArgumentTypes.GetOrAdd(setup, k => [..k.GenericTypeArguments.Select(i => symbolNames.GetDisplayName(i.Type))])
            .Contains(symbolNames.GetDisplayName(typeSymbol));

    public bool IsGenericTypeArgumentAttribute(MdSetup setup, ITypeSymbol typeSymbol) => 
        _genericTypeArgumentAttributesTypes.GetOrAdd(setup, k => [..k.GenericTypeArgumentAttributes.Select(i => symbolNames.GetDisplayName(i.AttributeType))])
            .Contains(symbolNames.GetDisplayName(typeSymbol));
}