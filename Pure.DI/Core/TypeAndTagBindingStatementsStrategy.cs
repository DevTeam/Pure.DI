// ReSharper disable LoopCanBeConvertedToQuery
namespace Pure.DI.Core
{
    using System.Collections.Generic;
    using System.Collections.Immutable;
    using System.Linq;
    using Microsoft.CodeAnalysis;
    using Microsoft.CodeAnalysis.CSharp;
    using Microsoft.CodeAnalysis.CSharp.Syntax;

    // ReSharper disable once ClassNeverInstantiated.Global
    internal class TypeAndTagBindingStatementsStrategy : IBindingStatementsStrategy
    {
        private readonly IDiagnostic _diagnostic;
        private readonly ITypeResolver _typeResolver;

        public TypeAndTagBindingStatementsStrategy(
            IDiagnostic diagnostic,
            ITypeResolver typeResolver)
        {
            _diagnostic = diagnostic;
            _typeResolver = typeResolver;
        }

        public IEnumerable<StatementSyntax> CreateStatements(
            IBuildStrategy buildStrategy,
            BindingMetadata binding,
            SemanticType dependency)
        {
            var instance = buildStrategy.TryBuild(_typeResolver.Resolve(dependency, binding.Tags.First(), dependency.Type.Locations), dependency);
            if (instance == null)
            {
                if (binding.FromProbe)
                {
                    yield break;
                }

                var error = $"Cannot resolve {binding.Implementation}.";
                _diagnostic.Error(Diagnostics.Error.CannotResolve, error);
                throw new HandledException(error);
            }

            yield return SyntaxFactory.ReturnStatement(instance);
        }
    }
}