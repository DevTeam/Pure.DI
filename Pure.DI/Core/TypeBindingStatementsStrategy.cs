namespace Pure.DI.Core
{
    using System.Collections.Generic;
    using Microsoft.CodeAnalysis;
    using Microsoft.CodeAnalysis.CSharp;
    using Microsoft.CodeAnalysis.CSharp.Syntax;

    // ReSharper disable once ClassNeverInstantiated.Global
    internal class TypeBindingStatementsStrategy: IBindingStatementsStrategy
    {
        private readonly ITypeResolver _typeResolver;

        public TypeBindingStatementsStrategy(ITypeResolver typeResolver) =>
            _typeResolver = typeResolver;

        public IEnumerable<StatementSyntax> CreateStatements(
            IBuildStrategy buildStrategy,
            BindingMetadata binding,
            ITypeSymbol contractType)
        {
            var instanceTypeDescriptor = _typeResolver.Resolve(contractType, null);
            var instance = buildStrategy.Build(instanceTypeDescriptor);
            yield return SyntaxFactory.ReturnStatement(instance);
        }
    }
}