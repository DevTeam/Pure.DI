namespace Pure.DI.Core;

internal interface ISyntaxRegistry
{
    MethodDeclarationSyntax FindMethod(string className, string methodName);
}