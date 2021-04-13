namespace Pure.DI.Core
{
    using System.Collections.Generic;
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
            SemanticType dependency)
        {
            var instance = buildStrategy.Build(_typeResolver.Resolve(dependency, null));
            yield return SyntaxFactory.ReturnStatement(instance);
        }
    }
}