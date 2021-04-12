namespace Pure.DI.Core
{
    using Microsoft.CodeAnalysis.CSharp;

    // ReSharper disable once ClassNeverInstantiated.Global
    internal class StaticWithTagResolveMethodBuilder : IResolveMethodBuilder
    {
        private readonly ResolverMetadata _metadata;
        private readonly IFallbackStrategy _fallbackStrategy;
        private readonly IBuildStrategy _buildStrategy;
        private readonly IBindingStatementsStrategy _bindingStatementsStrategy;

        public StaticWithTagResolveMethodBuilder(
            ResolverMetadata metadata,
            IFallbackStrategy fallbackStrategy,
            [Tag(Tags.SimpleBuildStrategy)] IBuildStrategy buildStrategy,
            [Tag(Tags.TypeAndTagStatementsStrategy)] IBindingStatementsStrategy bindingStatementsStrategy)
        {
            _metadata = metadata;
            _fallbackStrategy = fallbackStrategy;
            _buildStrategy = buildStrategy;
            _bindingStatementsStrategy = bindingStatementsStrategy;
        }

        public ResolveMethod Build()
        {
            var returnWithTagDefault = _fallbackStrategy.Build(_metadata.Fallback, null, SyntaxFactory.ParseTypeName("type"), SyntaxFactory.ParseTypeName("type"));
            return new ResolveMethod(SyntaxRepo.StaticResolveWithTagMethodSyntax, false, _buildStrategy, _bindingStatementsStrategy, SyntaxFactory.ParseName("type"), returnWithTagDefault);
        }
    }
}