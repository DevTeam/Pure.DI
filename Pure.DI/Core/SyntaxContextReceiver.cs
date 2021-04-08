namespace Pure.DI.Core
{
    using System.Collections.Generic;
    using System.Linq;
    using Microsoft.CodeAnalysis;

    // ReSharper disable once ClassNeverInstantiated.Global
    internal class SyntaxContextReceiver : ISyntaxContextReceiver, IGeneratorTargets
    {
        private readonly HashSet<SyntaxTree> _trees = new();

        public IReadOnlyCollection<SyntaxTree> Trees => _trees;

        public void OnVisitSyntaxNode(GeneratorSyntaxContext context)
        {
            var tree = context.Node.SyntaxTree;
            if(!_trees.Contains(tree) && ContainsMetadata(tree.GetRoot(), context.SemanticModel))
            {
                _trees.Add(tree);
            }
        }

        private bool ContainsMetadata(SyntaxNode tree, SemanticModel semanticModel)
        {
            var walker = new MetadataWalker(semanticModel);
            walker.Visit(tree);
            return walker.Metadata.Any();
        }
    }
}