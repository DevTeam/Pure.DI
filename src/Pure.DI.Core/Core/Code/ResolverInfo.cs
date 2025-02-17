namespace Pure.DI.Core.Code;

record ResolverInfo(
    int Id,
    ITypeSymbol Type,
    IReadOnlyCollection<Root> Roots)
{
    public string ClassName => $"{Names.ResolverClassName}_{Id:0000}";
}