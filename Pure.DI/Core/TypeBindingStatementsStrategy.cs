namespace Pure.DI.Core
{
    using System.Collections.Generic;
    using Microsoft.CodeAnalysis;
    using Microsoft.CodeAnalysis.CSharp;
    using Microsoft.CodeAnalysis.CSharp.Syntax;

    internal class TypeBindingStatementsStrategy: IBindingStatementsStrategy
    {
        private readonly IBindingExpressionStrategy _bindingExpressionStrategy;
        private readonly ISet<BindingMetadata> _additionalBindings;

        public TypeBindingStatementsStrategy(IBindingExpressionStrategy bindingExpressionStrategy, ISet<BindingMetadata> additionalBindings)
        {
            _bindingExpressionStrategy = bindingExpressionStrategy;
            _additionalBindings = additionalBindings;
        }

        public IEnumerable<StatementSyntax> CreateStatements(BindingMetadata binding, INamedTypeSymbol contractType)
        {
            var instance = _bindingExpressionStrategy.TryBuild(binding, contractType, null, _additionalBindings);
            yield return SyntaxFactory.ReturnStatement(instance);
        }
    }
}