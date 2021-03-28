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

        private static INamedTypeSymbol ToTypeSymbol(this Type type, SemanticModel semanticModel)
        {
            if (!type.IsConstructedGenericType)
            {
                return semanticModel.Compilation.GetTypeByMetadataName(type.FullName);
            }

            var typeArgumentsTypeInfos = type.GenericTypeArguments.Select(a => ToTypeSymbol(a, semanticModel));
            var openType = type.GetGenericTypeDefinition();
            var typeSymbol = semanticModel.Compilation.GetTypeByMetadataName(openType.FullName);
            return typeSymbol.Construct(typeArgumentsTypeInfos.ToArray<ITypeSymbol>());
        }
    }
}
