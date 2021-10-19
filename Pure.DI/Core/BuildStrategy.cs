// ReSharper disable UnusedAutoPropertyAccessor.Local
// ReSharper disable MemberCanBePrivate.Global
// ReSharper disable UnusedAutoPropertyAccessor.Global
// ReSharper disable AutoPropertyCanBeMadeGetOnly.Global
// ReSharper disable MemberCanBePrivate.Local
namespace Pure.DI.Core
{
    using System.Collections.Generic;
    using System.Linq;
    using Microsoft.CodeAnalysis;
    using Microsoft.CodeAnalysis.CSharp.Syntax;
    using static Tags;

    // ReSharper disable once ClassNeverInstantiated.Global
    internal class BuildStrategy : IBuildStrategy
    {
        private readonly IDiagnostic _diagnostic;
        private readonly ITracer _tracer;
        private readonly ILog<BuildStrategy> _log;
        private readonly IBuildContext _buildContext;
        private readonly IBindingResultStrategy _resultStrategy;
        private readonly ICache<BuildStrategyKey, ExpressionSyntax?> _cache;
        private readonly Dictionary<Lifetime, ILifetimeStrategy> _lifetimes;

        public BuildStrategy(
            IDiagnostic diagnostic,
            ITracer tracer,
            ILog<BuildStrategy> log,
            IBuildContext buildContext,
            [Tag(AsIsResult)] IBindingResultStrategy resultStrategy,
            IEnumerable<ILifetimeStrategy> lifetimeStrategies,
            [Tag(ContainerScope)] ICache<BuildStrategyKey, ExpressionSyntax?> cache)
        {
            _diagnostic = diagnostic;
            _tracer = tracer;
            _log = log;
            _buildContext = buildContext;
            _resultStrategy = resultStrategy;
            _cache = cache;
            _lifetimes = lifetimeStrategies.ToDictionary(i => i.Lifetime, i => i);
        }
        
        public ExpressionSyntax? TryBuild(Dependency dependency, SemanticType resolvingType)
        {
            var key = new BuildStrategyKey(_buildContext.Id, dependency);
            if (_cache.TryGetValue(key, out var result))
            {
                _log.Trace(() => new []{ $"Cache: {dependency} => {result}"});
                return result;
            }

            using var traceToken = _tracer.RegisterResolving(dependency);
            var objectBuildExpression = dependency.ObjectBuilder.TryBuild(this, dependency);
            if (objectBuildExpression != null)
            {
                // Apply lifetime
                if (!_lifetimes.TryGetValue(dependency.Binding.Lifetime, out var lifetimeStrategy))
                {
                    var error = $"{dependency.Binding.Lifetime} lifetime is not supported.";
                    _diagnostic.Error(Diagnostics.Error.Unsupported, error, dependency.Implementation.Type.Locations.FirstOrDefault());
                    throw new HandledException(error);
                }

                objectBuildExpression = lifetimeStrategy.Build(resolvingType, dependency, objectBuildExpression);
                objectBuildExpression = _resultStrategy.Build(objectBuildExpression);
                _log.Info(() => new []{ $"{dependency} => {objectBuildExpression.NormalizeWhitespace()}"});
            }

            _cache.Add(key, objectBuildExpression);
            return objectBuildExpression;
        }
    }
}