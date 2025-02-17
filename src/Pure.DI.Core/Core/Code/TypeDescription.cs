﻿namespace Pure.DI.Core.Code;

readonly record struct TypeDescription(
    string Name,
    IReadOnlyCollection<TypeDescription> TypeArgs,
    ITypeParameterSymbol? TypeParam)
{
    public override string ToString() => Name;
}