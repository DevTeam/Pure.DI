namespace Pure.DI.Core
{
    using System.Collections.Generic;
    using Microsoft.CodeAnalysis.CSharp.Syntax;

    internal interface IDisposeStatementsBuilder
    {
        IEnumerable<StatementSyntax> Build(MemberAccessExpressionSyntax instanceExpression, ExpressionSyntax hasInstanceExpression);
    }
}