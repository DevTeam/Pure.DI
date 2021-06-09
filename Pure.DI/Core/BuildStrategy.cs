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

    // ReSharper disable once ClassNeverInstantiated.Global
    internal class BuildStrategy : IBuildStrategy
    {
        private readonly IDiagnostic _diagnostic;
        private readonly ITracer _tracer;
        private readonly ILog<BuildStrategy> _log;
        private readonly IWrapperStrategy[] _wrapperStrategy;
        private readonly Dictionary<Lifetime, ILifetimeStrategy> _lifetimes;
        private readonly Dictionary<Dependency, ExpressionSyntax> _cache = new();

        public BuildStrategy(
            IDiagnostic diagnostic,
            ITracer tracer,
            ILog<BuildStrategy> log,
            IWrapperStrategy[] wrapperStrategy,
            IEnumerable<ILifetimeStrategy> lifetimeStrategies)
        {
            _diagnostic = diagnostic;
            _tracer = tracer;
            _log = log;
            _wrapperStrategy = wrapperStrategy;
            _lifetimes = lifetimeStrategies.ToDictionary(i => i.Lifetime, i => i);
            DependencyBindingExpressionStrategy = this;
        }

        internal IBindingResultStrategy? ResultStrategy { get; set; }

        internal IBuildStrategy DependencyBindingExpressionStrategy { get; set; }

        public ExpressionSyntax Build(Dependency dependency, SemanticType resolvingType)
        {
            if (_cache.TryGetValue(dependency, out var result))
            {
                return result;
            }

            using var traceToken = _tracer.RegisterResolving(dependency);
            var objectBuildExpression = dependency.ObjectBuilder.Build(DependencyBindingExpressionStrategy, dependency);
            
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
            result = ResultStrategy!.Build(objectBuildExpression);
            _cache.Add(dependency, result);
            _log.Info(() => new []{ $"{dependency} => {result.NormalizeWhitespace()}"});
            return result;
        }
    }
}