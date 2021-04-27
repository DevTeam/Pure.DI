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
        private static int _currentId;
        public object Id;

        public BindingMetadata(object? id = null)
        {
            Id = id ?? System.Threading.Interlocked.Increment(ref _currentId);
        }

        public BindingMetadata(BindingMetadata binding, SemanticType dependency, object? id)
            :this(id)
        {
            if (id == null)
            {
                Id = binding.Id;
            }
            
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
