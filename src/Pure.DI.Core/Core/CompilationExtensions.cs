// ReSharper disable HeapView.DelegateAllocation
namespace Pure.DI.Core;

using System.Diagnostics.CodeAnalysis;

internal static class CompilationExtensions
{
    public static IEqualityComparer<ISymbol?> GetComparer(this Compilation compilation) => 
        compilation.Options.NullableContextOptions == NullableContextOptions.Enable
            ? SymbolEqualityComparer.IncludeNullability
            : SymbolEqualityComparer.Default;

    [SuppressMessage("ReSharper", "HeapView.ClosureAllocation")]
    public static IReadOnlyList<AttributeData> GetAttributes(this Compilation compilation, ISymbol member, ITypeSymbol attributeType)
    {
        var symbolComparer = compilation.GetComparer();
        return member.GetAttributes()
            .Where(attr => attr.AttributeClass != null && symbolComparer.Equals(attr.AttributeClass, attributeType))
            .ToArray();
    }
}