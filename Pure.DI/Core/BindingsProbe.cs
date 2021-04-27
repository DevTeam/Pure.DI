namespace Pure.DI.Core
{
    using System.Linq;
    using Microsoft.CodeAnalysis.CSharp.Syntax;

    // ReSharper disable once ClassNeverInstantiated.Global
    internal class BindingsProbe : IBindingsProbe
    {
        private readonly IBuildContext _buildContext;
        private readonly IBuildStrategy _buildStrategy;

        public BindingsProbe(
            IBuildContext buildContext,
            [Tag(Tags.SimpleBuildStrategy)] IBuildStrategy buildStrategy)
        {
            _buildContext = buildContext;
            _buildStrategy = buildStrategy;
        }

        public void Probe()
        {
            var dependencies = (
                    from binding in _buildContext.Metadata.Bindings
                    from dependency in binding.Dependencies
                    where dependency.IsValidTypeToResolve
                    // ReSharper disable once RedundantTypeArgumentsOfMethod
                    from tag in binding.Tags.DefaultIfEmpty<ExpressionSyntax?>(null)
                    select (dependency, tag))
                .Distinct()
                .ToArray();

            foreach (var (dependency, tag) in dependencies)
            {
                _buildStrategy.Build(_buildContext.TypeResolver.Resolve(dependency, tag, dependency.Type.Locations));
            }
        }
    }
}
