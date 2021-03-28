namespace Pure.DI.Core
{
    using System.Collections.Generic;
    using Microsoft.CodeAnalysis;
    using Microsoft.CodeAnalysis.CSharp.Syntax;

    internal class BindingMetadata
    {
        [CanBeNull] public ITypeSymbol ImplementationType = null;
        public Lifetime Lifetime = Lifetime.Transient;
        [NotNull] public readonly ICollection<ITypeSymbol> ContractTypes = new HashSet<ITypeSymbol>(SymbolEqualityComparer.Default);
        [NotNull] public readonly ICollection<ExpressionSyntax> Tags = new List<ExpressionSyntax>();
    }
}
