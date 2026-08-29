// ReSharper disable ClassNeverInstantiated.Global

// ReSharper disable UseCollectionExpression
#pragma warning disable RS1024 // Pure.DI intentionally uses ITypeSymbolComparer to control nullable-reference contract equality.

namespace Pure.DI.Core.Code;

sealed class ResolversBuilder(
    ITypeResolver typeResolver,
    ITypeSymbolComparer typeSymbolComparer,
    IRefSafety refSafety)
    : IBuilder<RootsContext, IEnumerable<ResolverInfo>>
{
    public IEnumerable<ResolverInfo> Build(RootsContext ctx) =>
        ctx.Roots
            .Where(i => i.RootArgs.IsEmpty && i.Source.LightweightKind is not LightweightKind.RootsProvider)
            .Where(i => !refSafety.IsMaybeRefLike(i.Injection.Type))
            .Where(i => !ReferenceEquals(i.Injection.Tag, MdTag.ContextTag))
            .Where(i => typeResolver.Resolve(ctx.Setup, i.Injection.Type).TypeArgs.Count == 0)
            .GroupBy(i => i.Injection.Type, typeSymbolComparer.Dependency)
            .Select((i, id) => new ResolverInfo(id, i.Key!, i.ToList()));
}
