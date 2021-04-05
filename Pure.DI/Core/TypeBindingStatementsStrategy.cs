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

        public IEnumerable<StatementSyntax> CreateStatements(BindingMetadata binding, ITypeSymbol contractType)
        {
            var instance = _bindingExpressionStrategy.TryBuild(contractType, null, _additionalBindings);
            yield return SyntaxFactory.ReturnStatement(instance);
        }
    }
}