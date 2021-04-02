namespace Pure.DI.Core
{
    using System.Collections.Generic;
    using Microsoft.CodeAnalysis;
    using Microsoft.CodeAnalysis.CSharp.Syntax;

    internal class BindingMetadata
    {
        public INamedTypeSymbol? ImplementationType;
        public SimpleLambdaExpressionSyntax? Factory;
        public Lifetime Lifetime = Lifetime.Transient;
        public readonly ISet<INamedTypeSymbol> ContractTypes = new HashSet<INamedTypeSymbol>(SymbolEqualityComparer.Default);
        public readonly ISet<ExpressionSyntax> Tags = new HashSet<ExpressionSyntax>();

        public BindingMetadata() { }

        public BindingMetadata(BindingMetadata binding, INamedTypeSymbol constructedType)
        {
            ContractTypes.Add(constructedType);
            ImplementationType = binding.ImplementationType;
            Factory = binding.Factory;
            foreach (var tag in binding.Tags)
            {
                Tags.Add(tag);
            }
        }
    }
}
