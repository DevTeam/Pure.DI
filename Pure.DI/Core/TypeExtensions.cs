namespace Pure.DI.Core
{
    using System;
    using System.Linq;
    using Microsoft.CodeAnalysis;
    using Microsoft.CodeAnalysis.CSharp;
    using Microsoft.CodeAnalysis.CSharp.Syntax;

    internal static class TypeExtensions
    {
        public static TypeSyntax ToTypeSyntax(this ITypeSymbol typeSymbol, SemanticModel semanticModel) =>
            SyntaxFactory.ParseTypeName(typeSymbol.ToMinimalDisplayString(semanticModel, 0));

        public static bool Equals(this Type type, ITypeSymbol typeSymbol, SemanticModel semanticModel) => 
            SymbolEqualityComparer.Default.Equals(ToTypeSymbol(type, semanticModel), typeSymbol);

        public static bool IsValidTypeToResolve(this INamedTypeSymbol typeSymbol, SemanticModel semanticModel) =>
            !typeSymbol.IsUnboundGenericType && !typeSymbol.IsComposedGenericTypeMarker(semanticModel);

        public static INamedTypeSymbol Construct(this INamedTypeSymbol typeSymbol, SemanticModel semanticModel, params ITypeSymbol[] typeArgSymbols)
        {
            if (!typeSymbol.IsGenericType)
            {
                throw new ArgumentException(nameof(typeSymbol), $"{typeSymbol} is nt generic type.");
            }

            var unboundGeneric = typeSymbol.ConstructUnboundGenericType();
            var typeName = string.Join("", unboundGeneric.ToDisplayParts().TakeWhile(i => i.ToString() != "<")) + "`" + typeSymbol.TypeArguments.Length;
            unboundGeneric = semanticModel.Compilation.GetTypeByMetadataName(typeName);
            if (unboundGeneric == null)
            {
                throw new InvalidOperationException($"Cannot construct {typeSymbol}");
            }

            return unboundGeneric.Construct(typeArgSymbols);
        }


        public static bool IsComposedGenericTypeMarker(this INamedTypeSymbol typeSymbol, SemanticModel semanticModel)
        {
            if (IsGenericTypeMarker(typeSymbol, semanticModel))
            {
                return true;
            }

            if (!typeSymbol.IsGenericType)
            {
                return false;
            }

            return
                typeSymbol
                    .TypeArguments
                    .OfType<INamedTypeSymbol>()
                    .Any(type => IsComposedGenericTypeMarker(type, semanticModel));
        }

        public static bool IsGenericTypeMarker(this INamedTypeSymbol typeSymbol, SemanticModel semanticModel)
        {
            foreach (var attr in typeSymbol.GetAttributes())
            {
                if (
                    attr.AttributeClass != null
                    && typeof(GenericTypeArgumentAttribute).Equals(attr.AttributeClass, semanticModel))
                {
                    return true;
                }
            }

            return false;
        }

        private static INamedTypeSymbol ToTypeSymbol(this Type type, SemanticModel semanticModel)
        {
            if (type.FullName == null)
            {
                throw new ArgumentException(nameof(type));
            }

            if (!type.IsConstructedGenericType)
            {
                return semanticModel.Compilation.GetTypeByMetadataName(type.FullName) ?? throw new InvalidOperationException("Cannot convert type.");
            }

            var typeArgumentsTypeInfos = type.GenericTypeArguments.Select(a => ToTypeSymbol(a, semanticModel));
            var openType = type.GetGenericTypeDefinition();
            var typeSymbol = semanticModel.Compilation.GetTypeByMetadataName(openType.FullName ?? throw new InvalidOperationException("Cannot get generic type name."));
            return typeSymbol?.Construct(typeArgumentsTypeInfos.ToArray<ITypeSymbol>()) ?? throw new InvalidOperationException("Cannot convert generic type.");
        }
    }
}
