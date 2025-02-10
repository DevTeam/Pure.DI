namespace Pure.DI.Core;

internal class Metadata(
    ISymbolNames symbolNames)
    : IMetadata
{
    public bool IsMetadata(SyntaxNode node, SemanticModel? semanticModel, CancellationToken cancellationToken)
    {
        if (node is not InvocationExpressionSyntax invocation)
        {
            return false;
        }

        foreach (var curInvocation in invocation.DescendantNodesAndSelf().OfType<InvocationExpressionSyntax>().Reverse())
        {
            if (cancellationToken.IsCancellationRequested)
            {
                return false;
            }

            switch (curInvocation.Expression)
            {
                case IdentifierNameSyntax { Identifier.Text: nameof(DI.Setup) }:
                case MemberAccessExpressionSyntax memberAccess
                    when memberAccess.Kind() == SyntaxKind.SimpleMemberAccessExpression
                         && memberAccess.Name.Identifier.Text == nameof(DI.Setup)
                         && (memberAccess.Expression is IdentifierNameSyntax { Identifier.Text: nameof(DI) }
                             || memberAccess.Expression is MemberAccessExpressionSyntax firstMemberAccess
                             && firstMemberAccess.Kind() == SyntaxKind.SimpleMemberAccessExpression
                             && firstMemberAccess.Name.Identifier.Text == nameof(DI)):

                    if (semanticModel is null || ReturnConfiguration(curInvocation, semanticModel))
                    {
                        return true;
                    }

                    break;
            }
        }

        return false;
    }

    private bool ReturnConfiguration(InvocationExpressionSyntax invocation, SemanticModel semanticModel) =>
        semanticModel.GetTypeInfo(invocation) is var typeInfo
        && (typeInfo.Type ?? typeInfo.ConvertedType) is { } type
        && symbolNames.GetGlobalName(type) == Names.IConfigurationTypeName;
}