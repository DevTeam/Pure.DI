namespace Pure.DI.Core
{
    using System.Collections.Generic;
    using Microsoft.CodeAnalysis.CSharp.Syntax;

    internal interface IDefaultValueStrategy
    {
        ExpressionSyntax Build(
            ICollection<FactoryMetadata> metadata,
            ExpressionSyntax typeExpression,
            ExpressionSyntax tagExpression,
            ExpressionSyntax defaultValue);
    }
}