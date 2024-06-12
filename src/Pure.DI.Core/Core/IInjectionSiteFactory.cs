namespace Pure.DI.Core;

internal interface IInjectionSiteFactory
{
    string Create(ISymbol containingSymbol, string name);
}