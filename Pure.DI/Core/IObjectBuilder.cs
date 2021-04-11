namespace Pure.DI.Core
{
    using Microsoft.CodeAnalysis.CSharp.Syntax;

    internal interface IObjectBuilder
    {
        ExpressionSyntax TryBuild(IBindingExpressionStrategy bindingExpressionStrategy, TypeResolveDescription typeDescription);
    }
}