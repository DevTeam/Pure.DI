namespace Pure.DI.Core;

internal interface ISyntaxFilter
{
    bool Accept(SyntaxNode node);
}