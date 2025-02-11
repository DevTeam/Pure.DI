﻿namespace Pure.DI.Core;

internal interface ITypes
{
    INamedTypeSymbol? TryGet(SpecialType specialType, Compilation compilation);

    INamedTypeSymbol? GetMarker(int index, Compilation compilation);

    bool TypeEquals(ISymbol? type1, ISymbol? type2);
}