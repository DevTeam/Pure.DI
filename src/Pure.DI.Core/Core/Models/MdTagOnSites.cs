// ReSharper disable ConvertIfStatementToReturnStatement
// ReSharper disable NotAccessedPositionalProperty.Global
namespace Pure.DI.Core.Models;

internal record MdTagOnSites(
    SyntaxNode Source,
    ImmutableArray<MdInjectionSite> InjectionSites)
{
    private readonly HashSet<MdInjectionSite> _injectionSiteUsed = [];

    public IReadOnlyCollection<MdInjectionSite> NotUsed => InjectionSites.Except(_injectionSiteUsed).ToList();

    public void Use(MdInjectionSite site) => _injectionSiteUsed.Add(site);

    public override string ToString() => $"TagOn(\"{string.Join(", ", InjectionSites.Select(i => $"{i}"))}\")";
}