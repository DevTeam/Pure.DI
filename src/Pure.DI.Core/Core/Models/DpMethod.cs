namespace Pure.DI.Core.Models;

internal readonly record struct DpMethod(
    IMethodSymbol Method,
    int? Order,
    in ImmutableArray<DpParameter> Parameters)
{
    public override string ToString()
    {
        var walker = new DependenciesToLinesWalker(0);
        walker.VisitMethod(this);
        return string.Join(Environment.NewLine, walker);
    }
}