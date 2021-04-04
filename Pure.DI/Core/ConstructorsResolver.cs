namespace Pure.DI.Core
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Microsoft.CodeAnalysis;

    internal class ConstructorsResolver : IConstructorsResolver
    {
        public IEnumerable<IMethodSymbol> Resolve(ITypeResolver typeResolver, TypeResolveDescription typeDescription)
        {
            if (typeDescription.Type is INamedTypeSymbol type)
            {
                return from ctor in type.Constructors
                    where ctor.DeclaredAccessibility != Accessibility.Private
                    let isObsoleted = (
                        from attr in ctor.GetAttributes()
                        where
                            attr.AttributeClass != null
                            && typeof(ObsoleteAttribute).Equals(attr.AttributeClass, typeDescription.SemanticModel)
                        select attr).Any()
                    let parameters = ctor.Parameters
                    let canBeResolved = (
                            from parameter in parameters
                            let paramTypeDescription = typeResolver.Resolve(parameter.Type, null, true)
                            select parameter.IsOptional || parameter.HasExplicitDefaultValue || paramTypeDescription.IsResolved)
                        .All(isResolved => isResolved)
                    let order = (parameters.Length + 1) * (canBeResolved ? 0xffff : 1) * (isObsoleted ? 1 : 0xff)
                    orderby order descending
                    select ctor;
            }

            return Enumerable.Empty<IMethodSymbol>();
        }
    }
}
