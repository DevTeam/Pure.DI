namespace Pure.DI.Core.Code;

interface ITriviaTools
{
    TSyntax PreserveTrivia<TSyntax>(in SyntaxNode baseNode, in TSyntax newNode, bool format)
        where TSyntax : SyntaxNode;

    SyntaxToken PreserveTrivia(in SyntaxToken baseToken, in SyntaxToken newToken, bool format);
}