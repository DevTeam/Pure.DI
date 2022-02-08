// ReSharper disable UnusedAutoPropertyAccessor.Local
// ReSharper disable MemberCanBePrivate.Global
// ReSharper disable UnusedAutoPropertyAccessor.Global
// ReSharper disable AutoPropertyCanBeMadeGetOnly.Global
// ReSharper disable MemberCanBePrivate.Local
namespace Pure.DI.Core;

using NS35EBD81B;
using static Tags;

// ReSharper disable once ClassNeverInstantiated.Global
internal class BuildStrategy : IBuildStrategy
{
    private readonly IDiagnostic _diagnostic;
    private readonly ITracer _tracer;
    private readonly ILog<BuildStrategy> _log;
    private readonly IBuildContext _buildContext;
    private readonly IBindingResultStrategy _resultStrategy;
    private readonly ICache<BuildStrategyKey, Optional<ExpressionSyntax>> _cache;
    private readonly Dictionary<Lifetime, ILifetimeStrategy> _lifetimes;

    public BuildStrategy(
        IDiagnostic diagnostic,
        ITracer tracer,
        ILog<BuildStrategy> log,
        IBuildContext buildContext,
        [Tag(AsIsResult)] IBindingResultStrategy resultStrategy,
        IEnumerable<ILifetimeStrategy> lifetimeStrategies,
        [Tag(ContainerScope)] ICache<BuildStrategyKey, Optional<ExpressionSyntax>> cache)
    {
        _diagnostic = diagnostic;
        _tracer = tracer;
        _log = log;
        _buildContext = buildContext;
        _resultStrategy = resultStrategy;
        _cache = cache;
        _lifetimes = lifetimeStrategies.Where(i => i.Lifetime.HasValue).ToDictionary(i => (Lifetime)i.Lifetime!, i => i);
    }

    public Optional<ExpressionSyntax> TryBuild(Dependency dependency, SemanticType resolvingType) =>
        _cache.GetOrAdd(new BuildStrategyKey(_buildContext.Id, dependency), _ => TryBuildInternal(dependency, resolvingType));

    public Optional<ExpressionSyntax> TryBuildInternal(Dependency dependency, SemanticType resolvingType)
    {
        using var traceToken = _tracer.RegisterResolving(dependency);
        var stopwatch = new Stopwatch();
        stopwatch.Start();
        var objectBuildExpression = dependency.ObjectBuilder.TryBuild(this, dependency);
        if (!objectBuildExpression.HasValue)
        {
            var path = _tracer.Paths
                           .Where(i => i.Length > 1)
                           .OrderBy(i => i.Length)
                           .LastOrDefault()
                           ?.Reverse()
                           .Select(i => $"[{i}]")
                           .ToArray()
                       ?? Array.Empty<string>();

            var chain = string.Join("---", path);
            var description = objectBuildExpression.Description;
            if (chain.Length > 0)
            {
                description = $"{description}; the chain is {chain}";
            }

            return new Optional<ExpressionSyntax>(objectBuildExpression.Value, objectBuildExpression.HasValue, description, objectBuildExpression.Locations);
        }

        // Apply lifetime
        if (!_lifetimes.TryGetValue(dependency.Binding.Lifetime, out var lifetimeStrategy))
        {
            var error = $"{dependency.Binding.Lifetime} lifetime is not supported.";
            _diagnostic.Error(Diagnostics.Error.Unsupported, error, dependency.Implementation.Type.Locations.ToArray());
            throw new HandledException(error);
        }

        objectBuildExpression = lifetimeStrategy.Build(resolvingType, dependency, objectBuildExpression.Value);
        objectBuildExpression = _resultStrategy.Build(objectBuildExpression.Value);
        stopwatch.Stop();
        _log.Info(() => new[]
        {
            $"[{stopwatch.ElapsedMilliseconds}] {dependency} => {objectBuildExpression.Value.NormalizeWhitespace()}"
        });

        return objectBuildExpression;
    }
}