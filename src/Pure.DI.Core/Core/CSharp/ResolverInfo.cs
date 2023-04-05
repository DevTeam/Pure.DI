namespace Pure.DI.Core.CSharp;

internal record ResolverInfo(
    int Id,
    ITypeSymbol Type,
    ImmutableArray<Root> Roots)
{
    internal static readonly string ResolverClassName = $"Resolver{Variable.Postfix}";

    public string ClassName => $"{ResolverClassName}{Id.ToString()}";
}