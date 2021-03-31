namespace Pure.DI.Core
{
    using Microsoft.CodeAnalysis;

    internal class TypeResolveDescription
    {
        public readonly BindingMetadata Binding;
        public readonly INamedTypeSymbol TypeSymbol;
        public readonly IObjectBuilder ObjectBuilder;

        public TypeResolveDescription(BindingMetadata binding, INamedTypeSymbol typeSymbol, IObjectBuilder objectBuilder)
        {
            Binding = binding;
            TypeSymbol = typeSymbol;
            ObjectBuilder = objectBuilder;
        }
    }
}
