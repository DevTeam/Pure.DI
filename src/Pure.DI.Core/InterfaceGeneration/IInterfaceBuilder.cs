namespace Pure.DI.InterfaceGeneration;

interface IInterfaceBuilder
{
    ImmutableArray<GeneratedInterfaceSource> BuildInterfacesFor(
        SemanticModel semanticModel,
        ITypeSymbol typeSymbol,
        ClassDeclarationSyntax classSyntax);
}
