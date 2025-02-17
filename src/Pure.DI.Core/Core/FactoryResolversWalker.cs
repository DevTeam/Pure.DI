namespace Pure.DI.Core;

sealed class FactoryResolversWalker : CSharpSyntaxWalker, IFactoryResolversWalker
{
    private readonly List<InvocationExpressionSyntax> _initializers = [];
    private readonly List<InvocationExpressionSyntax> _resolvers = [];

    public IReadOnlyCollection<InvocationExpressionSyntax> Resolvers => _resolvers;

    public IReadOnlyCollection<InvocationExpressionSyntax> Initializers => _initializers;

    public override void VisitInvocationExpression(InvocationExpressionSyntax invocation)
    {
        base.VisitInvocationExpression(invocation);
        if (invocation.Expression is not MemberAccessExpressionSyntax memberAccess
            || !memberAccess.IsKind(SyntaxKind.SimpleMemberAccessExpression))
        {
            return;
        }

        switch (memberAccess.Name)
        {
            case GenericNameSyntax genericName:
                switch (genericName.Identifier.Text)
                {
                    case nameof(IContext.Inject)
                        when invocation.ArgumentList.Arguments.Count is 1 or 2
                             && memberAccess is { Expression: IdentifierNameSyntax contextIdentifierName }
                             && contextIdentifierName.IsKind(SyntaxKind.IdentifierName):
                        _resolvers.Add(invocation);
                        break;

                    case nameof(IContext.BuildUp)
                        when invocation.ArgumentList.Arguments.Count is 1:
                        _initializers.Add(invocation);
                        break;
                }

                break;

            case IdentifierNameSyntax identifierName:
                switch (identifierName.Identifier.Text)
                {
                    case nameof(IContext.Inject)
                        when invocation.ArgumentList.Arguments.Count is 1 or 2
                             && memberAccess is { Expression: IdentifierNameSyntax contextIdentifierName }
                             && contextIdentifierName.IsKind(SyntaxKind.IdentifierName):
                        _resolvers.Add(invocation);
                        break;

                    case nameof(IContext.BuildUp)
                        when invocation.ArgumentList.Arguments.Count is 1:
                        _initializers.Add(invocation);
                        break;
                }

                break;
        }
    }
}