namespace Pure.DI.Core
{
    using System.Collections.Generic;
    using Microsoft.CodeAnalysis;

    internal interface IMetadataWalker
    {
        IReadOnlyCollection<ResolverMetadata> Metadata { get; }
        void Visit(SyntaxNode? node);
    }
}