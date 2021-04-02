namespace Pure.DI.Core
{
    using Microsoft.CodeAnalysis;

    internal class TypeResolveDescription
    {
        public readonly BindingMetadata Binding;
        public readonly INamedTypeSymbol Type;
        public readonly IObjectBuilder ObjectBuilder;
        public readonly ITypesMap TypesMap;
        public readonly SemanticModel SemanticModel;
        public readonly bool IsResolved;

        public TypeResolveDescription(
            BindingMetadata binding,
            INamedTypeSymbol type,
            IObjectBuilder objectBuilder,
            ITypesMap typesMap,
            SemanticModel semanticModel,
            bool isResolved = true)
        {
            Binding = binding;
            Type = type;
            ObjectBuilder = objectBuilder;
            TypesMap = typesMap;
            SemanticModel = semanticModel;
            IsResolved = isResolved;
        }
    }
}
