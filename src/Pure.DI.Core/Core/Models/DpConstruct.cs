namespace Pure.DI.Core.Models;

record DpConstruct(
    in MdConstruct Source,
    in MdBinding Binding,
    in ImmutableArray<Injection> Injections,
    ILocationProvider LocationProvider)
{
    public IEnumerable<string> ToStrings(int indent)
    {
        var walker = new DependenciesToLinesWalker(indent, LocationProvider);
        walker.VisitConstruct(Unit.Shared, this);
        return walker;
    }

    public override string ToString() => string.Join(Environment.NewLine, ToStrings(0));
}