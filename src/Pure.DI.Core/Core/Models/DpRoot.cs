namespace Pure.DI.Core.Models;

internal readonly record struct DpRoot(
    in MdRoot Source,
    in MdBinding Binding,
    in Injection Injection)
{
    public IEnumerable<string> ToStrings(int indent)
    {
        var walker = new DependenciesToLinesWalker(indent);
        walker.VisitRoot(this);
        return walker;
    }

    public override string ToString() => string.Join(Environment.NewLine, ToStrings(0));
}