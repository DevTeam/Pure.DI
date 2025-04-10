﻿// ReSharper disable ClassNeverInstantiated.Global
// ReSharper disable ConvertIfStatementToReturnStatement
namespace Pure.DI.Core.Code;

sealed class TriviaTools : ITriviaTools
{
    public T PreserveTrivia<T>(IHints hints, T newNode, SyntaxNode prevNode)
        where T : SyntaxNode
    {
        if (hints.IsFormatCodeEnabled)
        {
            return newNode.WithLeadingTrivia(TrimEnd(prevNode.GetLeadingTrivia())).WithTrailingTrivia(prevNode.GetTrailingTrivia());
        }

        return newNode.WithLeadingTrivia(SyntaxFactory.Space);
    }

    public SyntaxToken PreserveTrivia(bool formatCode, SyntaxToken newToken, SyntaxToken prevToken)
    {
        if (formatCode)
        {
            return newToken.WithLeadingTrivia(TrimEnd(prevToken.LeadingTrivia)).WithTrailingTrivia(prevToken.TrailingTrivia);
        }

        return newToken.WithLeadingTrivia(SyntaxFactory.Space);
    }

    private static SyntaxTriviaList TrimEnd(SyntaxTriviaList trivia) =>
        new(trivia.SkipWhile(i => i.IsKind(SyntaxKind.WhitespaceTrivia)).Reverse().SkipWhile(i => i.IsKind(SyntaxKind.WhitespaceTrivia)).Reverse());
}