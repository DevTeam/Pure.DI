namespace Pure.DI.InterfaceGeneration;

using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;

sealed class RoslynSymbols : IRoslynSymbols
{
    public IEnumerable<ISymbol> GetAllMembers(ITypeSymbol type)
    {
        var current = type;
        while (current != null)
        {
            foreach (var member in current.GetMembers())
            {
                yield return member;
            }

            current = current.BaseType;
        }
    }

    public string GetWhereStatement(ITypeParameterSymbol typeParameterSymbol, SymbolDisplayFormat typeDisplayFormat)
    {
        var constraints = new List<string>();

        if (typeParameterSymbol.HasReferenceTypeConstraint)
        {
            constraints.Add("class");
        }

        if (typeParameterSymbol.HasValueTypeConstraint)
        {
            constraints.Add("struct");
        }

        if (typeParameterSymbol.HasNotNullConstraint)
        {
            constraints.Add("notnull");
        }

        constraints.AddRange(typeParameterSymbol.ConstraintTypes.Select(t => t.ToDisplayString(typeDisplayFormat)));

        if (typeParameterSymbol.HasConstructorConstraint)
        {
            constraints.Add("new()");
        }

        return constraints.Count == 0 ? string.Empty : $"where {typeParameterSymbol.Name} : {string.Join(", ", constraints)}";
    }
}
