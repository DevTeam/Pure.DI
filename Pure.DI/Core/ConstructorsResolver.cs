namespace Pure.DI.Core
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Microsoft.CodeAnalysis;

    internal class ConstructorsResolver : IConstructorsResolver
    {
        public IEnumerable<IMethodSymbol> Resolve(INamedTypeSymbol typeSymbol, SemanticModel semanticModel) =>
            from ctor in typeSymbol.Constructors
            where ctor.DeclaredAccessibility != Accessibility.Private
            let isObsoleted = (
                from attr in ctor.GetAttributes()
                where typeof(ObsoleteAttribute).Equals(attr.AttributeClass, semanticModel)
                select attr).Any()
            let parameters = ctor.Parameters
            let canBeResolved = (
                from parameter in parameters
                where parameter.IsOptional || parameter.HasExplicitDefaultValue
                select parameter).Any()
            let order = (parameters.Length + 1) * (canBeResolved ? 0xffff : 1) * (isObsoleted ? 1 : 0xff)
            orderby order descending
            select ctor;
    }
}
