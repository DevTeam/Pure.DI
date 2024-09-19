namespace Pure.DI.Core.Code;

public class TriviaTools : ITriviaTools
{
    public T PreserveTrivia<T>(T newNode, SyntaxNode prevNode)
        where T: SyntaxNode
    {
        return newNode.WithLeadingTrivia(TrimEnd(prevNode.GetLeadingTrivia())).WithTrailingTrivia(prevNode.GetTrailingTrivia());
    }
    
    public SyntaxToken PreserveTrivia(SyntaxToken newToken, SyntaxToken prevToken)
    {
        return newToken.WithLeadingTrivia(TrimEnd(prevToken.LeadingTrivia)).WithTrailingTrivia(prevToken.TrailingTrivia);
    }

    private static SyntaxTriviaList TrimEnd(SyntaxTriviaList trivia)
    {
        return new SyntaxTriviaList(trivia.SkipWhile(i => i.IsKind(SyntaxKind.WhitespaceTrivia)).Reverse().SkipWhile(i => i.IsKind(SyntaxKind.WhitespaceTrivia)).Reverse());
    }
}