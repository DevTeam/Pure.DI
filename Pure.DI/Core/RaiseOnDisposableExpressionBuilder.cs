namespace Pure.DI.Core
{
    using System;
    using Microsoft.CodeAnalysis.CSharp;
    using Microsoft.CodeAnalysis.CSharp.Syntax;

    internal class RaiseOnDisposableExpressionBuilder : IRaiseOnDisposableExpressionBuilder
    {
        public ExpressionSyntax Build(SemanticType type, Lifetime lifetime, ExpressionSyntax instanceExpression)
        {
            if (!type.ImplementsInterface<IDisposable>())
            {
                return instanceExpression;
            }

            return SyntaxFactory.InvocationExpression(SyntaxFactory.IdentifierName(SyntaxRepo.RaiseOnDisposableMethodName))
                .AddArgumentListArguments(
                    SyntaxFactory.Argument(instanceExpression),
                    SyntaxFactory.Argument(SyntaxFactory.MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression, SyntaxFactory.IdentifierName(typeof(Lifetime).ToString()), SyntaxFactory.IdentifierName(lifetime.ToString()))));
        }
    }
}