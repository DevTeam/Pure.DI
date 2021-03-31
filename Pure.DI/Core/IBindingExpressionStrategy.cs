namespace Pure.DI.Core
{
    using Microsoft.CodeAnalysis;
    using Microsoft.CodeAnalysis.CSharp.Syntax;

    internal interface IBindingExpressionStrategy
    {
        ExpressionSyntax TryBuild(
            BindingMetadata binding,
            INamedTypeSymbol contractType,
            ExpressionSyntax tag);
    }
}