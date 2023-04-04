namespace Pure.DI.Core.CSharp;

internal class ResolversBuilder: IBuilder<IEnumerable<Root>, IEnumerable<ResolverInfo>>
{
    public IEnumerable<ResolverInfo> Build(IEnumerable<Root> roots, CancellationToken cancellationToken) =>
        roots.Where(i => !i.Injection.Type.IsRefLikeType)
            .GroupBy(i => i.Injection.Type, SymbolEqualityComparer.Default)
            .Select((i, id) => new ResolverInfo(id, (ITypeSymbol)i.Key!, i.ToImmutableArray()));
}