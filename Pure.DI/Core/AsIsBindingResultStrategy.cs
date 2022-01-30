// ReSharper disable All
namespace Pure.DI.Core
{
    // ReSharper disable once ClassNeverInstantiated.Global
    class AsIsBindingResultStrategy : IBindingResultStrategy
    {
        public ExpressionSyntax Build(ExpressionSyntax objectExpression) => objectExpression;
    }
}