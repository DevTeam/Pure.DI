namespace Pure.DI.Core;

class LocationProvider : ILocationProvider
{
    public Location GetLocation(SyntaxNode node)
    {
        switch (node)
        {
            case InvocationExpressionSyntax { Expression: MemberAccessExpressionSyntax member } invocation
                when member.IsKind(SyntaxKind.SimpleMemberAccessExpression)
                     && member.ChildNodes().OfType<SimpleNameSyntax>().LastOrDefault() is {} name:
            {
                var start = name.Span.Start;
                var end = invocation.ArgumentList.Span.End;
                var maxSpan = node.SyntaxTree.GetRoot().Span;
                if (start < maxSpan.Start)
                {
                    start = maxSpan.Start;
                }

                if (end > maxSpan.End)
                {
                    end = maxSpan.End;
                }

                return node.SyntaxTree.GetLocation(new TextSpan(start, end - start));
            }

            default:
                return node.GetLocation();
        }

    }

    public Location GetLocation(SyntaxToken token)
    {
        return token.GetLocation();
    }
}