namespace Pure.DI.Core.CSharp;

internal class ResolversBuilder: IBuilder<ImmutableArray<Root>, IEnumerable<ResolverInfo>>
{
    public IEnumerable<ResolverInfo> Build(ImmutableArray<Root> roots, CancellationToken cancellationToken) =>
        roots.Where(i => !i.Injection.Type.IsRefLikeType)
            .Where(i => !ReferenceEquals(i.Injection.Tag, MdTag.ContextTag))
            .GroupBy(i => i.Injection.Type, SymbolEqualityComparer.Default)
            .Select((i, id) => new ResolverInfo(id, (ITypeSymbol)i.Key!, i.ToImmutableArray()));
}