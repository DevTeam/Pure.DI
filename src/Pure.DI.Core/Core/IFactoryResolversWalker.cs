namespace Pure.DI.Core;

interface IFactoryResolversWalker
{
    IReadOnlyCollection<InvocationExpressionSyntax> Resolvers { get; }

    IReadOnlyCollection<InvocationExpressionSyntax> Initializers { get; }

    void Visit(SyntaxNode? node);
}