namespace Pure.DI.Core.Code;

interface ITriviaTools
{
    T PreserveTrivia<T>(IHints hints, T newNode, SyntaxNode prevNode)
        where T : SyntaxNode;

    SyntaxToken PreserveTrivia(bool formatCode, SyntaxToken newToken, SyntaxToken prevToken);
}