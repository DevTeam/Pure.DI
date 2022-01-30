namespace Pure.DI.Core;

// ReSharper disable once ClassNeverInstantiated.Global
internal class AttributesService : IAttributesService
{
    private readonly IBuildContext _buildContext;

    public AttributesService(IBuildContext buildContext) => _buildContext = buildContext;

    public IEnumerable<ExpressionSyntax> GetAttributeArgumentExpressions(AttributeKind attributeKind, ISymbol type) =>
        from attrMetadata in _buildContext.Metadata.Attributes
        where attrMetadata.Kind == attributeKind
        from attr in type.GetAttributes(attrMetadata.Type)
        let args = (attr.ApplicationSyntaxReference?.GetSyntax() as AttributeSyntax)?.ArgumentList?.Arguments
        where args != null
        where attrMetadata.ArgumentPosition < (args?.Count ?? 0)
        let argExpression = args?[attrMetadata.ArgumentPosition]?.Expression
        where argExpression != null
        select argExpression;
}