namespace Pure.DI.Core
{
    using System.Collections.Generic;
    using Microsoft.CodeAnalysis.CSharp.Syntax;

    internal interface IObjectBuilder
    {
        ExpressionSyntax TryBuild(
            ITypeResolver typeResolver,
            IBindingExpressionStrategy bindingExpressionStrategy,
            TypeResolveDescription typeDescription,
            ISet<BindingMetadata> additionalBindings,
            int level = 0);
    }
}