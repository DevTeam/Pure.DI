namespace Pure.DI.Core
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Microsoft.CodeAnalysis;
    using Microsoft.CodeAnalysis.CSharp.Syntax;

    // ReSharper disable once ClassNeverInstantiated.Global
    internal class ConstructorsResolver : IConstructorsResolver
    {
        private readonly ITypeResolver _typeResolver;
        private readonly IAttributesService _attributesService;

        public ConstructorsResolver(ITypeResolver typeResolver, IAttributesService attributesService)
        {
            _typeResolver = typeResolver;
            _attributesService = attributesService;
        }

        public IEnumerable<IMethodSymbol> Resolve(TypeDescription typeDescription)
        {
            if (typeDescription.Type is INamedTypeSymbol type)
            {
                return from ctor in type.Constructors
                    where ctor.DeclaredAccessibility != Accessibility.Private
                    let specifiedOrder = (
                        from attrExpression in _attributesService.GetAttributeArgumentExpressions(AttributeKind.Order, ctor)
                        let order = ((attrExpression as LiteralExpressionSyntax)?.Token)?.Value as IComparable
                        select order).LastOrDefault()
                    let isObsoleted = (
                        from attr in ctor.GetAttributes()
                        where attr.AttributeClass != null && typeof(ObsoleteAttribute).Equals(attr.AttributeClass, typeDescription.SemanticModel) select attr).Any()
                    let parameters = ctor.Parameters
                    let canBeResolved = (
                            from parameter in parameters
                            let paramTypeDescription = _typeResolver.Resolve(parameter.Type, null, true, true)
                            select parameter.IsOptional || parameter.HasExplicitDefaultValue || paramTypeDescription.IsResolved)
                        .All(isResolved => isResolved)
                    orderby 
                        new ConstructorOrder(
                            canBeResolved ? specifiedOrder: null,
                            (parameters.Length + 1) * (canBeResolved ? 0xffff : 1) * (isObsoleted ? 1 : 0xff))
                    select ctor;
            }

            return Enumerable.Empty<IMethodSymbol>();
        }
    }
}
