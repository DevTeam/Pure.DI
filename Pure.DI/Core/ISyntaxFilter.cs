namespace Pure.DI.Core
{
    using System;
    using Microsoft.CodeAnalysis;

    internal interface ISyntaxFilter
    {
        IComparable Order { get; }

        bool Accept(SyntaxNode node);
    }
}