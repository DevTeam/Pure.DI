// ReSharper disable ClassNeverInstantiated.Global
namespace Pure.DI.Core.Code;

internal sealed class ResolversBuilder(ITypeResolver typeResolver)
    : IBuilder<RootContext, IEnumerable<ResolverInfo>>
{
    public IEnumerable<ResolverInfo> Build(RootContext ctx) =>
        ctx.Roots         
            .Where(i => i.Args.IsEmpty)
            .Where(i => !i.Injection.Type.IsRefLikeType)
            .Where(i => !ReferenceEquals(i.Injection.Tag, MdTag.ContextTag))
            .Where(i => typeResolver.Resolve(ctx.Setup, i.Injection.Type).TypeArgs.Count == 0)
            .GroupBy(i => i.Injection.Type, SymbolEqualityComparer.Default)
            .Select((i, id) => new ResolverInfo(id, (ITypeSymbol)i.Key!, i.ToImmutableArray()));
}