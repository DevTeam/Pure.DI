// ReSharper disable UnusedAutoPropertyAccessor.Local
// ReSharper disable MemberCanBePrivate.Global
// ReSharper disable UnusedAutoPropertyAccessor.Global
// ReSharper disable AutoPropertyCanBeMadeGetOnly.Global
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
        private readonly IWrapperStrategy[] _wrapperStrategy;
        private readonly Dictionary<Lifetime, ILifetimeStrategy> _lifetimes;
        private readonly Dictionary<Dependency, ExpressionSyntax?> _cache = new();

        public BuildStrategy(
            IDiagnostic diagnostic,
            ITracer tracer,
            ILog<BuildStrategy> log,
            [Tag(AsIsResult)] IBindingResultStrategy resultStrategy,
            IWrapperStrategy[] wrapperStrategy,
            IEnumerable<ILifetimeStrategy> lifetimeStrategies)
        {
            _diagnostic = diagnostic;
            _tracer = tracer;
            _log = log;
            _resultStrategy = resultStrategy;
            _wrapperStrategy = wrapperStrategy;
            _lifetimes = lifetimeStrategies.ToDictionary(i => i.Lifetime, i => i);
        }
        
        public ExpressionSyntax? TryBuild(Dependency dependency, SemanticType resolvingType)
        {
            if (_cache.TryGetValue(dependency, out var result))
            {
                return result;
            }

            using var traceToken = _tracer.RegisterResolving(dependency);
            var objectBuildExpression = dependency.ObjectBuilder.TryBuild(this, dependency);
            if (objectBuildExpression != null)
            {
                // Apply wrappers
                objectBuildExpression = _wrapperStrategy.Aggregate(objectBuildExpression, (current, wrapperStrategy) => wrapperStrategy.Build(dependency, current));

                // Apply lifetime
                if (!_lifetimes.TryGetValue(dependency.Binding.Lifetime, out var lifetimeStrategy))
                {
                    var error = $"{dependency.Binding.Lifetime} lifetime is not supported.";
                    _diagnostic.Error(Diagnostics.Error.Unsupported, error, dependency.Implementation.Type.Locations.FirstOrDefault());
                    throw new HandledException(error);
                }

                objectBuildExpression = lifetimeStrategy.Build(dependency, objectBuildExpression);
                objectBuildExpression = _resultStrategy.Build(objectBuildExpression);
            }

            _cache.Add(dependency, objectBuildExpression);
            if(objectBuildExpression != null)
            {
                _log.Info(() => new []{ $"{dependency} => {objectBuildExpression.NormalizeWhitespace()}"});
            }
            
            return objectBuildExpression;
        }
    }
}