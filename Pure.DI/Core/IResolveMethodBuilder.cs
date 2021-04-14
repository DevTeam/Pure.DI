namespace Pure.DI.Core
{
    using Microsoft.CodeAnalysis;

    internal interface IResolveMethodBuilder
    {
        ResolveMethod Build(SemanticModel semanticModel);
    }
}