namespace Pure.DI.Core;

internal interface ICompilationUnitSyntaxBuilder
{
    CompilationUnitSyntax CreateRootNode(ClassDeclarationSyntax resolverClass);
}