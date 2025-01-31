// ReSharper disable ClassNeverInstantiated.Global

namespace Pure.DI.Core;

using System.Reflection;

internal class InjectionSiteFactory : IInjectionSiteFactory
{
    private static readonly SymbolDisplayFormat NameTagQualifiedFormat;

    static InjectionSiteFactory()
    {
        NameTagQualifiedFormat = new SymbolDisplayFormat(
            genericsOptions: SymbolDisplayGenericsOptions.IncludeTypeParameters,
            typeQualificationStyle: SymbolDisplayTypeQualificationStyle.NameAndContainingTypesAndNamespaces,
            memberOptions: SymbolDisplayMemberOptions.IncludeType | SymbolDisplayMemberOptions.IncludeContainingType,
            miscellaneousOptions: SymbolDisplayMiscellaneousOptions.UseSpecialTypes | SymbolDisplayMiscellaneousOptions.CollapseTupleTypes
        );

        var qualifiedNameArityFormat = NameTagQualifiedFormat.GetType().GetField("QualifiedNameArityFormat", BindingFlags.Static | BindingFlags.NonPublic);
        var format = (SymbolDisplayFormat?)qualifiedNameArityFormat?.GetValue(null)
                     ?? new SymbolDisplayFormat(typeQualificationStyle: SymbolDisplayTypeQualificationStyle.NameAndContainingTypesAndNamespaces);

        NameTagQualifiedFormat =
            format
                .WithGenericsOptions(SymbolDisplayGenericsOptions.IncludeTypeParameters)
                .WithMemberOptions(SymbolDisplayMemberOptions.IncludeType | SymbolDisplayMemberOptions.IncludeContainingType)
                .WithMiscellaneousOptions(SymbolDisplayMiscellaneousOptions.UseSpecialTypes | SymbolDisplayMiscellaneousOptions.CollapseTupleTypes);
    }

    public string CreateInjectionSite(ISymbol containingSymbol, string name)
    {
        var memberName = containingSymbol.ToDisplayString(NameTagQualifiedFormat);
        return $"{memberName}:{name}";
    }

    public MdInjectionSite CreateInjectionSite(SyntaxNode source, ISymbol containingSymbol, string name) => new(source, CreateInjectionSite(containingSymbol, name));
}