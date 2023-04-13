// ReSharper disable HeapView.DelegateAllocation
namespace Pure.DI.Core;

using System.Diagnostics.CodeAnalysis;

internal static class CompilationExtensions
{
    [SuppressMessage("ReSharper", "HeapView.ClosureAllocation")]
    public static IReadOnlyList<AttributeData> GetAttributes(this ISymbol member, ITypeSymbol attributeType)
    {
        return member.GetAttributes()
            .Where(attr => attr.AttributeClass != null && SymbolEqualityComparer.Default.Equals(attr.AttributeClass, attributeType))
            .ToArray();
    }

    public static LanguageVersion GetLanguageVersion(this Compilation compilation) =>
        compilation is CSharpCompilation sharpCompilation
            ? sharpCompilation.LanguageVersion
            : LanguageVersion.Default;
}