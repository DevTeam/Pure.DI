namespace Pure.DI.InterfaceGeneration;

interface IInterfaceBuilder
{
    string BuildInterfaceFor(
        SemanticModel semanticModel,
        ITypeSymbol typeSymbol,
        ClassDeclarationSyntax classSyntax);
}