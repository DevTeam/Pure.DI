namespace Pure.DI.Core.Models;

internal readonly record struct DpArg(
    in MdArg Source,
    in MdBinding Binding)
{
    public IEnumerable<string> ToStrings(int indent)
    {
        var walker = new DependenciesToLinesWalker(indent);
        walker.VisitArg(this);
        return walker;
    }

    public override string ToString() => string.Join(Environment.NewLine, ToStrings(0));
}