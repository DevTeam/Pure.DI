namespace Pure.DI.Core
{
    using System.Collections.Generic;
    using Microsoft.CodeAnalysis;
    using Microsoft.CodeAnalysis.CSharp.Syntax;

    internal interface IAttributesService
    {
        IEnumerable<ExpressionSyntax> GetAttributeArgumentExpressions(AttributeKind attributeKind, ISymbol type);
    }
}