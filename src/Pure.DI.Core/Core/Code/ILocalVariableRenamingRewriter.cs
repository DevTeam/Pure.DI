namespace Pure.DI.Core.Code;

internal interface ILocalVariableRenamingRewriter
{
    LambdaExpressionSyntax Rewrite(BuildContext ctx, LambdaExpressionSyntax lambda);
}