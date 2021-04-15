namespace Pure.DI.Core
{
    using Microsoft.CodeAnalysis.CSharp.Syntax;

    internal class ResolveMethod
    {
        public MethodDeclarationSyntax TargetMethod;
        public readonly StatementSyntax[] PostStatements;

        public ResolveMethod(
            MethodDeclarationSyntax targetMethod,
            params StatementSyntax[] postStatements)
        {
            TargetMethod = targetMethod;
            PostStatements = postStatements;
        }
    }
}