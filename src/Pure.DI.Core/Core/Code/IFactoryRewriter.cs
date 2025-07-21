namespace Pure.DI.Core.Code;

interface IFactoryRewriter
{
    bool IsFinishMarkRequired { get; }

    LambdaExpressionSyntax Rewrite(CodeContext codeCtx, LambdaExpressionSyntax lambda);
}