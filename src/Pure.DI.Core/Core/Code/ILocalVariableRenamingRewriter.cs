namespace Pure.DI.Core.Code;

interface ILocalVariableRenamingRewriter
{
    LambdaExpressionSyntax Rewrite(BuildContext ctx, LambdaExpressionSyntax lambda);
}