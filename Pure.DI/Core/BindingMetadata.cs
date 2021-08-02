namespace Pure.DI.Core
{
    using System.Collections.Generic;
    using System.Text;
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
        public readonly object Id;
        public bool AnyTag = false;
        public bool FromProbe;
        private static int _currentId;

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

        public override string ToString()
        {
            StringBuilder sb = new();
            foreach (var dependency in Dependencies)
            {
                sb.Append($"{nameof(IBinding.Bind)}<{dependency}>().");
            }

            if (Lifetime != Lifetime.Transient)
            {
                sb.Append($"{nameof(IBinding.As)}({Lifetime}).");
            }

            foreach (var tag in Tags)
            {
                sb.Append($"{nameof(IBinding.Tag)}<{tag.ToString()}>().");
            }

            sb.Append(Factory != null ? $"{nameof(IBinding.To)}({Factory.ToString()})" : $"{nameof(IBinding.To)}<{Implementation}>()");
            return sb.ToString();
        }
    }
}
