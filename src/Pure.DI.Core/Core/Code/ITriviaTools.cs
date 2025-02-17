namespace Pure.DI.Core.Code;

interface ITriviaTools
{
    T PreserveTrivia<T>(IHints hints, T newNode, SyntaxNode prevNode)
        where T : SyntaxNode;

    SyntaxToken PreserveTrivia(IHints hints, SyntaxToken newToken, SyntaxToken prevToken);
}