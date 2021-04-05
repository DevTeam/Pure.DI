namespace Pure.DI.Core
{
    using System.Collections.Generic;
    using Microsoft.CodeAnalysis;
    using Microsoft.CodeAnalysis.CSharp.Syntax;

    internal class BindingMetadata
    {
        public ITypeSymbol? ImplementationType;
        public SimpleLambdaExpressionSyntax? Factory;
        public Lifetime Lifetime = Lifetime.Transient;
        public readonly ISet<ITypeSymbol> ContractTypes = new HashSet<ITypeSymbol>(SymbolEqualityComparer.Default);
        public readonly ISet<ExpressionSyntax> Tags = new HashSet<ExpressionSyntax>();

        public BindingMetadata() { }

        public BindingMetadata(BindingMetadata binding, ITypeSymbol constructedType)
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
