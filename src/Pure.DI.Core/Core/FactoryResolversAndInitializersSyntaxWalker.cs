namespace Pure.DI.Core;

internal sealed class FactoryResolversAndInitializersSyntaxWalker : CSharpSyntaxWalker
{
    public readonly List<InvocationExpressionSyntax> Resolvers = [];
    public readonly List<InvocationExpressionSyntax> Initializers = [];

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
                        Resolvers.Add(invocation);
                        break;
                    
                    case nameof(IContext.Initialize)
                        when invocation.ArgumentList.Arguments.Count is 1:
                        Initializers.Add(invocation);
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
                        Resolvers.Add(invocation);
                        break;
                    
                    case nameof(IContext.Initialize)
                        when invocation.ArgumentList.Arguments.Count is 1:
                        Initializers.Add(invocation);
                        break;
                }

                break;
        }
    }
}