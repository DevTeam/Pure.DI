namespace Pure.DI.InterfaceGeneration;

interface IInterfaceBuilder
{
    Lines BuildInterfaceFor(
        SemanticModel semanticModel,
        ITypeSymbol typeSymbol,
        ClassDeclarationSyntax classSyntax);
}