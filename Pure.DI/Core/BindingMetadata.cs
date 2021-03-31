namespace Pure.DI.Core
{
    using System.Collections.Generic;
    using Microsoft.CodeAnalysis;
    using Microsoft.CodeAnalysis.CSharp.Syntax;

    internal class BindingMetadata
    {
        [CanBeNull] public INamedTypeSymbol ImplementationType = null;
        [CanBeNull] public SimpleLambdaExpressionSyntax Factory = null;
        public Lifetime Lifetime = Lifetime.Transient;
        [NotNull] public readonly ISet<INamedTypeSymbol> ContractTypes = new HashSet<INamedTypeSymbol>(SymbolEqualityComparer.IncludeNullability);
        [NotNull] public readonly ISet<ExpressionSyntax> Tags = new HashSet<ExpressionSyntax>();

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj))
            {
                return false;
            }

            if (ReferenceEquals(this, obj))
            {
                return true;
            }

            if (obj.GetType() != this.GetType())
            {
                return false;
            }

            return Equals((BindingMetadata) obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                var hashCode = (ImplementationType != null ? SymbolEqualityComparer.IncludeNullability.GetHashCode(ImplementationType) : 0);
                hashCode = (hashCode * 397) ^ (int) Lifetime;
                return hashCode;
            }
        }

        private bool Equals(BindingMetadata other)
        {
            return
                ImplementationType.Equals(other.ImplementationType, SymbolEqualityComparer.IncludeNullability)
                && Lifetime == other.Lifetime
                && ContractTypes.SetEquals(other.ContractTypes)
                && Tags.SetEquals(other.Tags);
        }
    }
}
