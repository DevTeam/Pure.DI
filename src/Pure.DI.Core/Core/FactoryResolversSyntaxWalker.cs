namespace Pure.DI.Core;

internal sealed class FactoryResolversSyntaxWalker : CSharpSyntaxWalker, IEnumerable<InvocationExpressionSyntax>
{
    private readonly List<InvocationExpressionSyntax> _resolvers = [];

    public override void VisitInvocationExpression(InvocationExpressionSyntax invocation)
    {
        base.VisitInvocationExpression(invocation);
        if (invocation.Expression is not MemberAccessExpressionSyntax memberAccess)
        {
            return;
        }

        switch (memberAccess.Name)
        {
            case GenericNameSyntax genericName:
                switch (genericName.Identifier.Text)
                {
                    case nameof(IContext.Inject):
                        if (
                            invocation.ArgumentList.Arguments.Count is 1 or 2
                            && invocation.Expression is MemberAccessExpressionSyntax
                            {
                                Name: GenericNameSyntax
                                {
                                    Identifier.Text: nameof(IContext.Inject),
                                    TypeArgumentList.Arguments: [not null]
                                },
                                Expression: IdentifierNameSyntax contextIdentifierName
                            } contextMemberAccess
                            && contextMemberAccess.IsKind(SyntaxKind.SimpleMemberAccessExpression)
                            && contextIdentifierName.IsKind(SyntaxKind.IdentifierName))
                        {
                            _resolvers.Add(invocation);
                        }

                        break;
                }

                break;

            case IdentifierNameSyntax identifierName:
                switch (identifierName.Identifier.Text)
                {
                    case nameof(IContext.Inject):
                        if (
                            invocation.ArgumentList.Arguments.Count is 1 or 2
                            && invocation.Expression is MemberAccessExpressionSyntax
                            {
                                Name: IdentifierNameSyntax
                                {
                                    Identifier.Text: nameof(IContext.Inject)
                                },
                                Expression: IdentifierNameSyntax contextIdentifierName
                            } contextMemberAccess
                            && contextMemberAccess.IsKind(SyntaxKind.SimpleMemberAccessExpression)
                            && contextIdentifierName.IsKind(SyntaxKind.IdentifierName))
                        {
                            _resolvers.Add(invocation);
                        }

                        break;
                }

                break;
        }
    }

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    public IEnumerator<InvocationExpressionSyntax> GetEnumerator() => _resolvers.GetEnumerator();
}