// ReSharper disable ConvertIfStatementToReturnStatement
// ReSharper disable NotAccessedPositionalProperty.Global
namespace Pure.DI.Core.Models;

internal record TagOnSites(SyntaxNode Source, params string[] InjectionSites)
{
    public override string ToString() => $"TagOn(\"{string.Join(", ", InjectionSites.Select(i => $"{i}"))}\")";

    public virtual bool Equals(TagOnSites? other)
    {
        if (ReferenceEquals(null, other)) return false;
        if (ReferenceEquals(this, other)) return true;
        return StructuralComparisons.StructuralEqualityComparer.Equals(this, other.InjectionSites);
    }

    public override int GetHashCode() => 
        StructuralComparisons.StructuralEqualityComparer.GetHashCode(InjectionSites);
}