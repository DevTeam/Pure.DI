namespace Pure.DI.Core
{
    using System.Collections.Generic;
    using Microsoft.CodeAnalysis.CSharp.Syntax;

    internal interface IDefaultValueStrategy
    {
        StatementSyntax Build(
            ICollection<FactoryMetadata> metadata,
            TypeSyntax? targetType,
            ExpressionSyntax typeExpression,
            ExpressionSyntax tagExpression);
    }
}