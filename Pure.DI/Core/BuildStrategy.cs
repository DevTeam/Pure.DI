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

        public ExpressionSyntax Build(TypeDescription typeDescription)
        {
            using var traceToken = _tracer.RegisterResolving(typeDescription);
            var objectBuildExpression = typeDescription.ObjectBuilder.Build(_dependencyBuildStrategy, typeDescription);
            if (!_lifetimes.TryGetValue(typeDescription.Binding.Lifetime, out var lifetimeStrategy))
            {
                _diagnostic.Error(Diagnostics.Unsupported, $"{typeDescription.Binding.Lifetime} lifetime is not supported.");
            }

            objectBuildExpression = lifetimeStrategy.Build(typeDescription, objectBuildExpression);
            return _resultStrategy.Build(objectBuildExpression);
        }
    }
}