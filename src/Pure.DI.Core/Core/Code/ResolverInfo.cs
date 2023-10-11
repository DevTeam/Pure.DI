namespace Pure.DI.Core.Code;

internal record ResolverInfo(
    int Id,
    ITypeSymbol Type,
    ImmutableArray<Root> Roots)
{
    public string ClassName => $"{Names.ResolverClassName}_{Id:0000}";
}