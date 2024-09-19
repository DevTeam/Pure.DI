namespace Pure.DI.Core.Code;

public interface ITriviaTools
{
    T PreserveTrivia<T>(T newNode, SyntaxNode prevNode)
        where T: SyntaxNode;

    SyntaxToken PreserveTrivia(SyntaxToken newToken, SyntaxToken prevToken);
}