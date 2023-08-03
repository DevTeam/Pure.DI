namespace Pure.DI.Core;

internal interface IApiInvocationProcessor
{
    void ProcessInvocation(
        IMetadataVisitor metadataVisitor,
        SemanticModel semanticModel,
        InvocationExpressionSyntax invocation,
        string @namespace);
}