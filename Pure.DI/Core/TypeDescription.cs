namespace Pure.DI.Core
{
    using Microsoft.CodeAnalysis;
    using Microsoft.CodeAnalysis.CSharp.Syntax;

    internal readonly struct TypeDescription
    {
        public readonly BindingMetadata Binding;
        public readonly ITypeSymbol Type;
        public readonly ExpressionSyntax? Tag;
        public readonly IObjectBuilder ObjectBuilder;
        public readonly ITypesMap TypesMap;
        public readonly SemanticModel SemanticModel;
        public readonly bool IsResolved;
        public readonly int Index;

        public TypeDescription(
            BindingMetadata binding,
            ITypeSymbol type,
            ExpressionSyntax? tag,
            IObjectBuilder objectBuilder,
            ITypesMap typesMap,
            SemanticModel semanticModel,
            bool isResolved = true,
            int index = 0)
        {
            Binding = binding;
            Type = type;
            Tag = tag;
            ObjectBuilder = objectBuilder;
            TypesMap = typesMap;
            SemanticModel = semanticModel;
            IsResolved = isResolved;
            Index = index;
        }

        public override string ToString() => $"{Type}({Tag})";
    }
}
