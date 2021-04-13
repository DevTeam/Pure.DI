namespace Pure.DI.Core
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Microsoft.CodeAnalysis;

    internal static class Attributes
    {
        public static IEnumerable<AttributeData> GetAttributes(this ISymbol symbol, Type attributeType, SemanticModel semanticModel) =>
            from attr in symbol.GetAttributes()
            where attr.AttributeClass != null && new SemanticType(attr.AttributeClass, semanticModel).Equals(attributeType)
            select attr;

        public static IEnumerable<AttributeData> GetAttributes(this ISymbol symbol, INamedTypeSymbol attributeType) =>
            from attr in symbol.GetAttributes()
            where attr.AttributeClass != null && SymbolEqualityComparer.Default.Equals(attr.AttributeClass, attributeType)
            select attr;
    }
}
