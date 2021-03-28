namespace Pure.DI.Core
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Microsoft.CodeAnalysis;

    internal class ConstructorsResolver : IConstructorsResolver
    {
        public IEnumerable<IMethodSymbol> Resolve(ITypeSymbol typeSymbol, SemanticModel semanticModel) =>
            from member in typeSymbol.GetMembers().OfType<IMethodSymbol>()
            where member.MetadataName.Equals(".ctor", StringComparison.CurrentCultureIgnoreCase)
            where member.DeclaredAccessibility != Accessibility.Private
            let isObsoleted = (
                from attr in member.GetAttributes()
                where typeof(ObsoleteAttribute).Equals(attr.AttributeClass, semanticModel)
                select attr).Any()
            let parameters = member.Parameters
            let canBeResolved = (
                from parameter in parameters
                where parameter.IsOptional || parameter.HasExplicitDefaultValue
                select parameter).Any()
            let order = (parameters.Length + 1) * (canBeResolved ? 0xffff : 1) * (isObsoleted ? 1 : 0xff)
            orderby order descending
            select member;
    }
}
