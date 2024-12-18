﻿// ReSharper disable HeapView.ObjectAllocation
// ReSharper disable NotAccessedPositionalProperty.Global
namespace Pure.DI.Core.Models;

internal record DpInitializer(
    in MdInitializer Source,
    in ImmutableArray<DpMethod> Methods,
    in ImmutableArray<DpProperty> Properties,
    in ImmutableArray<DpField> Fields)
{
    private IEnumerable<string> ToStrings(int indent)
    {
        var walker = new DependenciesToLinesWalker(indent);
        walker.VisitInitializer(Unit.Shared, this);
        return walker;
    }

    public override string ToString() => string.Join(Environment.NewLine, ToStrings(0));
}