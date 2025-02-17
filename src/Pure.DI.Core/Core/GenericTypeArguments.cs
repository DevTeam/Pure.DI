// ReSharper disable InvertIf
namespace Pure.DI.Core;

using System.Collections.Concurrent;

sealed class GenericTypeArguments(ISymbolNames symbolNames) : IGenericTypeArguments
{
    private readonly ConcurrentDictionary<MdSetup, HashSet<string>> _genericTypeArgumentAttributesTypes = new();
    private readonly ConcurrentDictionary<MdSetup, HashSet<string>> _genericTypeArgumentTypes = new();

    public bool IsGenericTypeArgument(MdSetup setup, ITypeSymbol typeSymbol) =>
        _genericTypeArgumentTypes.GetOrAdd(setup, k => [..k.GenericTypeArguments.Select(i => symbolNames.GetGlobalName(i.Type))])
            .Contains(symbolNames.GetGlobalName(typeSymbol));

    public bool IsGenericTypeArgumentAttribute(MdSetup setup, ITypeSymbol typeSymbol) =>
        _genericTypeArgumentAttributesTypes.GetOrAdd(setup, k => [..k.GenericTypeArgumentAttributes.Select(i => symbolNames.GetGlobalName(i.AttributeType))])
            .Contains(symbolNames.GetGlobalName(typeSymbol));
}