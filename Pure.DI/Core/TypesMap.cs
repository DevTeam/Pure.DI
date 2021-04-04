namespace Pure.DI.Core
{
    using System.Collections.Generic;
    using Microsoft.CodeAnalysis;

    internal class TypesMap : ITypesMap
    {
        private readonly SemanticModel _semanticModel;
        private readonly Dictionary<INamedTypeSymbol, INamedTypeSymbol> _map = new(SymbolEqualityComparer.Default);

        public TypesMap(INamedTypeSymbol type, INamedTypeSymbol targetType, SemanticModel semanticModel)
        {
            _semanticModel = semanticModel;
            CreateMap(type, targetType);
        }

        public TypesMap(SemanticModel semanticModel)
        {
            _semanticModel = semanticModel;
        }

        public int Count => _map.Count;

        private void CreateMap(INamedTypeSymbol type, INamedTypeSymbol targetType)
        {
            if (!type.IsGenericType && type.Equals(targetType, SymbolEqualityComparer.Default))
            {
                return;
            }

            if (_map.ContainsKey(type))
            {
                return;
            }

            if (type.IsGenericTypeMarker(_semanticModel))
            {
                _map[type] = targetType;
                return;
            }

            // Constructed generic
            if (type.IsGenericType && targetType.IsGenericType)
            {
                if (type.ConstructUnboundGenericType().Equals(targetType.ConstructUnboundGenericType(), SymbolEqualityComparer.Default))
                {
                    _map[type] = targetType;
                    var typeArgs = type.TypeArguments;
                    var targetTypeArgs = targetType.TypeArguments;
                    for (var i = 0; i < typeArgs.Length; i++)
                    {
                        if (
                            typeArgs[i] is INamedTypeSymbol typeArg
                            && targetTypeArgs[i] is INamedTypeSymbol targetTypeArg)
                        {
                            CreateMap(typeArg, targetTypeArg);
                        }
                    }

                    return;
                }

                foreach (var implementedInterface in targetType.Interfaces)
                {
                    CreateMap(type, implementedInterface);
                }

                foreach (var implementedInterface in type.Interfaces)
                {
                    CreateMap(implementedInterface, targetType);
                }
            }
        }

        public INamedTypeSymbol ConstructType(INamedTypeSymbol type)
        {
            if (_map.TryGetValue(type, out var newType))
            {
                return newType;
            }

            if (!type.IsGenericType)
            {
                return type;
            }

            var args = new List<ITypeSymbol>();
            foreach (var arg in type.TypeArguments)
            {
                var typeArgument = arg;
                if (typeArgument is INamedTypeSymbol namedTypeArgument)
                {
                    typeArgument = ConstructType(namedTypeArgument);
                }

                args.Add(typeArgument);
            }

            return type.Construct(_semanticModel, args.ToArray());
        }

    }
}
