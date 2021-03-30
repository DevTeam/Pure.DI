namespace Pure.DI.Core
{
    using System.Collections.Generic;
    using System.Linq;
    using Microsoft.CodeAnalysis;

    internal class MetadataReceiver : ISyntaxContextReceiver
    {
        private List<ResolverMetadata> _metadata = new List<ResolverMetadata>();

        public void OnVisitSyntaxNode(GeneratorSyntaxContext context)
        {
            var walker = new MetadataWalker(context.SemanticModel);
            walker.Visit(context.Node);
            if (walker.Metadata.Any(i => i.Bindings.Any()))
            {
                _metadata.Clear();
                _metadata.AddRange(walker.Metadata);
            }
        }

        public IReadOnlyCollection<ResolverMetadata> Metadata => _metadata;
    }
}