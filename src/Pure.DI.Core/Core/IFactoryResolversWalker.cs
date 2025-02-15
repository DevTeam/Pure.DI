namespace Pure.DI.Core;

internal interface IFactoryResolversWalker
{
    IReadOnlyCollection<InvocationExpressionSyntax> Resolvers { get; }

    IReadOnlyCollection<InvocationExpressionSyntax> Initializers { get; }

    void Visit(SyntaxNode? node);
}