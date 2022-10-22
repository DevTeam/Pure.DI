// ReSharper disable All
namespace Pure.DI.Core;

using Core;

// ReSharper disable once ClassNeverInstantiated.Global
internal sealed class AsIsBindingResultStrategy : IBindingResultStrategy
{
    public ExpressionSyntax Build(ExpressionSyntax objectExpression) => objectExpression;
}