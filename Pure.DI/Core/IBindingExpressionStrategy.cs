namespace Pure.DI.Core
{
    using System.Collections.Generic;
    using Microsoft.CodeAnalysis;
    using Microsoft.CodeAnalysis.CSharp.Syntax;

    internal interface IBindingExpressionStrategy
    {
        ExpressionSyntax TryBuild(
            BindingMetadata binding,
            INamedTypeSymbol contractType,
            ExpressionSyntax? tag,
            ISet<BindingMetadata> additionalBindings);
    }
}