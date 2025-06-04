// ReSharper disable ClassNeverInstantiated.Global

namespace Pure.DI.Core.Code;

sealed class ResolversBuilder(ITypeResolver typeResolver)
    : IBuilder<RootsContext, IEnumerable<ResolverInfo>>
{
    public IEnumerable<ResolverInfo> Build(RootsContext ctx) =>
        ctx.Roots
            .Where(i => i.RootArgs.IsEmpty)
            .Where(i => !i.Injection.Type.IsRefLikeType)
            .Where(i => !ReferenceEquals(i.Injection.Tag, MdTag.ContextTag))
            .Where(i => typeResolver.Resolve(ctx.Setup, i.Injection.Type).TypeArgs.Count == 0)
            .GroupBy(i => i.Injection.Type, SymbolEqualityComparer.Default)
            .Select((i, id) => new ResolverInfo(id, (ITypeSymbol)i.Key!, i.ToList()));
}