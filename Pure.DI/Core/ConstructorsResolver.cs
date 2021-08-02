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

        public IEnumerable<IMethodSymbol> Resolve(Dependency dependency)
        {
            if (dependency.Implementation.Type is INamedTypeSymbol type)
            {
                return from ctor in type.Constructors
                    where ctor.DeclaredAccessibility is Accessibility.Internal or Accessibility.Public || ctor.DeclaredAccessibility == Accessibility.Friend
                    let specifiedOrder = (
                        from attrExpression in _attributesService.GetAttributeArgumentExpressions(AttributeKind.Order, ctor)
                        let order = ((attrExpression as LiteralExpressionSyntax)?.Token)?.Value as IComparable
                        select order).LastOrDefault()
                    let isObsoleted = (
                        from attr in ctor.GetAttributes()
                        where attr.AttributeClass != null && new SemanticType(attr.AttributeClass, dependency.Implementation).Equals(typeof(ObsoleteAttribute)) select attr).Any()
                    let parameters = ctor.Parameters
                    let canBeResolved = (
                            from parameter in parameters
                            let paramTypeDescription = _typeResolver.Resolve(new SemanticType(parameter.Type, dependency.Implementation), dependency.Tag, dependency.Implementation.Type.Locations)
                            select parameter.IsOptional || parameter.HasExplicitDefaultValue || paramTypeDescription.IsResolved)
                        .All(isResolved => isResolved)
                    let weight = (parameters.Length + 1) * (canBeResolved ? 0xfff : 1) * (isObsoleted ? 1 : 0xff) * (ctor.DeclaredAccessibility == Accessibility.Public ? 0xf : 1)
                    orderby new ConstructorOrder(canBeResolved ? specifiedOrder: null, weight)
                    select ctor;
            }

            return Enumerable.Empty<IMethodSymbol>();
        }
    }
}
