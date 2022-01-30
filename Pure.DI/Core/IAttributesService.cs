namespace Pure.DI.Core;

internal interface IAttributesService
{
    IEnumerable<ExpressionSyntax> GetAttributeArgumentExpressions(AttributeKind attributeKind, ISymbol type);
}