namespace Pure.DI.Core.Code;

internal record ResolverInfo(
    int Id,
    ITypeSymbol Type,
    ImmutableArray<Root> Roots)
{
    internal static readonly string ResolverClassName = $"Resolver{Variable.Salt}";

    public string ClassName => $"{ResolverClassName}_{Id:0000}";
}