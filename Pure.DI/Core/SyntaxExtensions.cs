namespace Pure.DI.Core
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Microsoft.CodeAnalysis;
    using Microsoft.CodeAnalysis.CSharp;
    using Microsoft.CodeAnalysis.CSharp.Syntax;

    internal static class SyntaxExtensions
    {
        public static IEnumerable<AttributeData> GetAttributes(this ISymbol symbol, Type attributeType, SemanticModel semanticModel) =>
            from attr in symbol.GetAttributes()
            where attr.AttributeClass != null && new SemanticType(attr.AttributeClass, semanticModel).Equals(attributeType)
            select attr;

        public static IEnumerable<AttributeData> GetAttributes(this ISymbol symbol, INamedTypeSymbol attributeType) =>
            from attr in symbol.GetAttributes()
            where attr.AttributeClass != null && SymbolEqualityComparer.Default.Equals(attr.AttributeClass, attributeType)
            select attr;

        public static SemanticModel GetSemanticModel(this SyntaxNode node, SemanticModel semanticModel) =>
            semanticModel.Compilation.SyntaxTrees.Contains(node.SyntaxTree)
                ? semanticModel.Compilation.GetSemanticModel(node.SyntaxTree)
                : semanticModel;

        public static LiteralExpressionSyntax? ToLiteralExpression(this object? value)
        {
            if (value == null)
            {
                return SyntaxFactory.LiteralExpression(SyntaxKind.NullLiteralExpression, SyntaxFactory.Token(SyntaxKind.NullKeyword));
            }

            switch (value)
            {
                case string val:
                    return SyntaxFactory.LiteralExpression(SyntaxKind.StringLiteralExpression, SyntaxFactory.Literal(val));

                case char val:
                    return SyntaxFactory.LiteralExpression(SyntaxKind.CharacterLiteralExpression, SyntaxFactory.Literal(val));

                case int val:
                    return SyntaxFactory.LiteralExpression(SyntaxKind.NumericLiteralExpression, SyntaxFactory.Literal(val));

                case uint val:
                    return SyntaxFactory.LiteralExpression(SyntaxKind.NumericLiteralExpression, SyntaxFactory.Literal(val));

                case byte val:
                    return SyntaxFactory.LiteralExpression(SyntaxKind.NumericLiteralExpression, SyntaxFactory.Literal(val));

                case long val:
                    return SyntaxFactory.LiteralExpression(SyntaxKind.NumericLiteralExpression, SyntaxFactory.Literal(val));

                case ulong val:
                    return SyntaxFactory.LiteralExpression(SyntaxKind.NumericLiteralExpression, SyntaxFactory.Literal(val));

                case decimal val:
                    return SyntaxFactory.LiteralExpression(SyntaxKind.NumericLiteralExpression, SyntaxFactory.Literal(val));

                case double val:
                    return SyntaxFactory.LiteralExpression(SyntaxKind.NumericLiteralExpression, SyntaxFactory.Literal(val));

                case float val:
                    return SyntaxFactory.LiteralExpression(SyntaxKind.NumericLiteralExpression, SyntaxFactory.Literal(val));

                case bool val:
                    return val 
                        ? SyntaxFactory.LiteralExpression(SyntaxKind.TrueLiteralExpression, SyntaxFactory.Token(SyntaxKind.TrueKeyword)) 
                        : SyntaxFactory.LiteralExpression(SyntaxKind.FalseLiteralExpression, SyntaxFactory.Token(SyntaxKind.FalseKeyword));

                default:
                    return null;
            }
        }
    }
}
