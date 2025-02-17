﻿// ReSharper disable NotAccessedPositionalProperty.Global

namespace Pure.DI.Core.Models;

record DpFactory(
    in MdFactory Source,
    in MdBinding Binding,
    in ImmutableArray<Injection> ResolversInjections,
    in ImmutableArray<DpInitializer> Initializers)
{
    public IEnumerable<string> ToStrings(int indent)
    {
        var walker = new DependenciesToLinesWalker(indent);
        walker.VisitFactory(Unit.Shared, this);
        return walker;
    }

    public override string ToString() => string.Join(Environment.NewLine, ToStrings(0));
}