namespace Pure.DI.Core
{
    using Microsoft.CodeAnalysis.CSharp.Syntax;

    internal interface ISyntaxRegistry
    {
        MethodDeclarationSyntax FindMethod(string className, string methodName);
    }
}