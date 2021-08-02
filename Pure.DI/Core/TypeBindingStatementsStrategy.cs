namespace Pure.DI.Core
{
    using System.Collections.Generic;
    using System.Collections.Immutable;
    using Microsoft.CodeAnalysis;
    using Microsoft.CodeAnalysis.CSharp;
    using Microsoft.CodeAnalysis.CSharp.Syntax;

    // ReSharper disable once ClassNeverInstantiated.Global
    internal class TypeBindingStatementsStrategy: IBindingStatementsStrategy
    {
        private readonly IDiagnostic _diagnostic;
        private readonly ITypeResolver _typeResolver;

        public TypeBindingStatementsStrategy(
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
            var instance = buildStrategy.TryBuild(_typeResolver.Resolve(dependency, null, dependency.Type.Locations), dependency);
            if (instance == null)
            {
                if (binding.Probe)
                {
                    yield break;
                }
                
                var fallbackType = dependency.SemanticModel.Compilation.GetTypeByMetadataName(typeof(IFallback).ToString());
                var hasFallback = false;
                if (fallbackType != null)
                {
                    hasFallback = _typeResolver.Resolve(new SemanticType(fallbackType, dependency.SemanticModel), null, ImmutableArray<Location>.Empty).IsResolved;
                }

                if (hasFallback)
                {
                    var error = $"Cannot resolve {binding}.";
                    _diagnostic.Warning(Diagnostics.Warning.CannotResolve, error);
                    yield break;
                }
                else
                {
                    var error = $"Cannot resolve {binding}.";
                    _diagnostic.Error(Diagnostics.Error.CannotResolve, error);
                    throw new HandledException(error);
                }
            }
            
            yield return SyntaxFactory.ReturnStatement(instance);
        }
    }
}