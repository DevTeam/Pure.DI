namespace Pure.DI.Core.Models;

internal readonly record struct MdInjectionSite(SyntaxNode Source, string Site)
{
    public bool Equals(MdInjectionSite other) => Site == other.Site;

    public override int GetHashCode() => Site.GetHashCode();
}