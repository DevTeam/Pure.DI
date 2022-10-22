namespace Pure.DI.Core;

internal sealed class ResolveMethod
{
    public readonly MethodDeclarationSyntax TargetMethod;
    public readonly StatementSyntax[] PostStatements;

    public ResolveMethod(
        MethodDeclarationSyntax targetMethod,
        params StatementSyntax[] postStatements)
    {
        TargetMethod = targetMethod;
        PostStatements = postStatements;
    }
}