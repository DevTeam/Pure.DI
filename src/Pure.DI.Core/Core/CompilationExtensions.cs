// ReSharper disable HeapView.DelegateAllocation
namespace Pure.DI.Core;

internal static class CompilationExtensions
{
    [SuppressMessage("ReSharper", "HeapView.ClosureAllocation")]
    public static IReadOnlyList<AttributeData> GetAttributes(this ISymbol member, INamedTypeSymbol attributeType) =>
        member
            .GetAttributes()
            .Where(attr => 
                attr.AttributeClass != null
                && GetUnboundTypeSymbol(attr.AttributeClass)?.ToDisplayString(NullableFlowState.None, SymbolDisplayFormat.FullyQualifiedFormat) == attributeType.ToDisplayString(NullableFlowState.None, SymbolDisplayFormat.FullyQualifiedFormat))
            .ToArray();

    public static LanguageVersion GetLanguageVersion(this Compilation compilation) =>
        compilation is CSharpCompilation sharpCompilation
            ? sharpCompilation.LanguageVersion
            : LanguageVersion.Default;

    private static INamedTypeSymbol? GetUnboundTypeSymbol(INamedTypeSymbol? typeSymbol) => 
        typeSymbol is null
            ? typeSymbol : typeSymbol.IsGenericType
                ? typeSymbol.ConstructUnboundGenericType()
                : typeSymbol;
}