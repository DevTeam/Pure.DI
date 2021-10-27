// ReSharper disable ClassNeverInstantiated.Global
namespace Pure.DI.Core
{
    using System;
    using System.Collections.Generic;
    using Microsoft.CodeAnalysis.CSharp;
    using Microsoft.CodeAnalysis.CSharp.Syntax;

    internal class DisposeStatementsBuilder : IDisposeStatementsBuilder
    {
        public IEnumerable<StatementSyntax> Build(SemanticType type, MemberAccessExpressionSyntax instanceExpression, ExpressionSyntax hasInstanceExpression)
        {
            yield return SyntaxFactory.IfStatement(
                hasInstanceExpression,
                SyntaxFactory.Block().AddStatements(
                    SyntaxFactory.ExpressionStatement(
                        SyntaxFactory.AssignmentExpression(
                            SyntaxKind.SimpleAssignmentExpression,
                            hasInstanceExpression,
                            SyntaxFactory.LiteralExpression(SyntaxKind.FalseLiteralExpression))),
                    SyntaxFactory.ExpressionStatement(
                        SyntaxFactory.InvocationExpression(
                            SyntaxFactory.MemberAccessExpression(
                                SyntaxKind.SimpleMemberAccessExpression,
                                instanceExpression,
                                SyntaxFactory.IdentifierName(nameof(IDisposable.Dispose)))).AddArgumentListArguments())
                )
            );
        }
    }
}