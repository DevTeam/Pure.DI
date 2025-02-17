namespace Pure.DI.Core;

interface IApiInvocationProcessor
{
    void ProcessInvocation(
        IMetadataVisitor metadataVisitor,
        SemanticModel semanticModel,
        InvocationExpressionSyntax invocation,
        string @namespace);
}