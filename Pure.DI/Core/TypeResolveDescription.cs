namespace Pure.DI.Core
{
    using Microsoft.CodeAnalysis;
    using Microsoft.CodeAnalysis.CSharp.Syntax;

    internal class TypeResolveDescription
    {
        public readonly BindingMetadata Binding;
        public readonly INamedTypeSymbol Type;
        public readonly ExpressionSyntax Tag;
        public readonly IObjectBuilder ObjectBuilder;
        public readonly ITypesMap TypesMap;
        public readonly SemanticModel SemanticModel;
        public readonly ITypeResolver TypeResolver;
        public readonly bool IsResolved;

        public TypeResolveDescription(
            BindingMetadata binding,
            INamedTypeSymbol type,
            ExpressionSyntax tag,
            IObjectBuilder objectBuilder,
            ITypesMap typesMap,
            SemanticModel semanticModel,
            ITypeResolver typeResolver,
            bool isResolved = true)
        {
            Binding = binding;
            Type = type;
            Tag = tag;
            ObjectBuilder = objectBuilder;
            TypesMap = typesMap;
            SemanticModel = semanticModel;
            TypeResolver = typeResolver;
            IsResolved = isResolved;
        }
    }
}
