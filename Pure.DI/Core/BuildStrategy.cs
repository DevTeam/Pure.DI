namespace Pure.DI.Core
{
    using System.Collections.Generic;
    using System.Linq;
    using Microsoft.CodeAnalysis.CSharp.Syntax;

    // ReSharper disable once ClassNeverInstantiated.Global
    internal class BuildStrategy : IBuildStrategy
    {
        private readonly IDiagnostic _diagnostic;
        private readonly ITracer _tracer;
        private readonly IBindingResultStrategy _resultStrategy;
        private readonly IBuildStrategy _dependencyBuildStrategy;
        private readonly Dictionary<Lifetime, ILifetimeStrategy> _lifetimes;

        public BuildStrategy(
            IDiagnostic diagnostic,
            ITracer tracer,
            IEnumerable<ILifetimeStrategy> lifetimeStrategies,
            IBindingResultStrategy resultStrategy,
            IBuildStrategy? dependencyBindingExpressionStrategy = null)
        {
            _diagnostic = diagnostic;
            _tracer = tracer;
            _resultStrategy = resultStrategy;
            _dependencyBuildStrategy = dependencyBindingExpressionStrategy ?? this;
            _lifetimes = lifetimeStrategies.ToDictionary(i => i.Lifetime, i => i);
        }

        public ExpressionSyntax Build(Dependency dependency)
        {
            using var traceToken = _tracer.RegisterResolving(dependency);
            var objectBuildExpression = dependency.ObjectBuilder.Build(_dependencyBuildStrategy, dependency);
            if (!_lifetimes.TryGetValue(dependency.Binding.Lifetime, out var lifetimeStrategy))
            {
                _diagnostic.Error(Diagnostics.Unsupported, $"{dependency.Binding.Lifetime} lifetime is not supported.", dependency.Implementation.Type.Locations.FirstOrDefault());
            }

            objectBuildExpression = lifetimeStrategy.Build(dependency, objectBuildExpression);
            return _resultStrategy.Build(objectBuildExpression);
        }
    }
}