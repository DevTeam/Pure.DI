namespace Pure.DI.Core.Models;

record DpArg(
    in MdArg Source,
    in MdBinding Binding,
    ILocationProvider LocationProvider)
{
    public IEnumerable<string> ToStrings(int indent)
    {
        var walker = new DependenciesToLinesWalker(indent, LocationProvider);
        walker.VisitArg(Unit.Shared, this);
        return walker;
    }

    public override string ToString() => string.Join(Environment.NewLine, ToStrings(0));
}