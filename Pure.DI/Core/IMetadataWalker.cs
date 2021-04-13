namespace Pure.DI.Core
{
    using System.Collections.Generic;
    using Microsoft.CodeAnalysis;

    internal interface IMetadataWalker
    {
        IEnumerable<ResolverMetadata> Metadata { get; }

        void Visit(SyntaxNode? node);
    }
}