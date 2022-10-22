namespace Pure.DI.Core;

// ReSharper disable once ClassNeverInstantiated.Global
internal sealed class AttributesService : IAttributesService
{
    private readonly IBuildContext _buildContext;

    public AttributesService(IBuildContext buildContext) => _buildContext = buildContext;

    public IEnumerable<ExpressionSyntax> GetAttributeArgumentExpressions(AttributeKind attributeKind, ISymbol type) =>
        from metadata in _buildContext.Metadata.Attributes
        where metadata.Kind == attributeKind
        from attr in type.GetAttributes(metadata.Type)
        let attrSyntax = attr.ApplicationSyntaxReference?.GetSyntax() as AttributeSyntax
        where attrSyntax != null
        let expression = FindExpression(metadata, attrSyntax)
        where expression != null
        select expression;

    private static ExpressionSyntax? FindExpression(AttributeMetadata metadata, AttributeSyntax attr)
    {
        var args = attr.ArgumentList?.Arguments.ToList();
        if (args != null && metadata.ArgumentPosition < args.Count)
        {
            var expression = args[metadata.ArgumentPosition]?.Expression;
            // TypeAttribute
            if (
                metadata.Kind == AttributeKind.Type
                && expression is TypeOfExpressionSyntax typeOfExpressionSyntax)
            {
                return typeOfExpressionSyntax.Type;
            }

            return expression;
        }
        
        // TypeAttribute<T> 
        if (
            metadata.Kind == AttributeKind.Type
            && attr.Name is GenericNameSyntax genericNameSyntax
            && metadata.ArgumentPosition < genericNameSyntax.TypeArgumentList.Arguments.Count)
        {
            return genericNameSyntax.TypeArgumentList.Arguments[metadata.ArgumentPosition];
        }

        return default;
    }
}