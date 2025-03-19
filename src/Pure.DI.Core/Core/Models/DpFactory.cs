// ReSharper disable NotAccessedPositionalProperty.Global

namespace Pure.DI.Core.Models;

record DpFactory(
    in MdFactory Source,
    in MdBinding Binding,
    in ImmutableArray<DpResolver> Resolvers,
    in ImmutableArray<DpInitializer> Initializers)
{
    public bool HasOverrides => Overrides.Any();

    public IEnumerable<DpOverride> Overrides => Resolvers.SelectMany(i => i.Overrides).Concat(Initializers.SelectMany(i => i.Overrides));

    public IEnumerable<string> ToStrings(int indent)
    {
        var walker = new DependenciesToLinesWalker(indent);
        walker.VisitFactory(Unit.Shared, this);
        return walker;
    }

    public override string ToString() => string.Join(Environment.NewLine, ToStrings(0));
}