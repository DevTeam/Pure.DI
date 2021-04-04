namespace Pure.DI.Core
{
    using Microsoft.CodeAnalysis;
    using Microsoft.CodeAnalysis.CSharp.Syntax;

    internal class TypeResolveDescription
    {
        public readonly BindingMetadata Binding;
        public readonly ITypeSymbol Type;
        public readonly ExpressionSyntax? Tag;
        public readonly IObjectBuilder ObjectBuilder;
        public readonly ITypesMap TypesMap;
        public readonly SemanticModel SemanticModel;
        public readonly bool IsResolved;

        public TypeResolveDescription(
            BindingMetadata binding,
            ITypeSymbol type,
            ExpressionSyntax? tag,
            IObjectBuilder objectBuilder,
            ITypesMap typesMap,
            SemanticModel semanticModel,
            bool isResolved = true)
        {
            Binding = binding;
            Type = type;
            Tag = tag;
            ObjectBuilder = objectBuilder;
            TypesMap = typesMap;
            SemanticModel = semanticModel;
            IsResolved = isResolved;
        }

        public override string ToString() => $"{Type}: {IsResolved}";
    }
}
