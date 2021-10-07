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
        private readonly IBindingResultStrategy _resultStrategy;
        private readonly Dictionary<Lifetime, ILifetimeStrategy> _lifetimes;
        private readonly Dictionary<CacheKey, ExpressionSyntax?> _cache = new();

        public BuildStrategy(
            IDiagnostic diagnostic,
            ITracer tracer,
            ILog<BuildStrategy> log,
            [Tag(AsIsResult)] IBindingResultStrategy resultStrategy,
            IEnumerable<ILifetimeStrategy> lifetimeStrategies)
        {
            _diagnostic = diagnostic;
            _tracer = tracer;
            _log = log;
            _resultStrategy = resultStrategy;
            _lifetimes = lifetimeStrategies.ToDictionary(i => i.Lifetime, i => i);
        }
        
        public ExpressionSyntax? TryBuild(Dependency dependency, SemanticType resolvingType)
        {
            var cacheKey = new CacheKey(dependency, resolvingType);
            if (_cache.TryGetValue(cacheKey, out var result))
            {
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

            _cache.Add(cacheKey, objectBuildExpression);
            return objectBuildExpression;
        }

        private struct CacheKey
        {
            public readonly Dependency Dependency;
            public readonly SemanticType ResolvingType;

            public CacheKey(Dependency dependency, SemanticType resolvingType)
            {
                Dependency = dependency;
                ResolvingType = resolvingType;
            }

            public bool Equals(CacheKey other) => Dependency.Equals(other.Dependency) && ResolvingType.Equals(other.ResolvingType);

            public override bool Equals(object? obj) => obj is CacheKey other && Equals(other);

            public override int GetHashCode()
            {
                unchecked
                {
                    return (Dependency.GetHashCode() * 397) ^ ResolvingType.GetHashCode();
                }
            }
        }
    }
}