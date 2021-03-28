namespace Pure.DI.Core
{
    using System.Collections.Generic;
    using Microsoft.CodeAnalysis;

    internal class TypeResolver : ITypeResolver
    {
        private readonly Dictionary<ITypeSymbol, ITypeSymbol> _map = new Dictionary<ITypeSymbol, ITypeSymbol>(SymbolEqualityComparer.Default);

        public TypeResolver(DIMetadata metadata)
        {
            foreach (var binding in metadata.Bindings)
            {
                foreach (var contractType in binding.ContractTypes)
                {
                    _map[contractType] = binding.ImplementationType;
                }
            }
        }

        public ITypeSymbol Resolve(ITypeSymbol typeSymbol)
        {
            if (_map.TryGetValue(typeSymbol, out var implementationType))
            {
                return implementationType;
            }

            return typeSymbol;
        }
    }
}
