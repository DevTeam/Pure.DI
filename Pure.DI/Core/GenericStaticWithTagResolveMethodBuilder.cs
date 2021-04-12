namespace Pure.DI.Core
{
    using Microsoft.CodeAnalysis.CSharp;

    // ReSharper disable once ClassNeverInstantiated.Global
    internal class GenericStaticWithTagResolveMethodBuilder : IResolveMethodBuilder
    {
        private readonly ResolverMetadata _metadata;
        private readonly IFallbackStrategy _fallbackStrategy;
        private readonly IBuildStrategy _buildStrategy;
        private readonly IBindingStatementsStrategy _bindingStatementsStrategy;

        public GenericStaticWithTagResolveMethodBuilder(
            ResolverMetadata metadata,
            IFallbackStrategy fallbackStrategy,
            [Tag(Tags.GenericBuildStrategy)] IBuildStrategy buildStrategy,
            [Tag(Tags.TypeAndTagStatementsStrategy)] IBindingStatementsStrategy bindingStatementsStrategy)
        {
            _metadata = metadata;
            _fallbackStrategy = fallbackStrategy;
            _buildStrategy = buildStrategy;
            _bindingStatementsStrategy = bindingStatementsStrategy;
        }

        public ResolveMethod Build()
        {
            var genericWithTagReturnDefault = _fallbackStrategy.Build(_metadata.Fallback, SyntaxRepo.TTypeSyntax, SyntaxFactory.TypeOfExpression(SyntaxRepo.TTypeSyntax), SyntaxFactory.ParseTypeName("tag"));
            return new ResolveMethod(SyntaxRepo.GenericStaticResolveWithTagMethodSyntax, false, _buildStrategy, _bindingStatementsStrategy, SyntaxFactory.TypeOfExpression(SyntaxRepo.TTypeSyntax), genericWithTagReturnDefault);
        }
    }
}