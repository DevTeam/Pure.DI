// ReSharper disable ClassNeverInstantiated.Global
namespace Pure.DI.Core;

using NS35EBD81B;

internal sealed class RaiseOnDisposableExpressionBuilder : IRaiseOnDisposableExpressionBuilder
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
                SyntaxFactory.Argument(SyntaxFactory.MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression, SyntaxFactory.IdentifierName(typeof(Lifetime).FullName.ReplaceNamespace()), SyntaxFactory.IdentifierName(lifetime.ToString()))));
    }
}