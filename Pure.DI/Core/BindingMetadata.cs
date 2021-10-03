// ReSharper disable InvertIf
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
        public readonly object Id;
        public bool AnyTag = false;
        public bool FromProbe;
        private static int _currentId;
        private readonly ISet<SemanticType> _dependencies = new HashSet<SemanticType>(SemanticTypeEqualityComparer.Default);
        private readonly IDictionary<SemanticType, ISet<ExpressionSyntax>> _dependencyTags = new Dictionary<SemanticType, ISet<ExpressionSyntax>>(SemanticTypeEqualityComparer.Default);
        private readonly ISet<ExpressionSyntax> _tags = new HashSet<ExpressionSyntax>();

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

            _dependencies.Add(dependency);
            Implementation = binding.Implementation;
            Factory = binding.Factory;
            Location = binding.Location;
            foreach (var tag in binding._tags)
            {
                _tags.Add(tag);
            }

            _dependencyTags = new Dictionary<SemanticType, ISet<ExpressionSyntax>>(binding._dependencyTags);
        }

        public IEnumerable<SemanticType> Dependencies => _dependencies;

        public void AddDependency(SemanticType dependency) => _dependencies.Add(dependency);

        public IEnumerable<ExpressionSyntax> GetTags(SemanticType dependencyType)
        {
            var tags = new HashSet<ExpressionSyntax>(_tags);
            if (_dependencyTags.TryGetValue(dependencyType, out var dependencyTags))
            {
                foreach (var dependencyTag in dependencyTags)
                {
                    tags.Add(dependencyTag);
                }
            }

            return tags;
        }

        public void AddTags(params ExpressionSyntax[] tags)
        {
            foreach (var tag in tags)
            {
                _tags.Add(tag);
            }
        }

        public void AddDependencyTags(SemanticType dependency, params ExpressionSyntax[] tags)
        {
            if (!_dependencyTags.TryGetValue(dependency, out var curTags))
            {
                curTags = new HashSet<ExpressionSyntax>();
                _dependencyTags.Add(dependency, curTags);
            }

            foreach (var tag in tags)
            {
                curTags.Add(tag);
            }
        }

        public override string ToString()
        {
            StringBuilder sb = new();
            foreach (var dependency in _dependencies)
            {
                var tags = string.Empty;
                if (_dependencyTags.TryGetValue(dependency, out var dependencyTags))
                {
                    tags = string.Join(", ", dependencyTags);
                }

                sb.Append($"{nameof(IBinding.Bind)}<{dependency}>({tags}).");
            }

            if (Lifetime != Lifetime.Transient)
            {
                sb.Append($"{nameof(IBinding.As)}({Lifetime}).");
            }

            foreach (var tag in _tags)
            {
                sb.Append($"{nameof(IBinding.Tags)}<{tag.ToString()}>().");
            }

            sb.Append(Factory != null ? $"{nameof(IBinding.To)}({Factory.ToString()})" : $"{nameof(IBinding.To)}<{Implementation}>()");
            return sb.ToString();
        }
    }
}
