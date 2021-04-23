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

            return value switch
            {
                string val => SyntaxFactory.LiteralExpression(SyntaxKind.StringLiteralExpression, SyntaxFactory.Literal(val)),
                char val => SyntaxFactory.LiteralExpression(SyntaxKind.CharacterLiteralExpression, SyntaxFactory.Literal(val)),
                int val => SyntaxFactory.LiteralExpression(SyntaxKind.NumericLiteralExpression, SyntaxFactory.Literal(val)),
                uint val => SyntaxFactory.LiteralExpression(SyntaxKind.NumericLiteralExpression, SyntaxFactory.Literal(val)),
                byte val => SyntaxFactory.LiteralExpression(SyntaxKind.NumericLiteralExpression, SyntaxFactory.Literal(val)),
                long val => SyntaxFactory.LiteralExpression(SyntaxKind.NumericLiteralExpression, SyntaxFactory.Literal(val)),
                ulong val => SyntaxFactory.LiteralExpression(SyntaxKind.NumericLiteralExpression, SyntaxFactory.Literal(val)),
                decimal val => SyntaxFactory.LiteralExpression(SyntaxKind.NumericLiteralExpression, SyntaxFactory.Literal(val)),
                double val => SyntaxFactory.LiteralExpression(SyntaxKind.NumericLiteralExpression, SyntaxFactory.Literal(val)),
                float val => SyntaxFactory.LiteralExpression(SyntaxKind.NumericLiteralExpression, SyntaxFactory.Literal(val)),
                bool val => val ? SyntaxFactory.LiteralExpression(SyntaxKind.TrueLiteralExpression, SyntaxFactory.Token(SyntaxKind.TrueKeyword)) : SyntaxFactory.LiteralExpression(SyntaxKind.FalseLiteralExpression, SyntaxFactory.Token(SyntaxKind.FalseKeyword)),
                _ => null
            };
        }
    }
}
