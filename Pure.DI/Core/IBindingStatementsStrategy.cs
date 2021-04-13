namespace Pure.DI.Core
{
    using System.Collections.Generic;
    using Microsoft.CodeAnalysis.CSharp.Syntax;

    internal interface IBindingStatementsStrategy
    {
        IEnumerable<StatementSyntax> CreateStatements(
            IBuildStrategy buildStrategy,
            BindingMetadata binding,
            SemanticType dependency);
    }
}