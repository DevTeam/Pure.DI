namespace Pure.DI.Core
{
    using System.Collections.Generic;
    using Microsoft.CodeAnalysis;
    using Microsoft.CodeAnalysis.CSharp;
    using Microsoft.CodeAnalysis.CSharp.Syntax;

    internal class TypeBindingStatementsStrategy: IBindingStatementsStrategy
    {
        private readonly SemanticModel _semanticModel;
        private readonly IBindingExpressionStrategy _bindingExpressionStrategy;

        public TypeBindingStatementsStrategy(SemanticModel semanticModel, IBindingExpressionStrategy bindingExpressionStrategy)
        {
            _semanticModel = semanticModel;
            _bindingExpressionStrategy = bindingExpressionStrategy;
        }

        public IEnumerable<StatementSyntax> CreateStatements(BindingMetadata binding, INamedTypeSymbol contractType)
        {
            var instance = _bindingExpressionStrategy.TryBuild(binding, contractType, null);
            yield return SyntaxFactory.ReturnStatement(instance);
        }
    }
}