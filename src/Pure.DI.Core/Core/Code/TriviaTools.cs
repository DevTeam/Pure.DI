// ReSharper disable ClassNeverInstantiated.Global
// ReSharper disable ConvertIfStatementToReturnStatement
namespace Pure.DI.Core.Code;

sealed class TriviaTools : ITriviaTools
{
    public TSyntax PreserveTrivia<TSyntax>(in SyntaxNode baseNode, in TSyntax newNode, bool format)
        where TSyntax : SyntaxNode =>
        format
            ? newNode.WithLeadingTrivia(baseNode.GetLeadingTrivia()).WithTrailingTrivia(baseNode.GetTrailingTrivia())
            : newNode.WithLeadingTrivia(SyntaxFactory.Space);

    public SyntaxToken PreserveTrivia(in SyntaxToken baseToken, in SyntaxToken newToken, bool format) =>
        format
            ? newToken.WithLeadingTrivia(baseToken.LeadingTrivia).WithTrailingTrivia(baseToken.TrailingTrivia)
            : newToken.WithLeadingTrivia(SyntaxFactory.Space);
}