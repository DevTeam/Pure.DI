// ReSharper disable ClassNeverInstantiated.Global
namespace Pure.DI.Core
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Microsoft.CodeAnalysis;
    using Microsoft.CodeAnalysis.CSharp;
    using Microsoft.CodeAnalysis.CSharp.Syntax;

    internal class DisposeStatementsBuilder : IDisposeStatementsBuilder
    {
        public IEnumerable<StatementSyntax> Build(SemanticType type, ExpressionSyntax instanceExpression)
        {
            if (!type.ImplementsInterface<IDisposable>())
            {
                yield break;
            }

            yield return SyntaxFactory.IfStatement(
                SyntaxFactory.BinaryExpression(SyntaxKind.NotEqualsExpression, instanceExpression, SyntaxFactory.DefaultExpression(type)),
                SyntaxFactory.ExpressionStatement(
                            SyntaxFactory.InvocationExpression(
                                SyntaxFactory.MemberAccessExpression(
                                    SyntaxKind.SimpleMemberAccessExpression,
                                    instanceExpression,
                                    SyntaxFactory.IdentifierName(nameof(IDisposable.Dispose)))).AddArgumentListArguments()
                        )
            );
        }
    }
}