namespace Pure.DI.Core
{
    using System.Linq;
    using Microsoft.CodeAnalysis.CSharp.Syntax;

    // ReSharper disable once ClassNeverInstantiated.Global
    internal class BindingsProbe : IBindingsProbe
    {
        private readonly IBuildContext _buildContext;
        private readonly IBuildStrategy _buildStrategy;
        private readonly Log<BindingsProbe> _log;

        public BindingsProbe(
            IBuildContext buildContext,
            IBuildStrategy buildStrategy,
            Log<BindingsProbe> log)
        {
            _buildContext = buildContext;
            _buildStrategy = buildStrategy;
            _log = log;
        }

        public void Probe()
        {
            var dependencies = (
                    from binding in _buildContext.Metadata.Bindings
                    from dependency in binding.Dependencies
                    where dependency.IsValidTypeToResolve
                    // ReSharper disable once RedundantTypeArgumentsOfMethod
                    from tag in binding.GetTags(dependency).DefaultIfEmpty<ExpressionSyntax?>(null)
                    select (dependency, tag))
                .Distinct()
                .ToArray();

            foreach (var (dependency, tag) in dependencies)
            {
                if (_buildContext.IsCancellationRequested)
                {
                    _log.Trace(() => new []{ "Build canceled" });
                    break;
                }

                _buildStrategy.TryBuild(_buildContext.TypeResolver.Resolve(dependency, tag), dependency);
            }
        }
    }
}
