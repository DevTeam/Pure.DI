namespace Pure.DI.Core
{
    using System.Collections.Generic;
    using Microsoft.CodeAnalysis;
    using Microsoft.CodeAnalysis.CSharp.Syntax;

    internal class TypesMap : ITypesMap
    {
        private readonly SemanticModel _semanticModel;
        private readonly Dictionary<ITypeSymbol, ITypeSymbol> _map = new(SymbolEqualityComparer.Default);

        public TypesMap(ITypeSymbol type, ITypeSymbol targetType, SemanticModel semanticModel)
        {
            _semanticModel = semanticModel;
            CreateMap(type, targetType);
        }

        public TypesMap(SemanticModel semanticModel)
        {
            _semanticModel = semanticModel;
        }

        public int Count => _map.Count;

        private void CreateMap(ITypeSymbol type, ITypeSymbol targetType)
        {
            if (
                type is INamedTypeSymbol namedType
                && targetType is INamedTypeSymbol targetNamedType)
            {
                if (!namedType.IsGenericType && type.Equals(targetType, SymbolEqualityComparer.Default))
                {
                    return;
                }

                if (_map.ContainsKey(type))
                {
                    return;
                }

                if (namedType.IsGenericTypeMarker(_semanticModel))
                {
                    _map[type] = targetType;
                    return;
                }

                // Constructed generic
                if (namedType.IsGenericType && targetNamedType.IsGenericType)
                {
                    if (namedType.ConstructUnboundGenericType().Equals(targetNamedType.ConstructUnboundGenericType(), SymbolEqualityComparer.Default))
                    {
                        _map[type] = targetType;
                        var typeArgs = namedType.TypeArguments;
                        var targetTypeArgs = targetNamedType.TypeArguments;
                        if (typeArgs.Length == targetTypeArgs.Length)
                        {
                            for (var i = 0; i < typeArgs.Length; i++)
                            {
                                CreateMap(typeArgs[i], targetTypeArgs[i]);
                            }
                        }

                        return;
                    }
                }
            }

            if (
                type is IArrayTypeSymbol namedArrayType
                && targetType is IArrayTypeSymbol targetArrayType)
            {
                _map[namedArrayType.ElementType] = targetArrayType.ElementType;
                CreateMap(namedArrayType.ElementType, targetArrayType.ElementType);
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

        public ITypeSymbol ConstructType(ITypeSymbol type)
        {
            if (_map.TryGetValue(type, out var newType))
            {
                return newType;
            }

            if (type is INamedTypeSymbol namedType)
            {
                if (!namedType.IsGenericType)
                {
                    return type;
                }

                var args = new List<ITypeSymbol>();
                foreach (var arg in namedType.TypeArguments)
                {
                    args.Add(ConstructType(arg));
                }

                return namedType.Construct(_semanticModel, args.ToArray());
            }

            if (type is IArrayTypeSymbol arrayTypeSymbol)
            {
                return _semanticModel.Compilation.CreateArrayTypeSymbol(ConstructType(arrayTypeSymbol.ElementType), arrayTypeSymbol.Rank);
            }

            return type;
        }
    }
}
