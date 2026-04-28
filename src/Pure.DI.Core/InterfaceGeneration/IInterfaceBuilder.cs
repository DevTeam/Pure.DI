namespace Pure.DI.InterfaceGeneration;

interface IInterfaceBuilder
{
    GeneratedInterfacesResult BuildInterfacesFor(
        SemanticModel semanticModel,
        ITypeSymbol typeSymbol,
        ClassDeclarationSyntax classSyntax);
}
