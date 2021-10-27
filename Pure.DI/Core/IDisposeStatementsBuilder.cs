namespace Pure.DI.Core
{
    using System.Collections.Generic;
    using Microsoft.CodeAnalysis.CSharp.Syntax;

    internal interface IDisposeStatementsBuilder
    {
        IEnumerable<StatementSyntax> Build(SemanticType type, MemberAccessExpressionSyntax instanceExpression, ExpressionSyntax hasInstanceExpression);
    }
}