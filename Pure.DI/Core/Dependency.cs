// ReSharper disable MemberCanBePrivate.Global
namespace Pure.DI.Core
{
    using Microsoft.CodeAnalysis.CSharp.Syntax;

    internal readonly struct Dependency
    {
        public readonly BindingMetadata Binding;
        public readonly SemanticType Implementation;
        public readonly ExpressionSyntax? Tag;
        public readonly IObjectBuilder ObjectBuilder;
        public readonly ITypesMap TypesMap;
        public readonly bool IsResolved;
        
        public Dependency(
            BindingMetadata binding,
            SemanticType implementation,
            ExpressionSyntax? tag,
            IObjectBuilder objectBuilder,
            ITypesMap typesMap,
            bool isResolved = true)
        {
            Binding = binding;
            Implementation = implementation;
            Tag = tag;
            ObjectBuilder = objectBuilder;
            TypesMap = typesMap;
            IsResolved = isResolved;
        }

        public override string ToString()
        {
            var tag = Tag != null ? $"({Tag})" : string.Empty;
            return $"{Implementation}{tag}";
        }

        public bool Equals(Dependency other) => Implementation.Equals(other.Implementation) && Equals(Tag?.ToString(), other.Tag?.ToString());

        public override bool Equals(object? obj) => obj is Dependency other && Equals(other);

        public override int GetHashCode()
        {
            unchecked
            {
                return (Implementation.GetHashCode() * 397) ^ (Tag != null ? Tag.ToString().GetHashCode() : 0);
            }
        }
    }
}
