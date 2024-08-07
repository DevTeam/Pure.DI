namespace Pure.DI.Core;

using Microsoft.CodeAnalysis.CSharp.Syntax;

internal class FullyQualifiedNameRewriter(SemanticModel semanticModel): CSharpSyntaxRewriter
{
    public static readonly SymbolDisplayFormat FullTypeNameFormat =
        new(
            globalNamespaceStyle: SymbolDisplayGlobalNamespaceStyle.Omitted,
            typeQualificationStyle: SymbolDisplayTypeQualificationStyle.NameAndContainingTypesAndNamespaces,
            genericsOptions: SymbolDisplayGenericsOptions.IncludeTypeParameters,
            miscellaneousOptions:
            SymbolDisplayMiscellaneousOptions.EscapeKeywordIdentifiers |
            SymbolDisplayMiscellaneousOptions.UseSpecialTypes);

    public override SyntaxNode? VisitQualifiedName(QualifiedNameSyntax node)
    {
        if (semanticModel.GetSymbolInfo(node).Symbol is not ITypeSymbol type)
        {
            return base.VisitQualifiedName(node);
        }
        
        var typeName = type.ToDisplayString(NullableFlowState.None, FullTypeNameFormat);
        return SyntaxFactory.ParseTypeName(typeName);
    }

    public override SyntaxNode? VisitGenericName(GenericNameSyntax node)
    {
        if (semanticModel.GetSymbolInfo(node).Symbol is not ITypeSymbol type)
        {
            return base.VisitGenericName(node);
        }
        
        var typeName = type.ToDisplayString(NullableFlowState.None, FullTypeNameFormat);
        return SyntaxFactory.ParseTypeName(typeName);
    }

    public override SyntaxNode? VisitIdentifierName(IdentifierNameSyntax node)
    {
        if (semanticModel.GetSymbolInfo(node).Symbol is not ITypeSymbol type)
        {
            return base.VisitIdentifierName(node);
        }
        
        var typeName = type.ToDisplayString(NullableFlowState.None, FullTypeNameFormat);
        return SyntaxFactory.ParseTypeName(typeName);
    }
}