namespace Pure.DI.Core
{
    using System.Collections.Generic;
    using Microsoft.CodeAnalysis;
    using Microsoft.CodeAnalysis.CSharp.Syntax;

    internal class BindingMetadata
    {
        public Location? Location;
        public SemanticType? Implementation;
        public SimpleLambdaExpressionSyntax? Factory;
        public Lifetime Lifetime = Lifetime.Transient;
        public readonly ISet<SemanticType> Dependencies = new HashSet<SemanticType>(SemanticTypeEqualityComparer.Default);
        public readonly ISet<ExpressionSyntax> Tags = new HashSet<ExpressionSyntax>();
        public int Weight = 0;

        public BindingMetadata() { }

        public BindingMetadata(BindingMetadata binding, SemanticType dependency)
        {
            Dependencies.Add(dependency);
            Implementation = binding.Implementation;
            Factory = binding.Factory;
            Location = binding.Location;
            foreach (var tag in binding.Tags)
            {
                Tags.Add(tag);
            }
        }
    }
}
