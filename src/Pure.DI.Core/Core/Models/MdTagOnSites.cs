// ReSharper disable ConvertIfStatementToReturnStatement
// ReSharper disable NotAccessedPositionalProperty.Global

namespace Pure.DI.Core.Models;

record MdTagOnSites(
    SyntaxNode Source,
    ImmutableArray<MdInjectionSite> InjectionSites)
{
    public override string ToString() => $"TagOn(\"{string.Join(", ", InjectionSites.Select(i => $"{i}"))}\")";
}