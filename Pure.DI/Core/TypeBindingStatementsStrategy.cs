namespace Pure.DI.Core
{
    using System.Collections.Generic;
    using Microsoft.CodeAnalysis;
    using Microsoft.CodeAnalysis.CSharp;
    using Microsoft.CodeAnalysis.CSharp.Syntax;

    // ReSharper disable once ClassNeverInstantiated.Global
    internal class TypeBindingStatementsStrategy: IBindingStatementsStrategy
    {
        public IEnumerable<StatementSyntax> CreateStatements(
            IBindingExpressionStrategy bindingExpressionStrategy,
            BindingMetadata binding,
            ITypeSymbol contractType)
        {
            var instance = bindingExpressionStrategy.TryBuild(contractType, null);
            yield return SyntaxFactory.ReturnStatement(instance);
        }
    }
}