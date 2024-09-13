namespace Pure.DI.Core;

internal interface IInjectionSiteFactory
{
    string CreateInjectionSite(ISymbol containingSymbol, string name);

    MdInjectionSite CreateInjectionSite(SyntaxNode source, ISymbol containingSymbol, string name);
}