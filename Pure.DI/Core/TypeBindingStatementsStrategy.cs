namespace Pure.DI.Core
{
    using System.Collections.Generic;
    using Microsoft.CodeAnalysis;
    using Microsoft.CodeAnalysis.CSharp;
    using Microsoft.CodeAnalysis.CSharp.Syntax;

    internal class TypeBindingStatementsStrategy: IBindingStatementsStrategy
    {
        private readonly IBindingExpressionStrategy _bindingExpressionStrategy;

        public TypeBindingStatementsStrategy(IBindingExpressionStrategy bindingExpressionStrategy)
        {
            _bindingExpressionStrategy = bindingExpressionStrategy;
        }

        public IEnumerable<StatementSyntax> CreateStatements(BindingMetadata binding, INamedTypeSymbol contractType)
        {
            var instance = _bindingExpressionStrategy.TryBuild(binding, contractType, null, new List<BindingMetadata>());
            yield return SyntaxFactory.ReturnStatement(instance);
        }
    }
}