namespace Pure.DI.InterfaceGeneration;

interface IInterfaceBuilder
{
    string BuildInterfaceFor(ITypeSymbol typeSymbol, ClassDeclarationSyntax classSyntax);
}