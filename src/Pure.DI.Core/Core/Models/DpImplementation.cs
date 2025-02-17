// ReSharper disable HeapView.ObjectAllocation

// ReSharper disable NotAccessedPositionalProperty.Global
namespace Pure.DI.Core.Models;

record DpImplementation(
    in MdImplementation Source,
    in MdBinding Binding,
    in DpMethod Constructor,
    in ImmutableArray<DpMethod> Methods,
    in ImmutableArray<DpProperty> Properties,
    in ImmutableArray<DpField> Fields)
{
    public IEnumerable<string> ToStrings(int indent)
    {
        var walker = new DependenciesToLinesWalker(indent);
        walker.VisitImplementation(Unit.Shared, this);
        return walker;
    }

    public override string ToString() => string.Join(Environment.NewLine, ToStrings(0));
}