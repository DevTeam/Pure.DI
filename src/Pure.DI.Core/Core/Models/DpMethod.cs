﻿namespace Pure.DI.Core.Models;

record DpMethod(
    IMethodSymbol Method,
    int? Ordinal,
    in ImmutableArray<DpParameter> Parameters)
{
    public override string ToString()
    {
        var walker = new DependenciesToLinesWalker(0);
        walker.VisitMethod(Unit.Shared, this, null);
        return string.Join(Environment.NewLine, walker);
    }
}