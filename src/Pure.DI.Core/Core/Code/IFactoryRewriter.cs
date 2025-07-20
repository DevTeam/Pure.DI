namespace Pure.DI.Core.Code;

interface IFactoryRewriter
{
    bool IsFinishMarkRequired { get; }

    IFactoryRewriter Initialize(
        DpFactory factory,
        VarInjection varInjection,
        string finishLabel,
        ICollection<FactoryRewriter.Injection> injections,
        ICollection<FactoryRewriter.Initializer> initializers);

    LambdaExpressionSyntax Rewrite(CodeContext ctx, LambdaExpressionSyntax lambda);
}