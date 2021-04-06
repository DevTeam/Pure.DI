namespace Pure.DI.Core
{
    using Microsoft.CodeAnalysis.CSharp.Syntax;

    internal interface IObjectBuilder
    {
        ExpressionSyntax TryBuild(
            ITypeResolver typeResolver,
            IBindingExpressionStrategy bindingExpressionStrategy,
            TypeResolveDescription typeDescription);
    }
}