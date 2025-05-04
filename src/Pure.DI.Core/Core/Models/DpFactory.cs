// ReSharper disable NotAccessedPositionalProperty.Global

namespace Pure.DI.Core.Models;

record DpFactory(
    in MdFactory Source,
    in MdBinding Binding,
    in ImmutableArray<DpResolver> Resolvers,
    in ImmutableArray<DpInitializer> Initializers,
    IReadOnlyDictionary<int, DpOverride> OverridesMap,
    ILocationProvider LocationProvider)
{
    public bool HasOverrides => Resolvers.SelectMany(i => i.Overrides).Concat(Initializers.SelectMany(i => i.Overrides)).Any();

    public DpOverride ResolveOverride(in DpOverride @override) =>
        OverridesMap.TryGetValue(@override.Source.Id, out var resolvedOverride) ? resolvedOverride : @override;

    public IEnumerable<string> ToStrings(int indent)
    {
        var walker = new DependenciesToLinesWalker(indent, LocationProvider);
        walker.VisitFactory(Unit.Shared, this);
        return walker;
    }

    public override string ToString() => string.Join(Environment.NewLine, ToStrings(0));
}