namespace Pure.DI.Core.Code;

public readonly record struct TypeDescription(
    string Name,
    IReadOnlyCollection<TypeDescription> TypeArgs,
    ITypeParameterSymbol? TypeParam)
{
    public override string ToString() => Name;
}