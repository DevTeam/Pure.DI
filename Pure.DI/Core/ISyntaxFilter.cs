namespace Pure.DI.Core;

internal interface ISyntaxFilter
{
    IComparable Order { get; }

    bool Accept(SyntaxNode node);
}