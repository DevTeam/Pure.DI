// ReSharper disable ClassNeverInstantiated.Global
namespace Pure.DI.Core;

internal class DependencyAccessibility : IDependencyAccessibility
{
    private readonly IAccessibilityToSyntaxKindConverter _accessibilityToSyntaxKindConverter;
    private readonly ISettings _settings;

    public DependencyAccessibility(
        IAccessibilityToSyntaxKindConverter accessibilityToSyntaxKindConverter,
        ISettings settings)
    {
        _accessibilityToSyntaxKindConverter = accessibilityToSyntaxKindConverter;
        _settings = settings;
    }

    public SyntaxKind GetSyntaxKind(SemanticType dependency)
    {
        var minAccessibility = GetAccessibility(dependency.Type).Concat(new [] {_settings.Accessibility}).Min();
        return _accessibilityToSyntaxKindConverter.Convert(minAccessibility);
    }
    
    private static IEnumerable<Accessibility> GetAccessibility(ISymbol symbol)
    {
        yield return symbol.DeclaredAccessibility == Accessibility.NotApplicable ? Accessibility.Internal : symbol.DeclaredAccessibility;
        switch (symbol)
        {
            case INamedTypeSymbol { IsGenericType: true } namedTypeSymbol:
            {
                var accessibilitySet =
                    from typeArg in namedTypeSymbol.TypeArguments
                    from accessibility in GetAccessibility(typeArg)
                    select accessibility;

                foreach (var accessibility in accessibilitySet)
                {
                    yield return accessibility;
                }

                break;
            }

            case IArrayTypeSymbol arrayTypeSymbol:
                yield return arrayTypeSymbol.ElementType.DeclaredAccessibility;
                break;
        }
    }
}