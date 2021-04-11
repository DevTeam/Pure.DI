namespace Pure.DI.Core
{
    using Microsoft.CodeAnalysis;
    using Microsoft.CodeAnalysis.CSharp.Syntax;

    internal interface IBindingExpressionStrategy
    {
        ExpressionSyntax TryBuild(ITypeSymbol contractType, ExpressionSyntax? tag);

        ExpressionSyntax TryBuild(TypeDescription typeDescription);
    }
}