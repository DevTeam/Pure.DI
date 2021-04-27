// ReSharper disable LoopCanBeConvertedToQuery
namespace Pure.DI.Core
{
    using System.Collections.Generic;
    using System.Linq;
    using Microsoft.CodeAnalysis.CSharp;
    using Microsoft.CodeAnalysis.CSharp.Syntax;

    // ReSharper disable once ClassNeverInstantiated.Global
    internal class TypeAndTagBindingStatementsStrategy : IBindingStatementsStrategy
    {
        private readonly ITypeResolver _typeResolver;

        public TypeAndTagBindingStatementsStrategy(ITypeResolver typeResolver) =>
            _typeResolver = typeResolver;

        public IEnumerable<StatementSyntax> CreateStatements(IBuildStrategy buildStrategy,
            BindingMetadata binding,
            SemanticType dependency)
        {
            var instance = buildStrategy.Build(_typeResolver.Resolve(dependency, binding.Tags.First(), dependency.Type.Locations));
            yield return SyntaxFactory.ReturnStatement(instance);
        }
    }
}